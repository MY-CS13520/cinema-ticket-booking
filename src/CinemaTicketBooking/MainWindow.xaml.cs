using CinemaTicketBooking.Views;

namespace CinemaTicketBooking;

/// <summary>
/// The shell. The navigation pane opens each desk screen.
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
    }
}
