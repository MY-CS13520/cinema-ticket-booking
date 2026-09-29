using CinemaTicketBooking.Data;
using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Services;

/// <summary>Catalogue of films the cinema can put on a screen.</summary>
public interface IMovieService
{
    Task<IReadOnlyList<Movie>> GetAllAsync();

    /// <returns>The saved movie id.</returns>
    Task<int> SaveAsync(MovieDraft draft);

    Task DeleteAsync(int id);
}

/// <inheritdoc cref="IMovieService"/>
public sealed class MovieService : IMovieService
{
    private readonly IDbContextFactory<CinemaDbContext> _dbFactory;

    public MovieService(IDbContextFactory<CinemaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<Movie>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Movies
            .AsNoTracking()
            .OrderBy(movie => movie.Title)
            .ToListAsync();
    }

    public async Task<int> SaveAsync(MovieDraft draft)
    {
        var title = Required(draft.Title, "Enter the film title.");
        var genre = Required(draft.Genre, "Enter a genre.");
        var language = Required(draft.Language, "Enter the language.");

        if (title.Length > 120)
            throw new CinemaValidationException("Keep the title under 120 characters.");

        if (draft.DurationMinutes is < 30 or > 300)
            throw new CinemaValidationException("Duration should be between 30 and 300 minutes.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var titles = await db.Movies
            .Where(movie => movie.Id != (draft.Id ?? 0))
            .Select(movie => movie.Title)
            .ToListAsync();

        if (titles.Any(existing => string.Equals(existing, title, StringComparison.OrdinalIgnoreCase)))
            throw new CinemaValidationException("A film with that title is already in the catalogue.");

        Movie movie;
        if (draft.Id is int id)
        {
            movie = await db.Movies.FirstOrDefaultAsync(item => item.Id == id)
                ?? throw new CinemaValidationException("That film is no longer in the catalogue.");
        }
        else
        {
            movie = new Movie();
            db.Movies.Add(movie);
        }

        movie.Title = title;
        movie.Genre = genre;
        movie.Director = draft.Director.Trim();
        movie.Language = language;
        movie.AgeRating = draft.AgeRating;
        movie.DurationMinutes = draft.DurationMinutes;
        movie.Description = draft.Description.Trim();
        movie.IsActive = draft.IsActive;

        await db.SaveChangesAsync();
        return movie.Id;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var movie = await db.Movies.FirstOrDefaultAsync(item => item.Id == id)
            ?? throw new CinemaValidationException("That film is no longer in the catalogue.");

        if (await db.Showtimes.AnyAsync(show => show.MovieId == id))
        {
            throw new CinemaValidationException(
                "This film has showtimes. Remove those screenings first, or turn off \"On the board\" so it cannot be scheduled again.");
        }

        db.Movies.Remove(movie);
        await db.SaveChangesAsync();
    }

    private static string Required(string? value, string message)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
            throw new CinemaValidationException(message);

        return text;
    }
}
