using CinemaTicketBooking.Models;

namespace CinemaTicketBooking.Services;

/// <summary>Fields the movies screen edits. A null <see cref="Id"/> means a new film.</summary>
public sealed class MovieDraft
{
    public int? Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Director { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public AgeRating AgeRating { get; set; } = AgeRating.PG13;
    public int DurationMinutes { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Fields the hall editor edits.</summary>
public sealed class HallDraft
{
    public int? Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int SeatsPerRow { get; set; }
    public HallFormat Format { get; set; } = HallFormat.Standard;
}

/// <summary>Fields the showtime editor edits.</summary>
public sealed class ShowtimeDraft
{
    public int? Id { get; init; }
    public int MovieId { get; set; }
    public int HallId { get; set; }
    public DateTime StartsAt { get; set; }
    public decimal TicketPrice { get; set; }
}

/// <summary>What the booking desk sends when the guest confirms.</summary>
public sealed class BookingDraft
{
    public int ShowtimeId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public int? AccountId { get; init; }
    public IReadOnlyList<SeatPick> Seats { get; init; } = [];
}

/// <summary>A chair the guest selected on the seat map.</summary>
public sealed record SeatPick(string RowLabel, int SeatNumber);

/// <summary>A screening row for the schedule, dashboard, and booking list.</summary>
public sealed class ShowtimeRow
{
    public int Id { get; init; }
    public int MovieId { get; init; }
    public int HallId { get; init; }
    public string MovieTitle { get; init; } = string.Empty;
    public string Genre { get; init; } = string.Empty;
    public string AgeRating { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string HallName { get; init; } = string.Empty;
    public string HallFormat { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public int SeatsPerRow { get; init; }
    public DateTime StartsAt { get; init; }
    public decimal TicketPrice { get; init; }
    public int SeatsTaken { get; init; }
    public bool IsCancelled { get; init; }

    public int SeatCapacity => RowCount * SeatsPerRow;
    public int SeatsLeft => Math.Max(0, SeatCapacity - SeatsTaken);
    public string WhenLabel => StartsAt.ToString("ddd dd MMM yyyy, HH:mm");
    public string OccupancyLabel => IsCancelled ? "Cancelled" : $"{SeatsTaken}/{SeatCapacity} taken";
}

/// <summary>A hall row for the schedule screen.</summary>
public sealed class HallRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public int SeatsPerRow { get; init; }
    public string Format { get; init; } = string.Empty;
    public int Capacity => RowCount * SeatsPerRow;
}

/// <summary>Occupied chairs for one screening, plus the hall size used to draw the map.</summary>
public sealed class SeatMapData
{
    public required ShowtimeRow Showtime { get; init; }
    public required IReadOnlySet<string> TakenKeys { get; init; }
}

/// <summary>A booking line for the list and the dashboard.</summary>
public sealed class BookingRow
{
    public int Id { get; init; }
    public string BookingCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MovieTitle { get; init; } = string.Empty;
    public string HallName { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public string SeatSummary { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime BookedAt { get; init; }
    public bool CanCancel { get; init; }
}

/// <summary>The slip shown after a successful booking.</summary>
public sealed class TicketReceipt
{
    public required string BookingCode { get; init; }
    public required string MovieTitle { get; init; }
    public required string HallName { get; init; }
    public required DateTime StartsAt { get; init; }
    public required IReadOnlyList<string> Seats { get; init; }
    public required decimal Total { get; init; }
    public required string Method { get; init; }
    public required string Status { get; init; }
}

/// <summary>A payment line for the tracking screen.</summary>
public sealed class PaymentRow
{
    public int Id { get; init; }
    public string BookingCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string MovieTitle { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
    public DateTime? RefundedAt { get; init; }
    public bool CanMarkPaid { get; init; }
    public bool CanRefund { get; init; }
}

/// <summary>Sales for one film inside a report range.</summary>
public sealed class MovieSalesRow
{
    public required string Title { get; init; }
    public int Tickets { get; init; }
    public decimal Revenue { get; init; }

    /// <summary>Bar width relative to the strongest film in the report, from 0 to 100.</summary>
    public double Share { get; set; }
}

/// <summary>Sales for one payment method inside a report range.</summary>
public sealed class MethodSalesRow
{
    public required string Method { get; init; }
    public int Payments { get; init; }
    public decimal Revenue { get; init; }

    /// <summary>Bar width relative to the strongest method in the report, from 0 to 100.</summary>
    public double Share { get; set; }
}

/// <summary>The sales report for an inclusive date range.</summary>
public sealed class SalesReport
{
    public int TicketsSold { get; init; }
    public int BookingCount { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal PendingAmount { get; init; }
    public decimal Refunds { get; init; }
    public decimal NetRevenue { get; init; }
    public IReadOnlyList<MovieSalesRow> ByMovie { get; init; } = [];
    public IReadOnlyList<MethodSalesRow> ByMethod { get; init; } = [];
}

/// <summary>Figures for the home screen.</summary>
public sealed class DashboardData
{
    public decimal RevenueToday { get; init; }
    public int TicketsToday { get; init; }
    public int ShowsToday { get; init; }
    public int SeatsLeftToday { get; init; }
    public IReadOnlyList<ShowtimeRow> Upcoming { get; init; } = [];
    public IReadOnlyList<BookingRow> RecentBookings { get; init; } = [];
}
