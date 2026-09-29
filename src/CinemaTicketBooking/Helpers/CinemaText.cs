using CinemaTicketBooking.Models;

namespace CinemaTicketBooking.Helpers;

/// <summary>
/// Short labels shared by the catalogue, the schedule, and the ticket slip.
/// </summary>
public static class CinemaText
{
    public static IReadOnlyList<string> AgeRatings { get; } = ["G", "PG", "PG-13", "R", "NC-17"];

    public static IReadOnlyList<string> HallFormats { get; } = ["Standard", "Premium", "IMAX"];

    public static IReadOnlyList<string> PaymentMethods { get; } = ["Cash", "Card", "Online"];

    /// <summary>Statuses the desk can choose while selling. Refunds happen later, from the payments screen.</summary>
    public static IReadOnlyList<string> SaleStatuses { get; } = ["Paid", "Pending"];

    public static string Age(AgeRating rating) => rating switch
    {
        AgeRating.PG13 => "PG-13",
        AgeRating.NC17 => "NC-17",
        _ => rating.ToString()
    };

    public static string Format(HallFormat format) => format == HallFormat.Imax ? "IMAX" : format.ToString();

    public static bool TryParseAge(string? label, out AgeRating rating)
    {
        rating = label?.Trim().ToUpperInvariant() switch
        {
            "G" => AgeRating.G,
            "PG" => AgeRating.PG,
            "PG-13" or "PG13" => AgeRating.PG13,
            "R" => AgeRating.R,
            "NC-17" or "NC17" => AgeRating.NC17,
            _ => (AgeRating)(-1)
        };

        return Enum.IsDefined(rating);
    }

    public static bool TryParseFormat(string? label, out HallFormat format)
    {
        format = label?.Trim().ToUpperInvariant() switch
        {
            "STANDARD" => HallFormat.Standard,
            "PREMIUM" => HallFormat.Premium,
            "IMAX" => HallFormat.Imax,
            _ => (HallFormat)(-1)
        };

        return Enum.IsDefined(format);
    }

    public static bool TryParseMethod(string? label, out PaymentMethod method)
        => Enum.TryParse(label, ignoreCase: true, out method) && Enum.IsDefined(method);

    public static bool TryParseStatus(string? label, out PaymentStatus status)
        => Enum.TryParse(label, ignoreCase: true, out status) && Enum.IsDefined(status);
}
