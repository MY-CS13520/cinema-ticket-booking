using CinemaTicketBooking.Data;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Services;

/// <summary>Follow-up on money: mark a pending sale paid, or refund a paid one.</summary>
public interface IPaymentService
{
    Task<IReadOnlyList<PaymentRow>> GetAsync(PaymentStatus? status);

    Task MarkPaidAsync(int paymentId);

    Task RefundAsync(int paymentId);
}

/// <inheritdoc cref="IPaymentService"/>
public sealed class PaymentService : IPaymentService
{
    private readonly IDbContextFactory<CinemaDbContext> _dbFactory;
    private readonly IBookingService _bookings;

    public PaymentService(IDbContextFactory<CinemaDbContext> dbFactory, IBookingService bookings)
    {
        _dbFactory = dbFactory;
        _bookings = bookings;
    }

    public async Task<IReadOnlyList<PaymentRow>> GetAsync(PaymentStatus? status)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var payments = await db.Payments
            .AsNoTracking()
            .Include(payment => payment.Booking).ThenInclude(booking => booking.Showtime).ThenInclude(show => show.Movie)
            .OrderByDescending(payment => payment.PaidAt ?? payment.Booking.BookedAt)
            .ToListAsync();

        if (status is PaymentStatus filter)
            payments = payments.Where(payment => payment.Status == filter).ToList();

        return payments.Select(RowFactory.Payment).ToList();
    }

    public async Task MarkPaidAsync(int paymentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var payment = await db.Payments
            .Include(item => item.Booking)
            .FirstOrDefaultAsync(item => item.Id == paymentId)
            ?? throw new CinemaValidationException("That payment is no longer on file.");

        if (payment.Booking.Status != BookingStatus.Confirmed)
            throw new CinemaValidationException("This booking is cancelled, so the payment cannot be collected.");

        if (payment.Status != PaymentStatus.Pending)
            throw new CinemaValidationException("Only a pending payment can be marked paid.");

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public Task RefundAsync(int paymentId) => RefundCoreAsync(paymentId);

    private async Task RefundCoreAsync(int paymentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == paymentId)
            ?? throw new CinemaValidationException("That payment is no longer on file.");

        if (payment.Status != PaymentStatus.Paid)
            throw new CinemaValidationException("Only a paid ticket can be refunded. Cancel a pending booking from the bookings list.");

        // Cancelling the booking releases the seats and stamps the refund.
        await _bookings.CancelAsync(payment.BookingId);
    }
}
