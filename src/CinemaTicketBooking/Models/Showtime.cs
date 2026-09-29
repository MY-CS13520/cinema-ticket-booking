namespace CinemaTicketBooking.Models;

/// <summary>
/// One screening of a movie in a hall. Ticket price is captured here so later price
/// changes do not rewrite tickets already sold.
/// </summary>
public class Showtime
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public int HallId { get; set; }

    public CinemaHall Hall { get; set; } = null!;

    public DateTime StartsAt { get; set; }

    public decimal TicketPrice { get; set; }

    /// <summary>Cancelled screenings stay on file so past tickets still resolve.</summary>
    public bool IsCancelled { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
