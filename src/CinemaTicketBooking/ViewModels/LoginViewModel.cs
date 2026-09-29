using CinemaTicketBooking.Models;
using CinemaTicketBooking.Services;

namespace CinemaTicketBooking.ViewModels;

/// <summary>
/// Guest and staff use different doors. A staff password will not open the guest door, and the reverse.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly DeskSession _session;

    public LoginViewModel(IAuthService auth, DeskSession session)
    {
        _auth = auth;
        _session = session;
    }

    [ObservableProperty]
    private bool _isStaff;

    [ObservableProperty]
    private bool _isCreatingAccount;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    public string RoleLabel => IsStaff ? "Staff sign-in" : "Guest sign-in";

    public string DemoHint => IsStaff
        ? "Try the staff desk with  admin  /  admin123"
        : "Try a guest account with  guest  /  guest123";

    public string SubmitLabel => IsCreatingAccount ? "Create account and sign in" : "Sign in";

    public bool CanRegister => !IsStaff;

    public bool ShowCreateButton => CanRegister && !IsCreatingAccount;

    [RelayCommand]
    private void ChooseGuest() => IsStaff = false;

    [RelayCommand]
    private void ChooseStaff() => IsStaff = true;

    [RelayCommand]
    private void StartRegister() => IsCreatingAccount = true;

    [RelayCommand]
    private void CancelRegister() => IsCreatingAccount = false;

    partial void OnIsStaffChanged(bool value)
    {
        if (value)
            IsCreatingAccount = false;

        OnPropertyChanged(nameof(RoleLabel));
        OnPropertyChanged(nameof(DemoHint));
        OnPropertyChanged(nameof(CanRegister));
        OnPropertyChanged(nameof(ShowCreateButton));
    }

    partial void OnIsCreatingAccountChanged(bool value)
    {
        OnPropertyChanged(nameof(SubmitLabel));
        OnPropertyChanged(nameof(ShowCreateButton));
    }

    public async Task<bool> TrySignInAsync(string password)
    {
        try
        {
            var account = await _auth.SignInAsync(
                Username,
                password,
                IsStaff ? AccountRole.Admin : AccountRole.User);
            _session.SignIn(account);
            return true;
        }
        catch (CinemaValidationException ex)
        {
            Warn(ex.Message);
            return false;
        }
    }

    public async Task<bool> TryRegisterAsync(string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            Warn("The two passwords do not match.");
            return false;
        }

        try
        {
            var account = await _auth.RegisterGuestAsync(Username, DisplayName, password);
            _session.SignIn(account);
            return true;
        }
        catch (CinemaValidationException ex)
        {
            Warn(ex.Message);
            return false;
        }
    }
}
