using CommunityToolkit.Mvvm.ComponentModel;

namespace SirSquintsDndAssistant.ViewModels;

/// <summary>
/// Base ViewModel class implementing IDisposable for proper cleanup of event handlers
/// and other resources. All ViewModels should call Dispose when no longer needed.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    private bool _disposed;

    /// <summary>
    /// Override in derived classes to perform cleanup of event handlers and resources.
    /// Always call base.Dispose(disposing) at the end.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Derived classes should unsubscribe from events here
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
