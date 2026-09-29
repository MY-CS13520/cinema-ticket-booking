namespace CinemaTicketBooking.Models;

/// <summary>
/// One chair on a booking. <see cref="IsActive"/> is cleared when the booking is cancelled
/// so the same chair can be sold again, while the original ticket line stays in history.
/// </summary>
public class BookingSeat
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    /// <summary>Copied from the screening so a seat can be locked without joining through the booking.</summary>
    public int ShowtimeId { get; set; }

    /// <summary>Letter of the row, starting at A.</summary>
    public string RowLabel { get; set; } = string.Empty;

    public int SeatNumber { get; set; }

    /// <summary>Price charged for this chair at the moment it was sold.</summary>
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public string Label => $"{RowLabel}{SeatNumber}";
}
