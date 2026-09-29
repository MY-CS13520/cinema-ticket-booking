using System.Globalization;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Parses the free-text boxes on the editors without throwing.</summary>
internal static class InputParsing
{
    public static bool TryParseWholeNumber(string? text, out int value)
        => int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out value);

    public static bool TryParseMoney(string? text, out decimal value)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        var styles = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
        return decimal.TryParse(trimmed, styles, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryParseClock(string? text, out TimeOnly value)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        return TimeOnly.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out value)
            || TimeOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
