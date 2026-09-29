using CinemaTicketBooking.Services;
using CinemaTicketBooking.Views;
using Wpf.Ui.Controls;

namespace CinemaTicketBooking;

/// <summary>
/// The shell. Staff see the full desk. Guests see booking only.
/// The title strip sits above the navigation so the pane button can be clicked.
/// </summary>
public partial class MainWindow
{
    private readonly DeskSession _session;

    public MainWindow()
    {
        InitializeComponent();
        _session = App.GetRequiredService<DeskSession>();
        BuildMenu();
        Loaded += (_, _) => NavigateHome();
    }

    private void BuildMenu()
    {
        RootNavigation.MenuItems.Clear();
        RootNavigation.FooterMenuItems.Clear();

        var account = _session.Current;
        SignedInText.Text = account is null
            ? "Not signed in"
            : _session.IsAdmin
                ? $"Staff · {account.DisplayName}"
                : $"Guest · {account.DisplayName}";

        if (_session.IsAdmin)
        {
            Add(RootNavigation.MenuItems, "Dashboard", SymbolRegular.Home24, typeof(DashboardPage));
            Add(RootNavigation.MenuItems, "Movies", SymbolRegular.Filmstrip24, typeof(MoviesPage));
            Add(RootNavigation.MenuItems, "Schedule", SymbolRegular.CalendarLtr24, typeof(SchedulePage));
            Add(RootNavigation.MenuItems, "Book tickets", SymbolRegular.TicketHorizontal24, typeof(BookingPage));
            Add(RootNavigation.MenuItems, "Bookings", SymbolRegular.Receipt24, typeof(BookingsPage));
            Add(RootNavigation.MenuItems, "Payments", SymbolRegular.Payment24, typeof(PaymentsPage));
            Add(RootNavigation.MenuItems, "Sales reports", SymbolRegular.DataHistogram24, typeof(ReportsPage));
        }
        else
        {
            Add(RootNavigation.MenuItems, "Book tickets", SymbolRegular.TicketHorizontal24, typeof(BookingPage));
            Add(RootNavigation.MenuItems, "My bookings", SymbolRegular.Receipt24, typeof(BookingsPage));
            Add(RootNavigation.MenuItems, "Movies", SymbolRegular.Filmstrip24, typeof(MoviesPage));
        }

        var signOut = new NavigationViewItem
        {
            Content = "Sign out",
            Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowExit24 }
        };
        signOut.Click += (_, _) => SignOut();
        RootNavigation.FooterMenuItems.Add(signOut);
    }

    private static void Add(System.Collections.IList items, string label, SymbolRegular symbol, Type page)
    {
        items.Add(new NavigationViewItem
        {
            Content = label,
            Icon = new SymbolIcon { Symbol = symbol },
            TargetPageType = page
        });
    }

    private void NavigateHome()
    {
        var home = _session.IsAdmin ? typeof(DashboardPage) : typeof(BookingPage);
        RootNavigation.Navigate(home);
    }

    private void SignOut()
    {
        _session.SignOut();
        Hide();

        var login = new LoginWindow();
        var signedIn = login.ShowDialog() == true;
        if (!signedIn)
        {
            Close();
            return;
        }

        BuildMenu();
        Show();
        NavigateHome();
    }
}
