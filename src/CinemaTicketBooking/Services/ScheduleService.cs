using CinemaTicketBooking.Data;
using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Services;

/// <summary>Halls and the screenings placed inside them.</summary>
public interface IScheduleService
{
    Task<IReadOnlyList<HallRow>> GetHallsAsync();

    Task<int> SaveHallAsync(HallDraft draft);

    Task DeleteHallAsync(int id);

    Task<IReadOnlyList<ShowtimeRow>> GetShowtimesAsync(bool upcomingOnly);

    Task<int> SaveShowtimeAsync(ShowtimeDraft draft);

    Task CancelShowtimeAsync(int id);

    Task DeleteShowtimeAsync(int id);
}

/// <inheritdoc cref="IScheduleService"/>
public sealed class ScheduleService : IScheduleService
{
    private readonly IDbContextFactory<CinemaDbContext> _dbFactory;

    public ScheduleService(IDbContextFactory<CinemaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<HallRow>> GetHallsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var halls = await db.Halls.AsNoTracking().OrderBy(hall => hall.Name).ToListAsync();
        return halls.Select(hall => new HallRow
        {
            Id = hall.Id,
            Name = hall.Name,
            RowCount = hall.RowCount,
            SeatsPerRow = hall.SeatsPerRow,
            Format = CinemaText.Format(hall.Format)
        }).ToList();
    }

    public async Task<int> SaveHallAsync(HallDraft draft)
    {
        var name = draft.Name.Trim();
        if (name.Length is < 2 or > 80)
            throw new CinemaValidationException("Give the hall a name between 2 and 80 characters.");

        if (draft.RowCount is < 1 or > 20)
            throw new CinemaValidationException("A hall can have between 1 and 20 rows.");

        if (draft.SeatsPerRow is < 4 or > 20)
            throw new CinemaValidationException("Each row can have between 4 and 20 seats.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var names = await db.Halls
            .Where(hall => hall.Id != (draft.Id ?? 0))
            .Select(hall => hall.Name)
            .ToListAsync();

        if (names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
            throw new CinemaValidationException("A hall with that name already exists.");

        CinemaHall hall;
        if (draft.Id is int id)
        {
            hall = await db.Halls.FirstOrDefaultAsync(item => item.Id == id)
                ?? throw new CinemaValidationException("That hall is no longer on file.");

            var shapeChanged = hall.RowCount != draft.RowCount || hall.SeatsPerRow != draft.SeatsPerRow;
            if (shapeChanged && await db.Showtimes.AnyAsync(show => show.HallId == id))
            {
                throw new CinemaValidationException(
                    "This hall already has showtimes. Row and seat counts stay fixed so sold tickets still match the map.");
            }
        }
        else
        {
            hall = new CinemaHall();
            db.Halls.Add(hall);
        }

        hall.Name = name;
        hall.RowCount = draft.RowCount;
        hall.SeatsPerRow = draft.SeatsPerRow;
        hall.Format = draft.Format;

        await db.SaveChangesAsync();
        return hall.Id;
    }

    public async Task DeleteHallAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var hall = await db.Halls.FirstOrDefaultAsync(item => item.Id == id)
            ?? throw new CinemaValidationException("That hall is no longer on file.");

        if (await db.Showtimes.AnyAsync(show => show.HallId == id))
            throw new CinemaValidationException("Remove this hall's showtimes before deleting it.");

        db.Halls.Remove(hall);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ShowtimeRow>> GetShowtimesAsync(bool upcomingOnly)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Showtimes
            .AsNoTracking()
            .Include(show => show.Movie)
            .Include(show => show.Hall)
            .AsQueryable();

        if (upcomingOnly)
        {
            var cutoff = DateTime.Now.AddMinutes(-30);
            query = query.Where(show => !show.IsCancelled && show.StartsAt >= cutoff);
        }

        var shows = await query.OrderBy(show => show.StartsAt).ToListAsync();
        var taken = await CountTakenSeatsAsync(db, shows.Select(show => show.Id));
        return shows.Select(show => RowFactory.Showtime(show, taken.GetValueOrDefault(show.Id))).ToList();
    }

    public async Task<int> SaveShowtimeAsync(ShowtimeDraft draft)
    {
        if (draft.TicketPrice is <= 0 or > 10000)
            throw new CinemaValidationException("Enter a ticket price greater than zero.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var movie = await db.Movies.FirstOrDefaultAsync(item => item.Id == draft.MovieId)
            ?? throw new CinemaValidationException("Choose a film.");

        if (!movie.IsActive)
            throw new CinemaValidationException($"{movie.Title} is not on the board. Turn it back on before scheduling it.");

        var hall = await db.Halls.FirstOrDefaultAsync(item => item.Id == draft.HallId)
            ?? throw new CinemaValidationException("Choose a hall.");

        Showtime show;
        if (draft.Id is int id)
        {
            show = await db.Showtimes.FirstOrDefaultAsync(item => item.Id == id)
                ?? throw new CinemaValidationException("That screening is no longer on the board.");

            if (show.IsCancelled)
                throw new CinemaValidationException("This screening is cancelled. Add a new one instead of editing it.");

            var sold = await db.BookingSeats.AnyAsync(seat => seat.ShowtimeId == id && seat.IsActive);
            var identityChanged = show.MovieId != draft.MovieId
                || show.HallId != draft.HallId
                || show.StartsAt != draft.StartsAt;

            if (sold && identityChanged)
            {
                throw new CinemaValidationException(
                    "Tickets have been sold for this screening. You can still change the price. Movie, hall, and time stay as sold.");
            }
        }
        else
        {
            show = new Showtime();
            db.Showtimes.Add(show);
        }

        await EnsureHallIsFreeAsync(db, hall.Id, draft.Id, draft.StartsAt, movie);

        show.MovieId = movie.Id;
        show.HallId = hall.Id;
        show.StartsAt = draft.StartsAt;
        show.TicketPrice = decimal.Round(draft.TicketPrice, 2, MidpointRounding.AwayFromZero);

        await db.SaveChangesAsync();
        return show.Id;
    }

    public async Task CancelShowtimeAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var show = await db.Showtimes.FirstOrDefaultAsync(item => item.Id == id)
            ?? throw new CinemaValidationException("That screening is no longer on the board.");

        if (show.IsCancelled)
            throw new CinemaValidationException("This screening is already cancelled.");

        if (await db.BookingSeats.AnyAsync(seat => seat.ShowtimeId == id && seat.IsActive))
            throw new CinemaValidationException("Cancel the sold tickets first, then cancel the screening.");

        show.IsCancelled = true;
        await db.SaveChangesAsync();
    }

    public async Task DeleteShowtimeAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var show = await db.Showtimes.FirstOrDefaultAsync(item => item.Id == id)
            ?? throw new CinemaValidationException("That screening is no longer on the board.");

        if (await db.Bookings.AnyAsync(booking => booking.ShowtimeId == id))
            throw new CinemaValidationException("This screening has ticket history. Cancel it instead of deleting it.");

        db.Showtimes.Remove(show);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Blocks a second film in the same hall while the first is still on screen, including a short turnover.
    /// </summary>
    private static async Task EnsureHallIsFreeAsync(
        CinemaDbContext db,
        int hallId,
        int? showtimeId,
        DateTime startsAt,
        Movie movie)
    {
        var end = startsAt.AddMinutes(movie.DurationMinutes + DbInitializer.TurnoverMinutes);
        var others = await db.Showtimes
            .Include(show => show.Movie)
            .Where(show => show.HallId == hallId && !show.IsCancelled && show.Id != (showtimeId ?? 0))
            .ToListAsync();

        foreach (var other in others)
        {
            var otherEnd = other.StartsAt.AddMinutes(other.Movie.DurationMinutes + DbInitializer.TurnoverMinutes);
            var overlaps = startsAt < otherEnd && other.StartsAt < end;
            if (!overlaps)
                continue;

            throw new CinemaValidationException(
                $"{other.Movie.Title} is already in this hall from {other.StartsAt:HH:mm} to {otherEnd:HH:mm}, including turnover.");
        }
    }

    internal static async Task<Dictionary<int, int>> CountTakenSeatsAsync(CinemaDbContext db, IEnumerable<int> showtimeIds)
    {
        var ids = showtimeIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var counts = await db.BookingSeats
            .AsNoTracking()
            .Where(seat => seat.IsActive && ids.Contains(seat.ShowtimeId))
            .GroupBy(seat => seat.ShowtimeId)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToListAsync();

        return counts.ToDictionary(row => row.Id, row => row.Count);
    }
}
