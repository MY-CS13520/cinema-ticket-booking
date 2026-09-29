namespace CinemaTicketBooking.Views;

public partial class ReportsPage
{
    public ReportsPage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.ReportsViewModel>(this);
    }
}
