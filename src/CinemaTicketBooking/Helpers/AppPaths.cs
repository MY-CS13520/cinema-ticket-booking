namespace CinemaTicketBooking.Helpers;

/// <summary>
/// Locations the desk application owns on this computer.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// SQLite file under the current user's local app data.
    /// The folder is created the first time the path is read.
    /// </summary>
    public static string DatabaseFile
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LuminaCinema");

            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "cinema.db");
        }
    }
}
