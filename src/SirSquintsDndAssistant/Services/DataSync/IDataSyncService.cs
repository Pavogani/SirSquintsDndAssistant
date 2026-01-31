namespace SirSquintsDndAssistant.Services.DataSync;

public interface IDataSyncService
{
    Task<bool> IsInitialSyncCompleteAsync();
    Task PerformInitialSyncAsync(IProgress<SyncProgress>? progress = null);

    /// <summary>
    /// Force a complete re-sync of all data, clearing existing data first.
    /// </summary>
    Task ForceResyncAsync(IProgress<SyncProgress>? progress = null);

    /// <summary>
    /// Checks if data is outdated and returns true if an update is recommended.
    /// </summary>
    Task<bool> CheckForUpdatesAsync();

    /// <summary>
    /// Sync only monsters from all sources.
    /// </summary>
    Task SyncMonstersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync only spells from all sources.
    /// </summary>
    Task SyncSpellsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync only equipment from all sources.
    /// </summary>
    Task SyncEquipmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync only magic items from all sources.
    /// </summary>
    Task SyncMagicItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event for progress updates during sync operations.
    /// </summary>
    event EventHandler<DataSyncProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Result of the last sync operation.
    /// </summary>
    DataSyncResult? LastSyncResult { get; }
}

/// <summary>
/// Event args for data sync progress.
/// </summary>
public class DataSyncProgressEventArgs : EventArgs
{
    public double Progress { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ItemsProcessed { get; set; }
    public int TotalItems { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
}

/// <summary>
/// Result of a data sync operation.
/// </summary>
public class DataSyncResult
{
    public int TotalItems { get; set; }
    public int MonstersAdded { get; set; }
    public int SpellsAdded { get; set; }
    public int EquipmentAdded { get; set; }
    public int MagicItemsAdded { get; set; }
    public int Errors { get; set; }
}
