namespace CinemaTicketBooking.Models;

/// <summary>
/// A person who signs in. Guests book tickets. Staff run the desk.
/// </summary>
public class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public AccountRole Role { get; set; }
}
