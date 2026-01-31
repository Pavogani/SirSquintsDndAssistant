using System.Collections.Concurrent;

namespace SirSquintsDndAssistant.Services.Utilities;

public interface IImageCacheService : IDisposable
{
    Task<ImageSource?> GetCachedImageAsync(string imagePath);
    void ClearCache();
    void RemoveFromCache(string imagePath);
    int CacheCount { get; }
    long EstimatedCacheSizeBytes { get; }
}

public class ImageCacheService : IImageCacheService
{
    private readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _cache = new();
    private readonly ConcurrentDictionary<string, DateTime> _accessTimes = new();
    private readonly int _maxCacheSize;
    private readonly TimeSpan _cacheExpiry;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private long _estimatedSize;
    private bool _disposed;

    public int CacheCount => _cache.Count;
    public long EstimatedCacheSizeBytes => _estimatedSize;

    public ImageCacheService(int maxCacheSize = 50, int cacheExpiryMinutes = 30)
    {
        _maxCacheSize = maxCacheSize;
        _cacheExpiry = TimeSpan.FromMinutes(cacheExpiryMinutes);
        _cancellationTokenSource = new CancellationTokenSource();

        // Start background cleanup task with cancellation support
        _ = Task.Run(() => CleanupExpiredEntriesAsync(_cancellationTokenSource.Token));
    }

    public async Task<ImageSource?> GetCachedImageAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return null;

        // Check if in cache and still valid
        if (_cache.TryGetValue(imagePath, out var weakRef) && weakRef.TryGetTarget(out var cachedImage))
        {
            _accessTimes[imagePath] = DateTime.Now;
            return cachedImage;
        }

        // Not in cache or expired, load from disk
        try
        {
            if (!File.Exists(imagePath))
                return null;

            // Load image on background thread
            var imageSource = await Task.Run(() =>
            {
                using var stream = File.OpenRead(imagePath);
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                return ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
            });

            // Add to cache
            await AddToCacheAsync(imagePath, imageSource);

            return imageSource;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading cached image: {ex.Message}");
            return null;
        }
    }

    private async Task AddToCacheAsync(string imagePath, ImageSource imageSource)
    {
        // Enforce cache size limit
        if (_cache.Count >= _maxCacheSize)
        {
            await EvictOldestEntryAsync();
        }

        _cache[imagePath] = new WeakReference<ImageSource>(imageSource);
        _accessTimes[imagePath] = DateTime.Now;

        // Estimate size (rough approximation based on file size)
        try
        {
            var fileInfo = new FileInfo(imagePath);
            Interlocked.Add(ref _estimatedSize, fileInfo.Length);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error estimating cache size: {ex.Message}");
        }
    }

    private Task EvictOldestEntryAsync()
    {
        // Find and remove the oldest accessed entry
        var oldest = _accessTimes
            .OrderBy(kvp => kvp.Value)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(oldest.Key))
        {
            RemoveFromCache(oldest.Key);
        }

        return Task.CompletedTask;
    }

    public void RemoveFromCache(string imagePath)
    {
        _cache.TryRemove(imagePath, out _);
        _accessTimes.TryRemove(imagePath, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
        _accessTimes.Clear();
        Interlocked.Exchange(ref _estimatedSize, 0);
    }

    private async Task CleanupExpiredEntriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                var now = DateTime.Now;
                var expiredKeys = _accessTimes
                    .Where(kvp => now - kvp.Value > _cacheExpiry)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    RemoveFromCache(key);
                }

                System.Diagnostics.Debug.WriteLine($"ImageCacheService: Cleaned up {expiredKeys.Count} expired entries. Current cache size: {_cache.Count}");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            System.Diagnostics.Debug.WriteLine("ImageCacheService: Cleanup task cancelled.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ImageCacheService: Error in cleanup task: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        ClearCache();
    }
}
