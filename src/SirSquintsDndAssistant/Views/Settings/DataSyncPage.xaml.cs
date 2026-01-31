using SirSquintsDndAssistant.Services.DataSync;

namespace SirSquintsDndAssistant.Views.Settings;

public partial class DataSyncPage : ContentPage
{
    private readonly IDataSyncService _dataSyncService;

    public DataSyncPage(IDataSyncService dataSyncService)
    {
        InitializeComponent();
        _dataSyncService = dataSyncService;

        // Start sync automatically when page loads
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await StartSyncAsync();
    }

    private async Task StartSyncAsync()
    {
        try
        {
            ErrorFrame.IsVisible = false;

            var progress = new Progress<SyncProgress>(OnProgressUpdated);
            await _dataSyncService.PerformInitialSyncAsync(progress);

            // Wait a moment to show completion message
            await Task.Delay(1500);

            // Navigate to AppShell safely
            NavigateToAppShell();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
            OnProgressUpdated(new SyncProgress
            {
                HasError = true,
                ErrorMessage = ex.Message,
                Percentage = -1
            });
        }
    }

    private void NavigateToAppShell()
    {
        try
        {
            if (Application.Current?.Windows is { Count: > 0 } windows)
            {
                var window = windows[0];
                if (window != null)
                {
                    window.Page = new AppShell();
                    return;
                }
            }

            // Fallback: try setting MainPage directly
            if (Application.Current != null)
            {
                Application.Current.MainPage = new AppShell();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating to AppShell: {ex.Message}");
        }
    }

    private void OnProgressUpdated(SyncProgress progress)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (progress.HasError)
            {
                StatusLabel.Text = "Sync Failed";
                StatusLabel.TextColor = Colors.Red;
                ErrorLabel.Text = progress.ErrorMessage ?? "An unknown error occurred";
                ErrorFrame.IsVisible = true;
                return;
            }

            if (progress.IsComplete)
            {
                SyncProgressBar.Progress = 1.0;
                ProgressLabel.Text = "100%";
                StatusLabel.Text = "✓ Download Complete!";
                StatusLabel.TextColor = Colors.Green;
                DetailLabel.Text = "Ready to use!";
                return;
            }

            // Update progress
            SyncProgressBar.Progress = progress.Percentage / 100.0;
            ProgressLabel.Text = $"{progress.Percentage}%";
            StatusLabel.Text = progress.Message;

            if (progress.TotalItems > 0)
            {
                DetailLabel.Text = $"{progress.CurrentItem} of {progress.TotalItems} items";
            }
            else
            {
                DetailLabel.Text = progress.Message;
            }
        });
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await StartSyncAsync();
    }
}
