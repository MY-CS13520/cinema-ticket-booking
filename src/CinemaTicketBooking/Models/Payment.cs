namespace CinemaTicketBooking.Models;

/// <summary>
/// Money taken for a booking. Status moves from pending to paid, or from paid to refunded.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    /// <summary>Set when the desk marks the payment as paid. Null while it is still pending.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>Set when a paid ticket is refunded. Gross sales and refunds use these two timestamps.</summary>
    public DateTime? RefundedAt { get; set; }

    public string Reference { get; set; } = string.Empty;
}
