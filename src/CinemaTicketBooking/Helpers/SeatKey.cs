namespace CinemaTicketBooking.Helpers;

/// <summary>
/// Stable key for a chair inside one screening, used when comparing the map with booked seats.
/// </summary>
public static class SeatKey
{
    public static string Of(string rowLabel, int seatNumber) => $"{rowLabel}:{seatNumber}";
}
