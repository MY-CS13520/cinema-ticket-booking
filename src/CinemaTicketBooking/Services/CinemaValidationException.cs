namespace CinemaTicketBooking.Services;

/// <summary>
/// A problem the desk can explain to the user, such as a missing title or a seat that was just taken.
/// </summary>
public sealed class CinemaValidationException : Exception
{
    public CinemaValidationException(string message)
        : base(message)
    {
    }
}
