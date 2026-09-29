using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Find a ticket and cancel it. Cancelling releases the chairs.</summary>
public partial class BookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookings;
    private readonly DeskSession _session;

    public BookingsViewModel(IBookingService bookings, DeskSession session)
    {
        _bookings = bookings;
        _session = session;
    }

    public string Heading => _session.IsAdmin ? "Bookings" : "My bookings";

    public string Subtitle => _session.IsAdmin
        ? "Find a ticket and cancel it if the guest is not coming. Seats go back on sale."
        : "Tickets bought with this guest account. Cancelling one puts the seats back on sale.";

    public ObservableCollection<BookingRow> Bookings { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private BookingRow? _selectedBooking;

    [ObservableProperty]
    private bool _hasBookings;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelAsync() => GuardAsync(async () =>
    {
        if (SelectedBooking is not BookingRow booking)
            throw new CinemaValidationException("Select a booking.");

        if (!Confirm($"Cancel {booking.BookingCode} for {booking.CustomerName}? The seats go back on sale. A paid ticket is refunded."))
            return;

        await _bookings.CancelAsync(booking.Id, _session.IsAdmin ? null : _session.Current?.Id);
        await ReloadAsync();
        ShowStatus($"{booking.BookingCode} cancelled.");
    });

    protected override Task LoadAsync() => GuardAsync(ReloadAsync);

    private bool CanCancel() => SelectedBooking?.CanCancel == true;

    private async Task ReloadAsync()
    {
        var ownerId = _session.IsAdmin ? null : _session.Current?.Id;
        var rows = await _bookings.SearchAsync(SearchText, ownerId);
        var selectedId = SelectedBooking?.Id;
        Bookings.Clear();
        foreach (var row in rows)
            Bookings.Add(row);

        SelectedBooking = Bookings.FirstOrDefault(row => row.Id == selectedId);
        HasBookings = Bookings.Count > 0;
    }
}
