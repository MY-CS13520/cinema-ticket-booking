using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;
using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>
/// Hall layout and the screening board. A hall's row count is the seat map used at the desk.
/// </summary>
public partial class ScheduleViewModel : ViewModelBase
{
    private readonly IScheduleService _schedule;
    private readonly IMovieService _movies;
    private int? _editingHallId;
    private int? _editingShowId;
    private bool _suppressSelection;

    public ScheduleViewModel(IScheduleService schedule, IMovieService movies)
    {
        _schedule = schedule;
        _movies = movies;
        ShowDate = DateTime.Today;
    }

    public ObservableCollection<HallRow> Halls { get; } = [];

    public ObservableCollection<Movie> MovieChoices { get; } = [];

    public ObservableCollection<ShowtimeRow> Showtimes { get; } = [];

    public IReadOnlyList<string> HallFormats { get; } = CinemaText.HallFormats;

    [ObservableProperty]
    private HallRow? _selectedHall;

    [ObservableProperty]
    private ShowtimeRow? _selectedShow;

    [ObservableProperty]
    private bool _upcomingOnly = true;

    [ObservableProperty]
    private string _hallHeading = "New hall";

    [ObservableProperty]
    private string _hallName = string.Empty;

    [ObservableProperty]
    private string _rowCountText = "8";

    [ObservableProperty]
    private string _seatsPerRowText = "12";

    [ObservableProperty]
    private string _hallFormat = "Standard";

    [ObservableProperty]
    private string _showHeading = "New screening";

    [ObservableProperty]
    private Movie? _selectedMovieChoice;

    [ObservableProperty]
    private HallRow? _selectedHallChoice;

    [ObservableProperty]
    private DateTime? _showDate;

    [ObservableProperty]
    private string _timeText = "19:30";

    [ObservableProperty]
    private string _priceText = "12.50";

    [ObservableProperty]
    private bool _hasShows;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private void NewHall()
    {
        _suppressSelection = true;
        SelectedHall = null;
        _suppressSelection = false;
        ClearHallForm();
    }

    [RelayCommand]
    private void NewShow()
    {
        _suppressSelection = true;
        SelectedShow = null;
        _suppressSelection = false;
        ClearShowForm();
    }

    [RelayCommand]
    private Task SaveHallAsync() => GuardAsync(async () =>
    {
        if (!InputParsing.TryParseWholeNumber(RowCountText, out var rows)
            || !InputParsing.TryParseWholeNumber(SeatsPerRowText, out var seats))
        {
            throw new CinemaValidationException("Enter the row count and seats per row as whole numbers.");
        }

        if (!CinemaText.TryParseFormat(HallFormat, out var format))
            throw new CinemaValidationException("Choose a hall format.");

        var id = await _schedule.SaveHallAsync(new HallDraft
        {
            Id = _editingHallId,
            Name = HallName,
            RowCount = rows,
            SeatsPerRow = seats,
            Format = format
        });

        await ReloadHallsAsync(id);
        ShowStatus("Hall saved.");
    });

    [RelayCommand]
    private Task DeleteHallAsync() => GuardAsync(async () =>
    {
        if (_editingHallId is not int id)
            throw new CinemaValidationException("Select a hall to delete.");

        if (!Confirm($"Delete {HallName}?"))
            return;

        await _schedule.DeleteHallAsync(id);
        ClearHallForm();
        await ReloadHallsAsync(null);
        ShowStatus("Hall deleted.");
    });

    [RelayCommand]
    private Task SaveShowAsync() => GuardAsync(async () =>
    {
        if (SelectedMovieChoice is null)
            throw new CinemaValidationException("Choose a film.");

        if (SelectedHallChoice is null)
            throw new CinemaValidationException("Choose a hall.");

        if (ShowDate is null)
            throw new CinemaValidationException("Choose a date.");

        if (!InputParsing.TryParseClock(TimeText, out var time))
            throw new CinemaValidationException("Enter a start time such as 19:30.");

        if (!InputParsing.TryParseMoney(PriceText, out var price))
            throw new CinemaValidationException("Enter the ticket price.");

        var id = await _schedule.SaveShowtimeAsync(new ShowtimeDraft
        {
            Id = _editingShowId,
            MovieId = SelectedMovieChoice.Id,
            HallId = SelectedHallChoice.Id,
            StartsAt = ShowDate.Value.Date.Add(time.ToTimeSpan()),
            TicketPrice = price
        });

        await ReloadShowsAsync(id);
        ShowStatus("Screening saved.");
    });

    [RelayCommand]
    private Task CancelShowAsync() => GuardAsync(async () =>
    {
        if (_editingShowId is not int id)
            throw new CinemaValidationException("Select a screening to cancel.");

        if (!Confirm("Cancel this screening? Sold tickets must be cancelled first."))
            return;

        await _schedule.CancelShowtimeAsync(id);
        await ReloadShowsAsync(id);
        ShowStatus("Screening cancelled.");
    });

    [RelayCommand]
    private Task DeleteShowAsync() => GuardAsync(async () =>
    {
        if (_editingShowId is not int id)
            throw new CinemaValidationException("Select a screening to delete.");

        if (!Confirm("Delete this screening from the board?"))
            return;

        await _schedule.DeleteShowtimeAsync(id);
        ClearShowForm();
        await ReloadShowsAsync(null);
        ShowStatus("Screening deleted.");
    });

    protected override Task LoadAsync() => GuardAsync(async () =>
    {
        await ReloadHallsAsync(_editingHallId);
        await ReloadMoviesAsync();
        await ReloadShowsAsync(_editingShowId);
    });

    partial void OnUpcomingOnlyChanged(bool value)
    {
        if (_suppressSelection)
            return;

        _ = GuardAsync(() => ReloadShowsAsync(_editingShowId));
    }

    partial void OnSelectedHallChanged(HallRow? value)
    {
        if (_suppressSelection || value is null)
            return;

        _editingHallId = value.Id;
        HallHeading = "Edit hall";
        HallName = value.Name;
        RowCountText = value.RowCount.ToString();
        SeatsPerRowText = value.SeatsPerRow.ToString();
        HallFormat = value.Format;
    }

    partial void OnSelectedShowChanged(ShowtimeRow? value)
    {
        if (_suppressSelection || value is null)
            return;

        _editingShowId = value.Id;
        ShowHeading = "Edit screening";
        ShowDate = value.StartsAt.Date;
        TimeText = value.StartsAt.ToString("HH:mm");
        PriceText = value.TicketPrice.ToString("0.00");
        SelectedMovieChoice = MovieChoices.FirstOrDefault(movie => movie.Id == value.MovieId);
        SelectedHallChoice = Halls.FirstOrDefault(hall => hall.Id == value.HallId);
    }

    private async Task ReloadHallsAsync(int? selectId)
    {
        var halls = await _schedule.GetHallsAsync();
        _suppressSelection = true;
        Halls.Clear();
        foreach (var hall in halls)
            Halls.Add(hall);

        SelectedHall = selectId is int id ? Halls.FirstOrDefault(hall => hall.Id == id) : null;
        _suppressSelection = false;

        if (SelectedHall is not null)
            OnSelectedHallChanged(SelectedHall);
        else
            ClearHallForm();
    }

    private async Task ReloadMoviesAsync()
    {
        var currentId = SelectedMovieChoice?.Id;
        var movies = await _movies.GetAllAsync();
        MovieChoices.Clear();
        foreach (var movie in movies.Where(movie => movie.IsActive))
            MovieChoices.Add(movie);

        SelectedMovieChoice = MovieChoices.FirstOrDefault(movie => movie.Id == currentId) ?? MovieChoices.FirstOrDefault();
    }

    private async Task ReloadShowsAsync(int? selectId)
    {
        var shows = await _schedule.GetShowtimesAsync(UpcomingOnly);
        _suppressSelection = true;
        Showtimes.Clear();
        foreach (var show in shows)
            Showtimes.Add(show);

        SelectedShow = selectId is int id ? Showtimes.FirstOrDefault(show => show.Id == id) : null;
        _suppressSelection = false;
        HasShows = Showtimes.Count > 0;

        if (SelectedShow is not null)
            OnSelectedShowChanged(SelectedShow);
        else
            ClearShowForm();
    }

    private void ClearHallForm()
    {
        _editingHallId = null;
        HallHeading = "New hall";
        HallName = string.Empty;
        RowCountText = "8";
        SeatsPerRowText = "12";
        HallFormat = "Standard";
    }

    private void ClearShowForm()
    {
        _editingShowId = null;
        ShowHeading = "New screening";
        ShowDate = DateTime.Today;
        TimeText = "19:30";
        PriceText = "12.50";
        SelectedMovieChoice = MovieChoices.FirstOrDefault();
        SelectedHallChoice = Halls.FirstOrDefault();
    }
}
