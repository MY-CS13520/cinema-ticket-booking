using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Find a ticket and cancel it. Cancelling releases the chairs.</summary>
public partial class BookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookings;

    public BookingsViewModel(IBookingService bookings)
    {
        _bookings = bookings;
    }

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

        await _bookings.CancelAsync(booking.Id);
        await ReloadAsync();
        ShowStatus($"{booking.BookingCode} cancelled.");
    });

    protected override Task LoadAsync() => GuardAsync(ReloadAsync);

    private bool CanCancel() => SelectedBooking?.CanCancel == true;

    private async Task ReloadAsync()
    {
        var rows = await _bookings.SearchAsync(SearchText);
        var selectedId = SelectedBooking?.Id;
        Bookings.Clear();
        foreach (var row in rows)
            Bookings.Add(row);

        SelectedBooking = Bookings.FirstOrDefault(row => row.Id == selectedId);
        HasBookings = Bookings.Count > 0;
    }
}
