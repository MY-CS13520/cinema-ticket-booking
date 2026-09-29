using System.Windows;
using System.Windows.Media;
using CinemaTicketBooking.Data;
using CinemaTicketBooking.Helpers;
using CinemaTicketBooking.Services;
using CinemaTicketBooking.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace CinemaTicketBooking;

/// <summary>
/// Starts the desk: opens the SQLite database, seeds it the first time, and shows the main window.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public static T GetRequiredService<T>() where T : notnull
        => Services!.GetRequiredService<T>();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CamCursor.Install();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        services.AddDbContextFactory<CinemaDbContext>(options =>
            options.UseSqlite($"Data Source={AppPaths.DatabaseFile}"));

        services.AddSingleton<DeskSession>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IMovieService, MovieService>();
        services.AddSingleton<IScheduleService, ScheduleService>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<IPaymentService, PaymentService>();
        services.AddSingleton<IReportService, ReportService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<MoviesViewModel>();
        services.AddTransient<ScheduleViewModel>();
        services.AddTransient<BookingViewModel>();
        services.AddTransient<BookingsViewModel>();
        services.AddTransient<PaymentsViewModel>();
        services.AddTransient<ReportsViewModel>();

        Services = services.BuildServiceProvider();

        try
        {
            ApplicationAccentColorManager.Apply(Color.FromRgb(0xE4, 0xB1, 0x5A), ApplicationTheme.Dark);
            await DbInitializer.InitializeAsync(Services.GetRequiredService<IDbContextFactory<CinemaDbContext>>());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Lumina Cinema could not start",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var login = new LoginWindow();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
