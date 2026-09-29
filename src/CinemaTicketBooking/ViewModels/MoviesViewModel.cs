using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Create, update, and delete films in the catalogue.</summary>
public partial class MoviesViewModel : ViewModelBase
{
    private readonly IMovieService _movies;
    private readonly List<Movie> _all = [];
    private int? _editingId;
    private bool _suppressSelection;

    public MoviesViewModel(IMovieService movies)
    {
        _movies = movies;
    }

    public ObservableCollection<Movie> Movies { get; } = [];

    public IReadOnlyList<string> AgeRatings { get; } = CinemaText.AgeRatings;

    [ObservableProperty]
    private Movie? _selectedMovie;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _editorHeading = "New film";

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _genre = string.Empty;

    [ObservableProperty]
    private string _director = string.Empty;

    [ObservableProperty]
    private string _language = "English";

    [ObservableProperty]
    private string _ageRating = "PG-13";

    [ObservableProperty]
    private string _durationText = "120";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isOnBoard = true;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private void NewMovie()
    {
        _suppressSelection = true;
        SelectedMovie = null;
        _suppressSelection = false;
        ClearForm();
    }

    [RelayCommand]
    private Task SaveAsync() => GuardAsync(async () =>
    {
        if (!InputParsing.TryParseWholeNumber(DurationText, out var minutes))
            throw new CinemaValidationException("Enter the duration as a whole number of minutes.");

        if (!CinemaText.TryParseAge(AgeRating, out var rating))
            throw new CinemaValidationException("Choose a certificate.");

        var id = await _movies.SaveAsync(new MovieDraft
        {
            Id = _editingId,
            Title = Title,
            Genre = Genre,
            Director = Director,
            Language = Language,
            AgeRating = rating,
            DurationMinutes = minutes,
            Description = Description,
            IsActive = IsOnBoard
        });

        await ReloadAsync(id);
        ShowStatus("Film saved.");
    });

    [RelayCommand]
    private Task DeleteAsync() => GuardAsync(async () =>
    {
        if (_editingId is not int id)
            throw new CinemaValidationException("Select a film to delete.");

        if (!Confirm($"Delete \"{Title}\" from the catalogue?"))
            return;

        await _movies.DeleteAsync(id);
        ClearForm();
        await ReloadAsync(null);
        ShowStatus("Film deleted.");
    });

    protected override Task LoadAsync() => GuardAsync(() => ReloadAsync(_editingId));

    partial void OnSearchTextChanged(string value) => ApplyFilter(_editingId);

    partial void OnSelectedMovieChanged(Movie? value)
    {
        if (_suppressSelection || value is null)
            return;

        LoadForm(value);
    }

    private async Task ReloadAsync(int? selectId)
    {
        var movies = await _movies.GetAllAsync();
        _all.Clear();
        _all.AddRange(movies);
        ApplyFilter(selectId);
    }

    private void ApplyFilter(int? selectId)
    {
        var needle = SearchText.Trim();
        var matches = _all.Where(movie =>
            needle.Length == 0
            || movie.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || movie.Genre.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || movie.Director.Contains(needle, StringComparison.OrdinalIgnoreCase));

        _suppressSelection = true;
        Movies.Clear();
        foreach (var movie in matches)
            Movies.Add(movie);

        SelectedMovie = selectId is int id ? Movies.FirstOrDefault(movie => movie.Id == id) : null;
        _suppressSelection = false;

        if (SelectedMovie is not null)
            LoadForm(SelectedMovie);
    }

    private void LoadForm(Movie movie)
    {
        _editingId = movie.Id;
        EditorHeading = "Edit film";
        Title = movie.Title;
        Genre = movie.Genre;
        Director = movie.Director;
        Language = movie.Language;
        AgeRating = movie.RatingLabel;
        DurationText = movie.DurationMinutes.ToString();
        Description = movie.Description;
        IsOnBoard = movie.IsActive;
    }

    private void ClearForm()
    {
        _editingId = null;
        EditorHeading = "New film";
        Title = string.Empty;
        Genre = string.Empty;
        Director = string.Empty;
        Language = "English";
        AgeRating = "PG-13";
        DurationText = "120";
        Description = string.Empty;
        IsOnBoard = true;
    }
}
