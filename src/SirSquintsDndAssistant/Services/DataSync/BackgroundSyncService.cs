using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.DataSync;

/// <summary>
/// Service for running data sync operations in the background.
/// </summary>
public class BackgroundSyncService : IBackgroundSyncService
{
    private readonly IDataSyncService _dataSyncService;
    private readonly IDuplicateDetectionService _duplicateDetection;
    private readonly IDatabaseService _databaseService;

    private CancellationTokenSource? _cancellationTokenSource;
    private Timer? _autoSyncTimer;
    private const string LastSyncKey = "LastSyncTime";

    public bool IsSyncInProgress { get; private set; }
    public double Progress { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public BackgroundSyncService(
        IDataSyncService dataSyncService,
        IDuplicateDetectionService duplicateDetection,
        IDatabaseService databaseService)
    {
        _dataSyncService = dataSyncService;
        _duplicateDetection = duplicateDetection;
        _databaseService = databaseService;
    }

    public async Task StartSyncAsync(SyncOptions options, CancellationToken cancellationToken = default)
    {
        if (IsSyncInProgress)
        {
            return;
        }

        IsSyncInProgress = true;
        Progress = 0;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var startTime = DateTime.Now;
        var result = new SyncCompletedEventArgs();

        try
        {
            var totalSteps = 0;
            if (options.SyncMonsters) totalSteps++;
            if (options.SyncSpells) totalSteps++;
            if (options.SyncEquipment) totalSteps++;
            if (options.SyncMagicItems) totalSteps++;
            if (options.RemoveDuplicates) totalSteps++;

            var currentStep = 0;

            // Subscribe to data sync progress events
            _dataSyncService.ProgressChanged += OnDataSyncProgress;

            try
            {
                if (options.SyncMonsters)
                {
                    UpdateProgress(currentStep, totalSteps, "Syncing monsters...");
                    await _dataSyncService.SyncMonstersAsync(_cancellationTokenSource.Token);
                    currentStep++;
                }

                if (options.SyncSpells)
                {
                    UpdateProgress(currentStep, totalSteps, "Syncing spells...");
                    await _dataSyncService.SyncSpellsAsync(_cancellationTokenSource.Token);
                    currentStep++;
                }

                if (options.SyncEquipment)
                {
                    UpdateProgress(currentStep, totalSteps, "Syncing equipment...");
                    await _dataSyncService.SyncEquipmentAsync(_cancellationTokenSource.Token);
                    currentStep++;
                }

                if (options.SyncMagicItems)
                {
                    UpdateProgress(currentStep, totalSteps, "Syncing magic items...");
                    await _dataSyncService.SyncMagicItemsAsync(_cancellationTokenSource.Token);
                    currentStep++;
                }

                if (options.RemoveDuplicates)
                {
                    UpdateProgress(currentStep, totalSteps, "Removing duplicates...");
                    result.DuplicatesRemoved = await RemoveDuplicatesAsync();
                    currentStep++;
                }

                // Store last sync time
                await SaveLastSyncTimeAsync(DateTime.Now);

                result.Success = true;
                result.TotalItemsSynced = _dataSyncService.LastSyncResult?.TotalItems ?? 0;
                result.MonstersAdded = _dataSyncService.LastSyncResult?.MonstersAdded ?? 0;
                result.SpellsAdded = _dataSyncService.LastSyncResult?.SpellsAdded ?? 0;
                result.EquipmentAdded = _dataSyncService.LastSyncResult?.EquipmentAdded ?? 0;
                result.MagicItemsAdded = _dataSyncService.LastSyncResult?.MagicItemsAdded ?? 0;
            }
            finally
            {
                _dataSyncService.ProgressChanged -= OnDataSyncProgress;
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Sync was cancelled";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Errors++;
        }
        finally
        {
            result.Duration = DateTime.Now - startTime;
            IsSyncInProgress = false;
            Progress = 1.0;
            StatusMessage = result.Success ? "Sync completed" : $"Sync failed: {result.ErrorMessage}";

            SyncCompleted?.Invoke(this, result);
        }
    }

    public void CancelSync()
    {
        _cancellationTokenSource?.Cancel();
    }

    public async Task ScheduleAutoSyncAsync(TimeSpan interval)
    {
        _autoSyncTimer?.Dispose();

        _autoSyncTimer = new Timer(async _ =>
        {
            if (!IsSyncInProgress && await NeedsSyncAsync())
            {
                await StartSyncAsync(new SyncOptions { IncrementalSync = true });
            }
        }, null, interval, interval);

        await Task.CompletedTask;
    }

    public async Task<bool> NeedsSyncAsync()
    {
        var lastSync = GetLastSyncTime();

        // If never synced, needs sync
        if (!lastSync.HasValue) return true;

        // Check if sync is older than 7 days
        if (DateTime.Now - lastSync.Value > TimeSpan.FromDays(7)) return true;

        // Check if database is empty
        var monsters = await _databaseService.GetItemsAsync<Models.Creatures.Monster>();
        if (monsters.Count == 0) return true;

        return false;
    }

    public DateTime? GetLastSyncTime()
    {
        try
        {
            var stored = Preferences.Get(LastSyncKey, string.Empty);
            if (string.IsNullOrEmpty(stored)) return null;
            return DateTime.Parse(stored);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveLastSyncTimeAsync(DateTime time)
    {
        Preferences.Set(LastSyncKey, time.ToString("O"));
        await Task.CompletedTask;
    }

    private void UpdateProgress(int currentStep, int totalSteps, string message)
    {
        Progress = totalSteps > 0 ? (double)currentStep / totalSteps : 0;
        StatusMessage = message;

        SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            Progress = Progress,
            StatusMessage = message,
            CurrentOperation = message
        });
    }

    private void OnDataSyncProgress(object? sender, DataSyncProgressEventArgs e)
    {
        SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            Progress = Progress + (e.Progress * 0.2), // Partial progress within current step
            StatusMessage = e.Message,
            ItemsProcessed = e.ItemsProcessed,
            TotalItems = e.TotalItems,
            CurrentOperation = e.CurrentOperation
        });
    }

    private async Task<int> RemoveDuplicatesAsync()
    {
        int removed = 0;

        // Find and remove monster duplicates
        var monsterDuplicates = await _duplicateDetection.FindAllMonsterDuplicatesAsync();
        foreach (var group in monsterDuplicates)
        {
            if (group.PreferredEntry != null && group.Entries.Count > 1)
            {
                var idsToDelete = group.Entries
                    .Where(e => e.Item.Id != group.PreferredEntry.Id)
                    .Select(e => e.Item.Id)
                    .ToList();

                removed += await _duplicateDetection.DeleteDuplicatesAsync<Models.Creatures.Monster>(idsToDelete);
            }
        }

        // Find and remove spell duplicates
        var spellDuplicates = await _duplicateDetection.FindAllSpellDuplicatesAsync();
        foreach (var group in spellDuplicates)
        {
            if (group.PreferredEntry != null && group.Entries.Count > 1)
            {
                var idsToDelete = group.Entries
                    .Where(e => e.Item.Id != group.PreferredEntry.Id)
                    .Select(e => e.Item.Id)
                    .ToList();

                removed += await _duplicateDetection.DeleteDuplicatesAsync<Models.Content.Spell>(idsToDelete);
            }
        }

        // Find and remove equipment duplicates
        var equipmentDuplicates = await _duplicateDetection.FindAllEquipmentDuplicatesAsync();
        foreach (var group in equipmentDuplicates)
        {
            if (group.PreferredEntry != null && group.Entries.Count > 1)
            {
                var idsToDelete = group.Entries
                    .Where(e => e.Item.Id != group.PreferredEntry.Id)
                    .Select(e => e.Item.Id)
                    .ToList();

                removed += await _duplicateDetection.DeleteDuplicatesAsync<Models.Content.Equipment>(idsToDelete);
            }
        }

        return removed;
    }
}
