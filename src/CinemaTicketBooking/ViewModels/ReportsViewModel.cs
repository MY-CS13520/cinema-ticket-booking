using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Gross sales, refunds, and which films earned them between two dates.</summary>
public partial class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reports;

    public ReportsViewModel(IReportService reports)
    {
        _reports = reports;
        From = DateTime.Today.AddDays(-6);
        To = DateTime.Today;
    }

    public ObservableCollection<MovieSalesRow> ByMovie { get; } = [];

    public ObservableCollection<MethodSalesRow> ByMethod { get; } = [];

    [ObservableProperty]
    private DateTime? _from;

    [ObservableProperty]
    private DateTime? _to;

    [ObservableProperty]
    private string _ticketsSold = "—";

    [ObservableProperty]
    private string _bookingCount = "—";

    [ObservableProperty]
    private string _grossRevenue = "—";

    [ObservableProperty]
    private string _pendingAmount = "—";

    [ObservableProperty]
    private string _refunds = "—";

    [ObservableProperty]
    private string _netRevenue = "—";

    [ObservableProperty]
    private bool _hasMovies;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    protected override Task LoadAsync() => GuardAsync(async () =>
    {
        if (From is null || To is null)
            throw new CinemaValidationException("Choose a start and end date.");

        var report = await _reports.GetSalesAsync(From.Value, To.Value);
        TicketsSold = report.TicketsSold.ToString();
        BookingCount = report.BookingCount.ToString();
        GrossRevenue = report.GrossRevenue.ToString("C");
        PendingAmount = report.PendingAmount.ToString("C");
        Refunds = report.Refunds.ToString("C");
        NetRevenue = report.NetRevenue.ToString("C");

        ByMovie.Clear();
        foreach (var row in report.ByMovie)
            ByMovie.Add(row);

        ByMethod.Clear();
        foreach (var row in report.ByMethod)
            ByMethod.Add(row);

        HasMovies = ByMovie.Count > 0;
        ShowStatus($"Report for {From:dd MMM yyyy} to {To:dd MMM yyyy}.");
    });
}
