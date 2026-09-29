using System.Windows;
using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>
/// Shared busy flag, status line, and the guard that turns storage errors into a desk message.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatus;

    /// <summary>Called each time the page is shown.</summary>
    public Task OnNavigatedAsync() => LoadAsync();

    protected virtual Task LoadAsync() => Task.CompletedTask;

    protected void ShowStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    protected static void Warn(string message)
        => MessageBox.Show(message, "Lumina Cinema", MessageBoxButton.OK, MessageBoxImage.Warning);

    protected static bool Confirm(string message)
        => MessageBox.Show(message, "Lumina Cinema", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    /// <summary>
    /// Runs a save or load. Validation problems and unexpected failures both surface as a message,
    /// and the busy flag is always cleared.
    /// </summary>
    protected async Task GuardAsync(Func<Task> action)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            HasStatus = false;
            await action();
        }
        catch (CinemaValidationException ex)
        {
            Warn(ex.Message);
        }
        catch (Exception ex)
        {
            Warn("That action could not be completed.\n\n" + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
