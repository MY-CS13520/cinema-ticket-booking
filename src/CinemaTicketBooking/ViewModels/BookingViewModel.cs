using System.Windows;
using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>
/// Pick a screening, tap seats, and take payment. Booked chairs are already sold.
/// </summary>
public partial class BookingViewModel : ViewModelBase
{
    private readonly IBookingService _bookings;
    private readonly DeskSession _session;
    private bool _suppressSelection;

    public BookingViewModel(IBookingService bookings, DeskSession session)
    {
        _bookings = bookings;
        _session = session;
        if (!_session.IsAdmin)
            CustomerName = _session.Current?.DisplayName ?? string.Empty;
    }

    public ObservableCollection<ShowtimeRow> Showtimes { get; } = [];

    public ObservableCollection<SeatRow> Rows { get; } = [];

    public IReadOnlyList<string> PaymentMethods { get; } = CinemaText.PaymentMethods;

    public IReadOnlyList<string> SaleStatuses { get; } = CinemaText.SaleStatuses;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private ShowtimeRow? _selectedShowtime;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _paymentMethod = "Card";

    [ObservableProperty]
    private string _paymentStatus = "Paid";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private int _selectedCount;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private string _seatSummary = "No seats selected";

    [ObservableProperty]
    private bool _hasShow;

    [ObservableProperty]
    private bool _hasShowtimes;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private void ToggleSeat(SeatItem? seat)
    {
        if (seat is null || seat.State == SeatState.Booked)
            return;

        seat.State = seat.State == SeatState.Selected ? SeatState.Available : SeatState.Selected;
        UpdateSelection();
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmAsync() => GuardAsync(async () =>
    {
        if (SelectedShowtime is null)
            throw new CinemaValidationException("Choose a screening.");

        if (!CinemaText.TryParseMethod(PaymentMethod, out var method))
            throw new CinemaValidationException("Choose how the guest is paying.");

        if (!CinemaText.TryParseStatus(PaymentStatus, out var status))
            throw new CinemaValidationException("Choose whether the payment is paid or still pending.");

        var picks = Rows
            .SelectMany(row => row.Seats)
            .Where(seat => seat.State == SeatState.Selected)
            .Select(seat => new SeatPick(seat.Row, seat.Number))
            .ToList();

        var receipt = await _bookings.CreateAsync(new BookingDraft
        {
            ShowtimeId = SelectedShowtime.Id,
            CustomerName = CustomerName,
            Phone = Phone,
            Email = Email,
            Method = method,
            Status = status,
            AccountId = _session.IsAdmin ? null : _session.Current?.Id,
            Seats = picks
        });

        var showId = SelectedShowtime.Id;
        CustomerName = _session.IsAdmin ? string.Empty : _session.Current?.DisplayName ?? string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        await ReloadAsync(showId);

        var seatList = string.Join(", ", receipt.Seats);
        ShowStatus($"Booked {receipt.BookingCode}.");
        MessageBox.Show(
            $"Booking {receipt.BookingCode}\n" +
            $"{receipt.MovieTitle}\n" +
            $"{receipt.HallName}\n" +
            $"{receipt.StartsAt:ddd dd MMM yyyy, HH:mm}\n" +
            $"Seats {seatList}\n" +
            $"Total {receipt.Total:C}\n" +
            $"{receipt.Method} · {receipt.Status}",
            "Ticket booked",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    });

    protected override Task LoadAsync() => GuardAsync(() => ReloadAsync(SelectedShowtime?.Id));

    partial void OnSelectedShowtimeChanged(ShowtimeRow? value)
    {
        HasShow = value is not null;
        if (_suppressSelection)
            return;

        _ = LoadMapAsync(value);
    }

    private bool CanConfirm()
        => SelectedShowtime is not null && SelectedCount > 0 && CustomerName.Trim().Length >= 2;

    private async Task ReloadAsync(int? selectId)
    {
        var shows = await _bookings.GetBookableShowtimesAsync();
        _suppressSelection = true;
        Showtimes.Clear();
        foreach (var show in shows)
            Showtimes.Add(show);

        SelectedShowtime = Showtimes.FirstOrDefault(show => show.Id == selectId) ?? Showtimes.FirstOrDefault();
        _suppressSelection = false;
        HasShowtimes = Showtimes.Count > 0;
        await LoadMapAsync(SelectedShowtime);
    }

    private async Task LoadMapAsync(ShowtimeRow? show)
    {
        Rows.Clear();
        UpdateSelection();
        if (show is null)
            return;

        try
        {
            var map = await _bookings.GetSeatMapAsync(show.Id);
            if (SelectedShowtime?.Id != show.Id)
                return;

            for (var index = 0; index < map.Showtime.RowCount; index++)
            {
                var label = ((char)('A' + index)).ToString();
                var seats = new ObservableCollection<SeatItem>();
                for (var number = 1; number <= map.Showtime.SeatsPerRow; number++)
                {
                    var taken = map.TakenKeys.Contains(SeatKey.Of(label, number));
                    seats.Add(new SeatItem
                    {
                        Row = label,
                        Number = number,
                        State = taken ? SeatState.Booked : SeatState.Available
                    });
                }

                Rows.Add(new SeatRow { Label = label, Seats = seats });
            }

            UpdateSelection();
        }
        catch (CinemaValidationException ex)
        {
            Warn(ex.Message);
        }
    }

    private void UpdateSelection()
    {
        var picked = Rows.SelectMany(row => row.Seats).Where(seat => seat.State == SeatState.Selected).ToList();
        SelectedCount = picked.Count;
        TotalAmount = (SelectedShowtime?.TicketPrice ?? 0m) * picked.Count;
        SeatSummary = picked.Count == 0
            ? "No seats selected"
            : string.Join(", ", picked.Select(seat => $"{seat.Row}{seat.Number}"));
    }
}
