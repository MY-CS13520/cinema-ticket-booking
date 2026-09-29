namespace CinemaTicketBooking.Models;

/// <summary>Certificate shown on the schedule and the ticket.</summary>
public enum AgeRating
{
    G,
    PG,
    PG13,
    R,
    NC17
}

/// <summary>Presentation format of a hall. Drives the default ticket price in sample data.</summary>
public enum HallFormat
{
    Standard,
    Premium,
    Imax
}

/// <summary>A booking stays on file after cancellation so sales history is kept.</summary>
public enum BookingStatus
{
    Confirmed,
    Cancelled
}

/// <summary>How the guest paid at the desk.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    Online
}

/// <summary>Where the money is in its lifecycle.</summary>
public enum PaymentStatus
{
    Pending,
    Paid,
    Refunded
}
