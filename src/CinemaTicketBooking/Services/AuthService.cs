using CinemaTicketBooking.Data;
using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Services;

/// <summary>Checks a username and password, and opens new guest accounts.</summary>
public interface IAuthService
{
    Task<Account> SignInAsync(string username, string password, AccountRole expectedRole);

    Task<Account> RegisterGuestAsync(string username, string displayName, string password);
}

/// <inheritdoc cref="IAuthService"/>
public sealed class AuthService : IAuthService
{
    private readonly IDbContextFactory<CinemaDbContext> _dbFactory;

    public AuthService(IDbContextFactory<CinemaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Account> SignInAsync(string username, string password, AccountRole expectedRole)
    {
        var name = username.Trim();
        if (name.Length == 0 || password.Length == 0)
            throw new CinemaValidationException("Enter a username and a password.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var accounts = await db.Accounts.AsNoTracking().ToListAsync();
        var account = accounts.FirstOrDefault(item =>
            string.Equals(item.Username, name, StringComparison.OrdinalIgnoreCase));

        if (account is null || !PasswordHasher.Verify(password, account.PasswordHash, account.PasswordSalt))
            throw new CinemaValidationException("That username or password is not recognised.");

        if (account.Role != expectedRole)
        {
            var door = expectedRole == AccountRole.Admin ? "Staff" : "Guest";
            var actual = account.Role == AccountRole.Admin ? "staff" : "guest";
            throw new CinemaValidationException($"This is a {actual} account. Use the {actual} sign-in, not {door}.");
        }

        return account;
    }

    public async Task<Account> RegisterGuestAsync(string username, string displayName, string password)
    {
        var name = username.Trim();
        var shown = displayName.Trim();

        if (name.Length is < 3 or > 40)
            throw new CinemaValidationException("Usernames need 3 to 40 characters.");

        if (shown.Length is < 2 or > 80)
            throw new CinemaValidationException("Enter the name that should appear on tickets.");

        if (password.Length < 6)
            throw new CinemaValidationException("Use a password of at least 6 characters.");

        if (!name.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-'))
            throw new CinemaValidationException("Usernames can use letters, numbers, dots, dashes, and underscores.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var names = await db.Accounts.Select(account => account.Username).ToListAsync();
        if (names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
            throw new CinemaValidationException("That username is already in use.");

        var (hash, salt) = PasswordHasher.Hash(password);
        var account = new Account
        {
            Username = name,
            DisplayName = shown,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = AccountRole.User
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }
}
