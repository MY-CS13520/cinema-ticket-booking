namespace CinemaTicketBooking.Views;

public partial class SchedulePage
{
    public SchedulePage()
    {
        InitializeComponent();
        ViewModels.PageBootstrap.Wire<ViewModels.ScheduleViewModel>(this);
    }
}
