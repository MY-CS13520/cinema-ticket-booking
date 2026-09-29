namespace CinemaTicketBooking.Models;

/// <summary>
/// A physical screen. Row and seat counts are the seat map; individual seats are not stored
/// until a guest books one.
/// </summary>
public class CinemaHall
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RowCount { get; set; }

    public int SeatsPerRow { get; set; }

    public HallFormat Format { get; set; } = HallFormat.Standard;

    public int Capacity => RowCount * SeatsPerRow;

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
