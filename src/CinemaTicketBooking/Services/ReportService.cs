using CinemaTicketBooking.Data;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Services;

/// <summary>Home-screen figures and the date-range sales report.</summary>
public interface IReportService
{
    Task<DashboardData> GetDashboardAsync();

    Task<SalesReport> GetSalesAsync(DateTime from, DateTime to);
}

/// <inheritdoc cref="IReportService"/>
public sealed class ReportService : IReportService
{
    private readonly IDbContextFactory<CinemaDbContext> _dbFactory;

    public ReportService(IDbContextFactory<CinemaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardData> GetDashboardAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var revenueToday = await db.Payments
            .Where(payment => payment.PaidAt >= today && payment.PaidAt < tomorrow)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0m;

        var ticketsToday = await db.BookingSeats
            .CountAsync(seat => seat.IsActive
                && seat.Booking.Status == BookingStatus.Confirmed
                && seat.Booking.BookedAt >= today
                && seat.Booking.BookedAt < tomorrow);

        var showsToday = await db.Showtimes
            .Include(show => show.Hall)
            .Where(show => !show.IsCancelled && show.StartsAt >= today && show.StartsAt < tomorrow)
            .ToListAsync();

        var taken = await ScheduleService.CountTakenSeatsAsync(db, showsToday.Select(show => show.Id));
        var seatsLeft = showsToday.Sum(show => Math.Max(0, show.Hall.Capacity - taken.GetValueOrDefault(show.Id)));

        var upcomingShows = await db.Showtimes
            .AsNoTracking()
            .Include(show => show.Movie)
            .Include(show => show.Hall)
            .Where(show => !show.IsCancelled && show.StartsAt >= DateTime.Now.AddMinutes(-20))
            .OrderBy(show => show.StartsAt)
            .Take(8)
            .ToListAsync();

        var upcomingTaken = await ScheduleService.CountTakenSeatsAsync(db, upcomingShows.Select(show => show.Id));

        var recent = await db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Seats)
            .Include(booking => booking.Showtime).ThenInclude(show => show.Movie)
            .Include(booking => booking.Showtime).ThenInclude(show => show.Hall)
            .OrderByDescending(booking => booking.BookedAt)
            .Take(6)
            .ToListAsync();

        return new DashboardData
        {
            RevenueToday = revenueToday,
            TicketsToday = ticketsToday,
            ShowsToday = showsToday.Count,
            SeatsLeftToday = seatsLeft,
            Upcoming = upcomingShows.Select(show => RowFactory.Showtime(show, upcomingTaken.GetValueOrDefault(show.Id))).ToList(),
            RecentBookings = recent.Select(RowFactory.Booking).ToList()
        };
    }

    public async Task<SalesReport> GetSalesAsync(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);
        if (end <= start)
            throw new CinemaValidationException("The end date needs to be on or after the start date.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var payments = await db.Payments
            .AsNoTracking()
            .Include(payment => payment.Booking).ThenInclude(booking => booking.Showtime).ThenInclude(show => show.Movie)
            .ToListAsync();

        var collected = payments
            .Where(payment => payment.PaidAt >= start && payment.PaidAt < end)
            .ToList();

        var refunds = payments
            .Where(payment => payment.PaidAt != null && payment.RefundedAt >= start && payment.RefundedAt < end)
            .Sum(payment => payment.Amount);

        var pending = payments
            .Where(payment => payment.Status == PaymentStatus.Pending
                && payment.Booking.BookedAt >= start
                && payment.Booking.BookedAt < end)
            .Sum(payment => payment.Amount);

        var confirmed = await db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Seats)
            .Include(booking => booking.Payment)
            .Include(booking => booking.Showtime).ThenInclude(show => show.Movie)
            .Where(booking => booking.Status == BookingStatus.Confirmed
                && booking.BookedAt >= start
                && booking.BookedAt < end)
            .ToListAsync();

        var byMovie = confirmed
            .GroupBy(booking => booking.Showtime.Movie.Title)
            .Select(group => new MovieSalesRow
            {
                Title = group.Key,
                Tickets = group.Sum(booking => booking.Seats.Count(seat => seat.IsActive)),
                Revenue = group.Where(booking => booking.Payment?.Status == PaymentStatus.Paid).Sum(booking => booking.Payment!.Amount)
            })
            .OrderByDescending(row => row.Revenue)
            .ThenBy(row => row.Title)
            .ToList();

        var moviePeak = byMovie.Count == 0 ? 0 : byMovie.Max(row => row.Revenue);
        foreach (var row in byMovie)
            row.Share = moviePeak <= 0 ? 0 : (double)(row.Revenue / moviePeak) * 100;

        var byMethod = collected
            .GroupBy(payment => payment.Method.ToString())
            .Select(group => new MethodSalesRow
            {
                Method = group.Key,
                Payments = group.Count(),
                Revenue = group.Sum(payment => payment.Amount)
            })
            .OrderByDescending(row => row.Revenue)
            .ToList();

        var methodPeak = byMethod.Count == 0 ? 0 : byMethod.Max(row => row.Revenue);
        foreach (var row in byMethod)
            row.Share = methodPeak <= 0 ? 0 : (double)(row.Revenue / methodPeak) * 100;

        var gross = collected.Sum(payment => payment.Amount);

        return new SalesReport
        {
            TicketsSold = confirmed.Sum(booking => booking.Seats.Count(seat => seat.IsActive)),
            BookingCount = confirmed.Count,
            GrossRevenue = gross,
            PendingAmount = pending,
            Refunds = refunds,
            NetRevenue = gross - refunds,
            ByMovie = byMovie,
            ByMethod = byMethod
        };
    }
}
