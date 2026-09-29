namespace CinemaTicketBooking.ViewModels;

/// <summary>How a chair is drawn on the seat map.</summary>
public enum SeatState
{
    Available,
    Selected,
    Booked
}

/// <summary>One chair. State changes when the guest taps it, and the button restyles itself.</summary>
public partial class SeatItem : ObservableObject
{
    public string Row { get; set; } = string.Empty;

    public int Number { get; set; }

    [ObservableProperty]
    private SeatState _state;
}

/// <summary>One lettered row on the seat map.</summary>
public sealed class SeatRow
{
    public required string Label { get; init; }

    public required ObservableCollection<SeatItem> Seats { get; init; }
}
