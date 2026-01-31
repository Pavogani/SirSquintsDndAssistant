namespace SirSquintsDndAssistant.Services.Utilities;

public interface IMemoryManagementService : IDisposable
{
    void TriggerGarbageCollection();
    long GetEstimatedMemoryUsage();
    void ClearAllCaches();
    void OnLowMemoryWarning();
    event EventHandler? LowMemoryDetected;
}

public class MemoryManagementService : IMemoryManagementService
{
    private readonly IImageCacheService _imageCacheService;
    private readonly long _lowMemoryThresholdBytes;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public event EventHandler? LowMemoryDetected;

    /// <summary>
    /// Memory threshold in bytes before triggering cleanup. Default: 100 MB.
    /// </summary>
    private const long DefaultMemoryThresholdBytes = 100 * 1024 * 1024;

    public MemoryManagementService(IImageCacheService imageCacheService)
    {
        _imageCacheService = imageCacheService;
        _lowMemoryThresholdBytes = DefaultMemoryThresholdBytes;
        _cancellationTokenSource = new CancellationTokenSource();

        // Start memory monitoring with cancellation support
        _ = Task.Run(() => MonitorMemoryAsync(_cancellationTokenSource.Token));
    }

    public void TriggerGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public long GetEstimatedMemoryUsage()
    {
        return GC.GetTotalMemory(false);
    }

    public void ClearAllCaches()
    {
        _imageCacheService.ClearCache();
        TriggerGarbageCollection();
    }

    public void OnLowMemoryWarning()
    {
        System.Diagnostics.Debug.WriteLine("Low memory warning received - clearing caches");
        ClearAllCaches();
        LowMemoryDetected?.Invoke(this, EventArgs.Empty);
    }

    private async Task MonitorMemoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                var memoryUsage = GetEstimatedMemoryUsage();
                if (memoryUsage > _lowMemoryThresholdBytes)
                {
                    System.Diagnostics.Debug.WriteLine($"High memory usage detected: {memoryUsage / 1024 / 1024} MB");

                    // Clear image cache first
                    _imageCacheService.ClearCache();

                    // Force garbage collection
                    TriggerGarbageCollection();

                    // Notify listeners
                    LowMemoryDetected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            System.Diagnostics.Debug.WriteLine("MemoryManagementService: Monitor task cancelled.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MemoryManagementService: Error in monitor task: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
