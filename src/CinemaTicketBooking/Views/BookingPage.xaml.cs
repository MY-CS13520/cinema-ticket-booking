namespace CinemaTicketBooking.Views;

public partial class BookingPage
{
    public BookingPage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.BookingViewModel>(this);
    }
}
