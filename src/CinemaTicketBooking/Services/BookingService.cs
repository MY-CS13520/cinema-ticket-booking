using CinemaTicketBooking.Data;
using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Services;

/// <summary>Seat maps, ticket sales, and the booking list.</summary>
public interface IBookingService
{
    /// <summary>Screenings the desk can still sell, from the start of today through the next two weeks.</summary>
    Task<IReadOnlyList<ShowtimeRow>> GetBookableShowtimesAsync();

    Task<SeatMapData> GetSeatMapAsync(int showtimeId);

    Task<TicketReceipt> CreateAsync(BookingDraft draft);

    Task<IReadOnlyList<BookingRow>> SearchAsync(string? query);

    Task CancelAsync(int bookingId);
}

/// <inheritdoc cref="IBookingService"/>
public sealed class BookingService : IBookingService
{
    private readonly IDbContextFactory<CinemaDbContext> _dbFactory;

    public BookingService(IDbContextFactory<CinemaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<ShowtimeRow>> GetBookableShowtimesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var from = DateTime.Today;
        var to = DateTime.Today.AddDays(14);

        var shows = await db.Showtimes
            .AsNoTracking()
            .Include(show => show.Movie)
            .Include(show => show.Hall)
            .Where(show => !show.IsCancelled && show.StartsAt >= from && show.StartsAt < to)
            .OrderBy(show => show.StartsAt)
            .ToListAsync();

        var taken = await ScheduleService.CountTakenSeatsAsync(db, shows.Select(show => show.Id));
        return shows.Select(show => RowFactory.Showtime(show, taken.GetValueOrDefault(show.Id))).ToList();
    }

    public async Task<SeatMapData> GetSeatMapAsync(int showtimeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var show = await db.Showtimes
            .AsNoTracking()
            .Include(item => item.Movie)
            .Include(item => item.Hall)
            .FirstOrDefaultAsync(item => item.Id == showtimeId)
            ?? throw new CinemaValidationException("That screening is no longer on the board.");

        var takenCount = await db.BookingSeats.CountAsync(seat => seat.ShowtimeId == showtimeId && seat.IsActive);
        var taken = await db.BookingSeats
            .AsNoTracking()
            .Where(seat => seat.ShowtimeId == showtimeId && seat.IsActive)
            .Select(seat => new { seat.RowLabel, seat.SeatNumber })
            .ToListAsync();

        return new SeatMapData
        {
            Showtime = RowFactory.Showtime(show, takenCount),
            TakenKeys = taken.Select(seat => SeatKey.Of(seat.RowLabel, seat.SeatNumber)).ToHashSet()
        };
    }

    public async Task<TicketReceipt> CreateAsync(BookingDraft draft)
    {
        var name = draft.CustomerName.Trim();
        if (name.Length < 2)
            throw new CinemaValidationException("Enter the guest's name.");

        var email = draft.Email.Trim();
        if (email.Length > 0 && !email.Contains('@'))
            throw new CinemaValidationException("Enter a valid email, or leave it blank.");

        var phone = draft.Phone.Trim();
        var digitCount = phone.Count(char.IsDigit);
        if (phone.Length > 0 && digitCount < 7)
            throw new CinemaValidationException("Enter a phone number with at least 7 digits, or leave it blank.");

        if (draft.Seats.Count == 0)
            throw new CinemaValidationException("Select at least one seat.");

        if (draft.Status is not (PaymentStatus.Paid or PaymentStatus.Pending))
            throw new CinemaValidationException("A new ticket can be marked paid or pending.");

        var requested = draft.Seats
            .Select(seat => (Row: seat.RowLabel.Trim().ToUpperInvariant(), seat.SeatNumber))
            .ToList();

        if (requested.Select(seat => SeatKey.Of(seat.Row, seat.SeatNumber)).Distinct().Count() != requested.Count)
            throw new CinemaValidationException("The same seat was selected twice.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var show = await db.Showtimes
            .Include(item => item.Movie)
            .Include(item => item.Hall)
            .FirstOrDefaultAsync(item => item.Id == draft.ShowtimeId)
            ?? throw new CinemaValidationException("That screening is no longer on the board.");

        if (show.IsCancelled)
            throw new CinemaValidationException("This screening has been cancelled.");

        foreach (var seat in requested)
        {
            var rowIndex = seat.Row.Length == 1 ? seat.Row[0] - 'A' : -1;
            var insideHall = rowIndex >= 0
                && rowIndex < show.Hall.RowCount
                && seat.SeatNumber >= 1
                && seat.SeatNumber <= show.Hall.SeatsPerRow;

            if (!insideHall)
                throw new CinemaValidationException($"Seat {seat.Row}{seat.SeatNumber} is not in this hall.");
        }

        var taken = await db.BookingSeats
            .Where(seat => seat.ShowtimeId == show.Id && seat.IsActive)
            .Select(seat => new { seat.RowLabel, seat.SeatNumber })
            .ToListAsync();

        var takenKeys = taken.Select(seat => SeatKey.Of(seat.RowLabel, seat.SeatNumber)).ToHashSet();
        var clash = requested.FirstOrDefault(seat => takenKeys.Contains(SeatKey.Of(seat.Row, seat.SeatNumber)));
        if (clash != default)
            throw new CinemaValidationException($"Seat {clash.Row}{clash.SeatNumber} was just booked. Choose another chair.");

        var price = show.TicketPrice;
        var total = decimal.Round(price * requested.Count, 2, MidpointRounding.AwayFromZero);
        var now = DateTime.Now;
        var code = await NextCodeAsync(db);

        var booking = new Booking
        {
            BookingCode = code,
            ShowtimeId = show.Id,
            CustomerName = name,
            Phone = phone,
            Email = email,
            BookedAt = now,
            Status = BookingStatus.Confirmed,
            TotalAmount = total,
            Seats = requested.Select(seat => new BookingSeat
            {
                ShowtimeId = show.Id,
                RowLabel = seat.Row,
                SeatNumber = seat.SeatNumber,
                Price = price,
                IsActive = true
            }).ToList(),
            Payment = new Payment
            {
                Amount = total,
                Method = draft.Method,
                Status = draft.Status,
                PaidAt = draft.Status == PaymentStatus.Paid ? now : null,
                Reference = code
            }
        };

        db.Bookings.Add(booking);

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            throw new CinemaValidationException("One of those seats was just booked. Pick another chair.");
        }

        return new TicketReceipt
        {
            BookingCode = code,
            MovieTitle = show.Movie.Title,
            HallName = show.Hall.Name,
            StartsAt = show.StartsAt,
            Seats = requested.Select(seat => $"{seat.Row}{seat.SeatNumber}").ToList(),
            Total = total,
            Method = draft.Method.ToString(),
            Status = draft.Status.ToString()
        };
    }

    public async Task<IReadOnlyList<BookingRow>> SearchAsync(string? query)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Seats)
            .Include(booking => booking.Showtime).ThenInclude(show => show.Movie)
            .Include(booking => booking.Showtime).ThenInclude(show => show.Hall)
            .OrderByDescending(booking => booking.BookedAt)
            .ToListAsync();

        var needle = query?.Trim();
        if (!string.IsNullOrEmpty(needle))
        {
            bookings = bookings.Where(booking =>
                booking.BookingCode.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || booking.CustomerName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || booking.Showtime.Movie.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || booking.Phone.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return bookings.Select(RowFactory.Booking).ToList();
    }

    public async Task CancelAsync(int bookingId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var booking = await db.Bookings
            .Include(item => item.Seats)
            .Include(item => item.Payment)
            .FirstOrDefaultAsync(item => item.Id == bookingId)
            ?? throw new CinemaValidationException("That booking is no longer on file.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new CinemaValidationException("This booking is already cancelled.");

        booking.Status = BookingStatus.Cancelled;
        foreach (var seat in booking.Seats)
            seat.IsActive = false;

        if (booking.Payment is { } payment)
        {
            // Money that was collected becomes a refund. A pending sale is voided and is not counted as a refund.
            if (payment.Status == PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = DateTime.Now;
            }
            else if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Refunded;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<string> NextCodeAsync(CinemaDbContext db)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = "LC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await db.Bookings.AnyAsync(booking => booking.BookingCode == code))
                return code;
        }

        throw new CinemaValidationException("Could not create a booking code. Try again.");
    }
}
