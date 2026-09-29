namespace CinemaTicketBooking.Views;

public partial class MoviesPage
{
    public MoviesPage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.MoviesViewModel>(this);
    }
}
