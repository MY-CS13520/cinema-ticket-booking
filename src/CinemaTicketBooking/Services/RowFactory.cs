using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Models;

namespace CinemaTicketBooking.Services;

/// <summary>
/// Turns tracked entities into the flat rows the screens bind to.
/// </summary>
internal static class RowFactory
{
    public static ShowtimeRow Showtime(Showtime show, int seatsTaken) => new()
    {
        Id = show.Id,
        MovieId = show.MovieId,
        HallId = show.HallId,
        MovieTitle = show.Movie.Title,
        Genre = show.Movie.Genre,
        AgeRating = CinemaText.Age(show.Movie.AgeRating),
        DurationMinutes = show.Movie.DurationMinutes,
        HallName = show.Hall.Name,
        HallFormat = CinemaText.Format(show.Hall.Format),
        RowCount = show.Hall.RowCount,
        SeatsPerRow = show.Hall.SeatsPerRow,
        StartsAt = show.StartsAt,
        TicketPrice = show.TicketPrice,
        SeatsTaken = seatsTaken,
        IsCancelled = show.IsCancelled
    };

    public static BookingRow Booking(Booking booking)
    {
        var seats = booking.Seats
            .OrderBy(seat => seat.RowLabel)
            .ThenBy(seat => seat.SeatNumber)
            .Select(seat => seat.Label);

        return new BookingRow
        {
            Id = booking.Id,
            BookingCode = booking.BookingCode,
            CustomerName = booking.CustomerName,
            Phone = booking.Phone,
            Email = booking.Email,
            MovieTitle = booking.Showtime.Movie.Title,
            HallName = booking.Showtime.Hall.Name,
            StartsAt = booking.Showtime.StartsAt,
            SeatSummary = string.Join(", ", seats),
            TotalAmount = booking.TotalAmount,
            Status = booking.Status.ToString(),
            BookedAt = booking.BookedAt,
            CanCancel = booking.Status == BookingStatus.Confirmed
        };
    }

    public static PaymentRow Payment(Payment payment)
    {
        var booking = payment.Booking;
        var voided = payment.Status == PaymentStatus.Refunded && payment.PaidAt is null;

        return new PaymentRow
        {
            Id = payment.Id,
            BookingCode = booking.BookingCode,
            CustomerName = booking.CustomerName,
            MovieTitle = booking.Showtime.Movie.Title,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            Status = voided ? "Voided" : payment.Status.ToString(),
            PaidAt = payment.PaidAt,
            RefundedAt = payment.RefundedAt,
            CanMarkPaid = payment.Status == PaymentStatus.Pending && booking.Status == BookingStatus.Confirmed,
            CanRefund = payment.Status == PaymentStatus.Paid && booking.Status == BookingStatus.Confirmed
        };
    }
}
