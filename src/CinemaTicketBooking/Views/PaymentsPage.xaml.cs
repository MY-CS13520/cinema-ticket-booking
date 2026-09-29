namespace CinemaTicketBooking.Views;

public partial class PaymentsPage
{
    public PaymentsPage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.PaymentsViewModel>(this);
    }
}
