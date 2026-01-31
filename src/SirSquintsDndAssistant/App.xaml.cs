using Microsoft.Extensions.DependencyInjection;
using SirSquintsDndAssistant.Extensions;
using SirSquintsDndAssistant.Services.Database;
using SirSquintsDndAssistant.Services.DataSync;
using SirSquintsDndAssistant.Views.Settings;

namespace SirSquintsDndAssistant;

public partial class App : Application
{
    private readonly IDatabaseService _databaseService;
    private readonly IDataSyncService _dataSyncService;
    private readonly IServiceProvider _serviceProvider;

    public App(IDatabaseService databaseService, IDataSyncService dataSyncService, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _dataSyncService = dataSyncService;
        _serviceProvider = serviceProvider;

        // Initialize database on startup with error handling
        InitializeDatabaseAsync();
    }

    private async void InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
            // Database initialization errors should be handled gracefully
            // The app can continue but some features may not work
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Start with a loading page, then navigate based on sync status
        var window = new Window(new ContentPage
        {
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new ActivityIndicator { IsRunning = true },
                    new Label { Text = "Loading...", HorizontalOptions = LayoutOptions.Center }
                }
            }
        });

        // Navigate to correct page after async check
        NavigateToStartPageAsync(window).SafeFireAndForget();

        return window;
    }

    private async Task NavigateToStartPageAsync(Window window)
    {
        try
        {
            // Check if initial sync is needed asynchronously
            var syncNeeded = !await _dataSyncService.IsInitialSyncCompleteAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (syncNeeded)
                {
                    // Show data sync page first
                    var dataSyncPage = _serviceProvider.GetRequiredService<DataSyncPage>();
                    window.Page = new NavigationPage(dataSyncPage);
                }
                else
                {
                    // Proceed to main app
                    window.Page = new AppShell();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking sync status: {ex.Message}");
            // Fallback to main app on error
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                window.Page = new AppShell();
            });
        }
    }
}
