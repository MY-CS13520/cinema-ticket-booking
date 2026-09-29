namespace CinemaTicketBooking.Models;

/// <summary>
/// A film in the catalogue. Scheduling and tickets point at a movie; they do not copy its title.
/// </summary>
public class Movie
{
    public int Id { get; set; }

    /// <summary>Title printed on the board and the ticket.</summary>
    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string Director { get; set; } = string.Empty;

    public string Language { get; set; } = "English";

    public AgeRating AgeRating { get; set; } = AgeRating.PG13;

    /// <summary>Running time used to keep two shows from overlapping in the same hall.</summary>
    public int DurationMinutes { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Inactive films stay in history but cannot be scheduled.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    /// <summary>Certificate as printed on a poster. Not stored; the enum is.</summary>
    public string RatingLabel => AgeRating switch
    {
        AgeRating.PG13 => "PG-13",
        AgeRating.NC17 => "NC-17",
        _ => AgeRating.ToString()
    };

    public string DurationLabel => $"{DurationMinutes} min";

    public string BoardLabel => IsActive ? "On the board" : "Hidden";
}
