namespace CinemaTicketBooking.Views;

public partial class BookingsPage
{
    public BookingsPage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.BookingsViewModel>(this);
    }
}
