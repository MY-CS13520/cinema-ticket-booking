using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Data;

/// <summary>
/// Creates the SQLite file on first launch and fills it with a small working cinema
/// so the seat map, payments, and reports are not empty.
/// </summary>
public static class DbInitializer
{
    /// <summary>Minutes of clearing time reserved after a film before the next one can start.</summary>
    public const int TurnoverMinutes = 15;

    public static async Task InitializeAsync(IDbContextFactory<CinemaDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        if (await db.Movies.AnyAsync())
            return;

        await using var transaction = await db.Database.BeginTransactionAsync();

        var halls = SeedHalls(db);
        var movies = SeedMovies(db);
        await db.SaveChangesAsync();

        var showtimes = SeedShowtimes(db, movies, halls);
        await db.SaveChangesAsync();

        SeedBookings(db, movies, showtimes);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    private static Dictionary<string, CinemaHall> SeedHalls(CinemaDbContext db)
    {
        var halls = new Dictionary<string, CinemaHall>
        {
            ["grand"] = new CinemaHall { Name = "Hall 1 · Grand", RowCount = 8, SeatsPerRow = 12, Format = HallFormat.Standard },
            ["velvet"] = new CinemaHall { Name = "Hall 2 · Velvet", RowCount = 6, SeatsPerRow = 10, Format = HallFormat.Premium },
            ["atlas"] = new CinemaHall { Name = "Hall 3 · Atlas", RowCount = 9, SeatsPerRow = 14, Format = HallFormat.Imax }
        };

        db.Halls.AddRange(halls.Values);
        return halls;
    }

    private static Dictionary<string, Movie> SeedMovies(CinemaDbContext db)
    {
        var movies = new Dictionary<string, Movie>
        {
            ["golden"] = Film("The Golden Hour", "Drama", "Mira Ellison", 118, AgeRating.PG13,
                "A photographer races the last light of the day to finish a portrait she cannot retake."),
            ["neon"] = Film("Neon District", "Science Fiction", "Jonah Park", 126, AgeRating.PG13,
                "In a city that never powers down, a courier carries a message that rewrites the grid."),
            ["harbor"] = Film("Harbor Lights", "Romance", "Adele Costa", 104, AgeRating.PG,
                "Two strangers keep missing the last ferry, and keep finding each other on the pier."),
            ["planes"] = Film("Paper Planes", "Family", "Samir Shah", 98, AgeRating.PG,
                "A quiet student folds a fleet of planes that carry notes across a divided town."),
            ["archive"] = Film("Midnight Archive", "Thriller", "Lena Voss", 112, AgeRating.R,
                "A night librarian discovers the restricted stacks are filing people, not books."),
            ["salt"] = Film("Salt & Silver", "Adventure", "Theo March", 131, AgeRating.PG13,
                "A cartographer maps a coastline that moves every time the tide turns.")
        };

        db.Movies.AddRange(movies.Values);
        return movies;
    }

    private static Movie Film(string title, string genre, string director, int minutes, AgeRating rating, string description)
        => new()
        {
            Title = title,
            Genre = genre,
            Director = director,
            Language = "English",
            DurationMinutes = minutes,
            AgeRating = rating,
            Description = description,
            IsActive = true
        };

    private static List<Showtime> SeedShowtimes(
        CinemaDbContext db,
        Dictionary<string, Movie> movies,
        Dictionary<string, CinemaHall> halls)
    {
        var today = DateTime.Today;
        var shows = new List<Showtime>
        {
            Show(movies["salt"], halls["atlas"], today.AddDays(-1).AddHours(18), 19.50m),
            Show(movies["golden"], halls["grand"], today.AddHours(13).AddMinutes(15), 12.50m),
            Show(movies["neon"], halls["atlas"], today.AddHours(16), 19.50m),
            Show(movies["harbor"], halls["velvet"], today.AddHours(18).AddMinutes(30), 16.00m),
            Show(movies["archive"], halls["grand"], today.AddHours(21), 12.50m),
            Show(movies["planes"], halls["grand"], today.AddDays(1).AddHours(11), 10.00m),
            Show(movies["salt"], halls["atlas"], today.AddDays(1).AddHours(15).AddMinutes(30), 19.50m),
            Show(movies["neon"], halls["velvet"], today.AddDays(1).AddHours(19).AddMinutes(15), 16.00m),
            Show(movies["golden"], halls["grand"], today.AddDays(1).AddHours(20).AddMinutes(45), 12.50m)
        };

        db.Showtimes.AddRange(shows);
        return shows;
    }

    private static Showtime Show(Movie movie, CinemaHall hall, DateTime startsAt, decimal price)
        => new()
        {
            Movie = movie,
            Hall = hall,
            StartsAt = startsAt,
            TicketPrice = price
        };

    private static void SeedBookings(
        CinemaDbContext db,
        Dictionary<string, Movie> movies,
        List<Showtime> showtimes)
    {
        var yesterdaySalt = showtimes.Single(s => s.Movie == movies["salt"] && s.StartsAt.Date < DateTime.Today);
        var harbor = showtimes.Single(s => s.Movie == movies["harbor"]);
        var archive = showtimes.Single(s => s.Movie == movies["archive"]);

        // A completed sale from yesterday, plus a refund, so the report has both columns filled.
        AddBooking(db, yesterdaySalt, "LC-DEMO1001", "Imani Joseph", "077 410 2201", "imani.joseph@example.com",
            PaymentMethod.Card, PaymentStatus.Paid, yesterdaySalt.StartsAt.AddHours(-3),
            seats: [( "C", 4), ("C", 5), ("C", 6)]);

        var refunded = AddBooking(db, yesterdaySalt, "LC-DEMO1002", "Leo Bennett", "077 410 2288", "",
            PaymentMethod.Cash, PaymentStatus.Refunded, yesterdaySalt.StartsAt.AddHours(-2),
            seats: [("D", 1), ("D", 2)]);
        refunded.Status = BookingStatus.Cancelled;
        foreach (var seat in refunded.Seats)
            seat.IsActive = false;
        refunded.Payment!.RefundedAt = yesterdaySalt.StartsAt.AddHours(-1);

        // Today's desk: one paid card sale and one cash sale still waiting to be marked paid.
        AddBooking(db, harbor, "LC-DEMO2001", "Ava Fernando", "077 555 0142", "ava.fernando@example.com",
            PaymentMethod.Card, PaymentStatus.Paid, DateTime.Now.AddMinutes(-40),
            seats: [("B", 3), ("B", 4), ("B", 5), ("B", 6)]);

        AddBooking(db, archive, "LC-DEMO2002", "Noah Perera", "076 220 1180", "",
            PaymentMethod.Cash, PaymentStatus.Pending, DateTime.Now.AddMinutes(-15),
            seats: [("A", 1), ("A", 2)]);
    }

    private static Booking AddBooking(
        CinemaDbContext db,
        Showtime showtime,
        string code,
        string name,
        string phone,
        string email,
        PaymentMethod method,
        PaymentStatus status,
        DateTime bookedAt,
        (string Row, int Number)[] seats)
    {
        var total = seats.Length * showtime.TicketPrice;
        var booking = new Booking
        {
            BookingCode = code,
            Showtime = showtime,
            CustomerName = name,
            Phone = phone,
            Email = email,
            BookedAt = bookedAt,
            Status = BookingStatus.Confirmed,
            TotalAmount = total,
            Seats = seats.Select(seat => new BookingSeat
            {
                ShowtimeId = showtime.Id,
                RowLabel = seat.Row,
                SeatNumber = seat.Number,
                Price = showtime.TicketPrice,
                IsActive = true
            }).ToList(),
            Payment = new Payment
            {
                Amount = total,
                Method = method,
                Status = status,
                PaidAt = status == PaymentStatus.Pending ? null : bookedAt,
                Reference = code
            }
        };

        db.Bookings.Add(booking);
        return booking;
    }
}
