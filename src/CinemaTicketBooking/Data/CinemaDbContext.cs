using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Data;

/// <summary>
/// EF Core model for the cinema desk. SQLite stores the file named by <see cref="AppPaths.DatabaseFile"/>.
/// </summary>
public class CinemaDbContext : DbContext
{
    public CinemaDbContext(DbContextOptions<CinemaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<CinemaHall> Halls => Set<CinemaHall>();

    public DbSet<Showtime> Showtimes => Set<Showtime>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureMovie(modelBuilder);
        ConfigureHall(modelBuilder);
        ConfigureShowtime(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureSeat(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureAccount(modelBuilder);
    }

    private static void ConfigureMovie(ModelBuilder modelBuilder)
    {
        var movie = modelBuilder.Entity<Movie>();
        movie.Property(m => m.Title).HasMaxLength(120).IsRequired();
        movie.Property(m => m.Genre).HasMaxLength(60).IsRequired();
        movie.Property(m => m.Director).HasMaxLength(80);
        movie.Property(m => m.Language).HasMaxLength(40);
        movie.Property(m => m.Description).HasMaxLength(1000);
        movie.Property(m => m.AgeRating).HasConversion<string>().HasMaxLength(16);
        movie.HasIndex(m => m.Title);
    }

    private static void ConfigureHall(ModelBuilder modelBuilder)
    {
        var hall = modelBuilder.Entity<CinemaHall>();
        hall.Property(h => h.Name).HasMaxLength(80).IsRequired();
        hall.Property(h => h.Format).HasConversion<string>().HasMaxLength(16);
        hall.Ignore(h => h.Capacity);
        hall.HasIndex(h => h.Name).IsUnique();
    }

    private static void ConfigureShowtime(ModelBuilder modelBuilder)
    {
        var show = modelBuilder.Entity<Showtime>();
        show.Property(s => s.TicketPrice).HasPrecision(10, 2);
        show.HasIndex(s => s.StartsAt);

        show.HasOne(s => s.Movie)
            .WithMany(m => m.Showtimes)
            .HasForeignKey(s => s.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        show.HasOne(s => s.Hall)
            .WithMany(h => h.Showtimes)
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBooking(ModelBuilder modelBuilder)
    {
        var booking = modelBuilder.Entity<Booking>();
        booking.Property(b => b.BookingCode).HasMaxLength(20).IsRequired();
        booking.Property(b => b.CustomerName).HasMaxLength(80).IsRequired();
        booking.Property(b => b.Phone).HasMaxLength(30);
        booking.Property(b => b.Email).HasMaxLength(120);
        booking.Property(b => b.TotalAmount).HasPrecision(10, 2);
        booking.Property(b => b.Status).HasConversion<string>().HasMaxLength(16);
        booking.HasIndex(b => b.BookingCode).IsUnique();

        booking.HasOne(b => b.Showtime)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        booking.HasOne(b => b.Account)
            .WithMany()
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureAccount(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<Account>();
        account.Property(a => a.Username).HasMaxLength(40).IsRequired();
        account.Property(a => a.DisplayName).HasMaxLength(80).IsRequired();
        account.Property(a => a.PasswordHash).HasMaxLength(100).IsRequired();
        account.Property(a => a.PasswordSalt).HasMaxLength(50).IsRequired();
        account.Property(a => a.Role).HasConversion<string>().HasMaxLength(16);
        account.HasIndex(a => a.Username).IsUnique();
    }

    private static void ConfigureSeat(ModelBuilder modelBuilder)
    {
        var seat = modelBuilder.Entity<BookingSeat>();
        seat.Property(s => s.RowLabel).HasMaxLength(2).IsRequired();
        seat.Property(s => s.Price).HasPrecision(10, 2);
        seat.Ignore(s => s.Label);

        seat.HasOne(s => s.Booking)
            .WithMany(b => b.Seats)
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Active chairs are unique per screening. Cancelled lines stay, with IsActive = false,
        // so the same seat can be sold again.
        seat.HasIndex(s => new { s.ShowtimeId, s.RowLabel, s.SeatNumber })
            .IsUnique()
            .HasFilter("IsActive = 1");
    }

    private static void ConfigurePayment(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<Payment>();
        payment.Property(p => p.Amount).HasPrecision(10, 2);
        payment.Property(p => p.Method).HasConversion<string>().HasMaxLength(16);
        payment.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        payment.Property(p => p.Reference).HasMaxLength(40);

        payment.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        payment.HasIndex(p => p.BookingId).IsUnique();
    }
}
