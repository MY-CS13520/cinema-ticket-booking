using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Today's revenue, open seats, and the next screenings.</summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IReportService _reports;

    public DashboardViewModel(IReportService reports)
    {
        _reports = reports;
    }

    public ObservableCollection<ShowtimeRow> Upcoming { get; } = [];

    public ObservableCollection<BookingRow> RecentBookings { get; } = [];

    public string DatabasePath { get; } = AppPaths.DatabaseFile;

    [ObservableProperty]
    private string _revenueToday = "—";

    [ObservableProperty]
    private string _ticketsToday = "—";

    [ObservableProperty]
    private string _showsToday = "—";

    [ObservableProperty]
    private string _seatsLeftToday = "—";

    [ObservableProperty]
    private bool _hasUpcoming;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    protected override Task LoadAsync() => GuardAsync(async () =>
    {
        var data = await _reports.GetDashboardAsync();
        RevenueToday = data.RevenueToday.ToString("C");
        TicketsToday = data.TicketsToday.ToString();
        ShowsToday = data.ShowsToday.ToString();
        SeatsLeftToday = data.SeatsLeftToday.ToString();

        Upcoming.Clear();
        foreach (var show in data.Upcoming)
            Upcoming.Add(show);

        RecentBookings.Clear();
        foreach (var booking in data.RecentBookings)
            RecentBookings.Add(booking);

        HasUpcoming = Upcoming.Count > 0;
    });
}
