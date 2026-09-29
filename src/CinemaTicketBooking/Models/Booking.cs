namespace CinemaTicketBooking.Models;

/// <summary>
/// A guest's tickets for a single screening. Seats and the payment hang off this record.
/// </summary>
public class Booking
{
    public int Id { get; set; }

    /// <summary>Short code the desk reads back to the guest, such as LC-A1B2C3D4.</summary>
    public string BookingCode { get; set; } = string.Empty;

    public int ShowtimeId { get; set; }

    public Showtime Showtime { get; set; } = null!;

    public string CustomerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Set when a signed-in guest buys the ticket. Staff sales can leave this empty.</summary>
    public int? AccountId { get; set; }

    public Account? Account { get; set; }

    public DateTime BookedAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public decimal TotalAmount { get; set; }

    public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();

    public Payment? Payment { get; set; }
}
