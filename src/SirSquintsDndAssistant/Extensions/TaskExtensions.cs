namespace SirSquintsDndAssistant.Extensions;

/// <summary>
/// Extension methods for Task to handle fire-and-forget scenarios safely.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Safely executes a Task without awaiting, with proper error handling.
    /// Use this instead of _ = SomeAsyncMethod() to ensure exceptions are logged.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="onError">Optional error handler. If null, errors are logged to Debug output.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured synchronization context.</param>
    public static async void SafeFireAndForget(
        this Task task,
        Action<Exception>? onError = null,
        bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            if (onError != null)
            {
                onError(ex);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SafeFireAndForget caught exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Safely executes a ValueTask without awaiting, with proper error handling.
    /// </summary>
    public static async void SafeFireAndForget(
        this ValueTask task,
        Action<Exception>? onError = null,
        bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            if (onError != null)
            {
                onError(ex);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SafeFireAndForget caught exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Safely executes a Task<T> without awaiting, with proper error handling.
    /// </summary>
    public static async void SafeFireAndForget<T>(
        this Task<T> task,
        Action<Exception>? onError = null,
        bool continueOnCapturedContext = false)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex)
        {
            if (onError != null)
            {
                onError(ex);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SafeFireAndForget caught exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
