using System.Windows;

namespace CinemaTicketBooking.ViewModels;

/// <summary>Connects a page to its view model and reloads the page whenever it is shown.</summary>
internal static class PageBootstrap
{
    public static void Wire<TViewModel>(FrameworkElement page)
        where TViewModel : ViewModelBase
    {
        if (App.Services is null)
            return;

        page.DataContext = App.GetRequiredService<TViewModel>();
        page.Loaded += async (_, _) =>
        {
            if (page.DataContext is ViewModelBase viewModel)
                await viewModel.OnNavigatedAsync();
        };
    }
}
