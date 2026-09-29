namespace CinemaTicketBooking.Views;

public partial class DashboardPage
{
    public DashboardPage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.DashboardViewModel>(this);
    }
}
