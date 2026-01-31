namespace SirSquintsDndAssistant.Services.DataSync;

/// <summary>
/// Service for running data sync operations in the background.
/// </summary>
public interface IBackgroundSyncService
{
    /// <summary>
    /// Whether a sync operation is currently running.
    /// </summary>
    bool IsSyncInProgress { get; }

    /// <summary>
    /// Current sync progress (0.0 to 1.0).
    /// </summary>
    double Progress { get; }

    /// <summary>
    /// Current status message.
    /// </summary>
    string StatusMessage { get; }

    /// <summary>
    /// Event raised when sync progress changes.
    /// </summary>
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    /// <summary>
    /// Event raised when sync completes.
    /// </summary>
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    /// <summary>
    /// Start a background sync operation.
    /// </summary>
    Task StartSyncAsync(SyncOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel the current sync operation.
    /// </summary>
    void CancelSync();

    /// <summary>
    /// Schedule an automatic sync check.
    /// </summary>
    Task ScheduleAutoSyncAsync(TimeSpan interval);

    /// <summary>
    /// Check if data needs to be synced.
    /// </summary>
    Task<bool> NeedsSyncAsync();

    /// <summary>
    /// Get the last sync timestamp.
    /// </summary>
    DateTime? GetLastSyncTime();
}

/// <summary>
/// Options for sync operations.
/// </summary>
public class SyncOptions
{
    /// <summary>Whether to sync monsters.</summary>
    public bool SyncMonsters { get; set; } = true;

    /// <summary>Whether to sync spells.</summary>
    public bool SyncSpells { get; set; } = true;

    /// <summary>Whether to sync equipment.</summary>
    public bool SyncEquipment { get; set; } = true;

    /// <summary>Whether to sync magic items.</summary>
    public bool SyncMagicItems { get; set; } = true;

    /// <summary>Whether to perform incremental sync (only new/updated items).</summary>
    public bool IncrementalSync { get; set; } = true;

    /// <summary>Whether to remove duplicates after sync.</summary>
    public bool RemoveDuplicates { get; set; } = true;

    /// <summary>Maximum items to sync (0 = unlimited).</summary>
    public int MaxItems { get; set; } = 0;

    /// <summary>Batch size for processing.</summary>
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Event args for sync progress updates.
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    public double Progress { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public int ItemsProcessed { get; set; }
    public int TotalItems { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
}

/// <summary>
/// Event args for sync completion.
/// </summary>
public class SyncCompletedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public int TotalItemsSynced { get; set; }
    public int MonstersAdded { get; set; }
    public int SpellsAdded { get; set; }
    public int EquipmentAdded { get; set; }
    public int MagicItemsAdded { get; set; }
    public int DuplicatesRemoved { get; set; }
    public int Errors { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
