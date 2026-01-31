namespace SirSquintsDndAssistant.Extensions;

/// <summary>
/// Helper class for debouncing rapid method calls.
/// Useful for search-as-you-type scenarios to reduce excessive API/database calls.
/// </summary>
public class DebounceHelper
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _lock = new();

    /// <summary>
    /// Debounce an action by the specified delay.
    /// If called again before the delay completes, the previous call is cancelled.
    /// </summary>
    /// <param name="action">The action to execute after the delay.</param>
    /// <param name="delayMs">The delay in milliseconds (default: 300ms).</param>
    public void Debounce(Action action, int delayMs = 300)
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        var token = _cancellationTokenSource.Token;

        Task.Delay(delayMs, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                action();
            }
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Debounce an async action by the specified delay.
    /// If called again before the delay completes, the previous call is cancelled.
    /// </summary>
    /// <param name="action">The async action to execute after the delay.</param>
    /// <param name="delayMs">The delay in milliseconds (default: 300ms).</param>
    public void DebounceAsync(Func<Task> action, int delayMs = 300)
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        var token = _cancellationTokenSource.Token;

        Task.Delay(delayMs, token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Debounced action error: {ex.Message}");
                }
            }
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Cancel any pending debounced action.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }
    }
}
