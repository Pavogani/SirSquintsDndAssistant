using System.Collections.Concurrent;

namespace SirSquintsDndAssistant.Services.Utilities;

public interface IDebounceService
{
    void Debounce(string key, Action action, int delayMs = 300);
    Task DebounceAsync(string key, Func<Task> action, int delayMs = 300);
    void Cancel(string key);
    void CancelAll();
}

public class DebounceService : IDebounceService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();

    public void Debounce(string key, Action action, int delayMs = 300)
    {
        // Cancel any existing debounce for this key
        Cancel(key);

        var cts = new CancellationTokenSource();
        _tokens[key] = cts;

        Task.Delay(delayMs, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                _tokens.TryRemove(key, out _);
                MainThread.BeginInvokeOnMainThread(action);
            }
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public async Task DebounceAsync(string key, Func<Task> action, int delayMs = 300)
    {
        // Cancel any existing debounce for this key
        Cancel(key);

        var cts = new CancellationTokenSource();
        _tokens[key] = cts;

        try
        {
            await Task.Delay(delayMs, cts.Token);
            _tokens.TryRemove(key, out _);
            await action();
        }
        catch (TaskCanceledException)
        {
            // Debounce was cancelled, ignore
        }
    }

    public void Cancel(string key)
    {
        if (_tokens.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void CancelAll()
    {
        foreach (var key in _tokens.Keys.ToList())
        {
            Cancel(key);
        }
    }
}
