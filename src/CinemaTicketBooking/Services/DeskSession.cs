using CinemaTicketBooking.Models;

namespace CinemaTicketBooking.Services;

/// <summary>Who is signed in for this run of the desk.</summary>
public sealed class DeskSession
{
    public Account? Current { get; private set; }

    public bool IsSignedIn => Current is not null;

    public bool IsAdmin => Current?.Role == AccountRole.Admin;

    public void SignIn(Account account) => Current = account;

    public void SignOut() => Current = null;
}
