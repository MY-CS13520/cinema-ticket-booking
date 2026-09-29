namespace CinemaTicketBooking;

/// <summary>Asks for a guest or staff account before the desk opens.</summary>
public partial class LoginWindow
{
    public LoginWindow()
    {
        InitializeComponent();
        var viewModel = App.GetRequiredService<ViewModels.LoginViewModel>();
        DataContext = viewModel;
        UsernameInput.TextChanged += (_, _) => viewModel.Username = UsernameInput.Text;
    }

    private async void Submit_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.LoginViewModel viewModel)
            return;

        viewModel.Username = UsernameInput.Text;
        var signedIn = viewModel.IsCreatingAccount
            ? await viewModel.TryRegisterAsync(PasswordInput.Password, ConfirmInput.Password)
            : await viewModel.TrySignInAsync(PasswordInput.Password);

        if (signedIn)
            DialogResult = true;
    }
}
