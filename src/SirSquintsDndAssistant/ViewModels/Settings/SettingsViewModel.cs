using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Services.DataSync;
using SirSquintsDndAssistant.Services.Import;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Validation;
using SirSquintsDndAssistant.Services.Export;

namespace SirSquintsDndAssistant.ViewModels.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IDataSyncService _dataSyncService;
    private readonly IDndBeyondImportService _importService;
    private readonly ICampaignRepository _campaignRepo;
    private readonly IDataValidationService _validationService;
    private readonly IExportImportService _exportImportService;

    [ObservableProperty] private bool initialSyncComplete;
    [ObservableProperty] private string appVersion = "1.2.0";
    [ObservableProperty] private string characterId = string.Empty;
    [ObservableProperty] private string importStatus = string.Empty;
    [ObservableProperty] private string validationStatus = string.Empty;
    [ObservableProperty] private bool isValidating;
    [ObservableProperty] private string exportStatus = string.Empty;
    [ObservableProperty] private bool isExporting;

    public SettingsViewModel(
        IDataSyncService dataSyncService,
        IDndBeyondImportService importService,
        ICampaignRepository campaignRepo,
        IDataValidationService validationService,
        IExportImportService exportImportService)
    {
        _dataSyncService = dataSyncService;
        _importService = importService;
        _campaignRepo = campaignRepo;
        _validationService = validationService;
        _exportImportService = exportImportService;
        Title = "Settings";
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        InitialSyncComplete = await _dataSyncService.IsInitialSyncCompleteAsync();
    }

    [RelayCommand]
    private void ClearSyncData()
    {
        Preferences.Default.Clear();
        ImportStatus = "Sync data cleared! Restart app to re-download.";
    }

    [ObservableProperty] private bool isSyncing;
    [ObservableProperty] private string syncStatus = string.Empty;
    [ObservableProperty] private double syncProgress;

    [RelayCommand]
    private async Task ForceResyncAsync()
    {
        if (IsSyncing) return;

        IsSyncing = true;
        SyncStatus = "Starting resync...";
        SyncProgress = 0;

        try
        {
            var progress = new Progress<SyncProgress>(p =>
            {
                SyncProgress = p.Percentage / 100.0;
                SyncStatus = p.Message;
                if (p.HasError)
                {
                    SyncStatus = $"Error: {p.ErrorMessage}";
                }
                else if (p.IsComplete)
                {
                    SyncStatus = "Resync complete!";
                }
            });

            await _dataSyncService.ForceResyncAsync(progress);
        }
        catch (Exception ex)
        {
            SyncStatus = $"Resync failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ForceResync error: {ex}");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task ImportCharacterAsync()
    {
        if (string.IsNullOrWhiteSpace(CharacterId))
        {
            ImportStatus = "Please enter a character ID";
            return;
        }

        ImportStatus = "Importing...";
        var result = await _importService.ImportCharacterAsync(CharacterId);

        if (result.Success && result.Character != null)
        {
            ImportStatus = $"Success! Imported {result.Character.Name}";
            CharacterId = string.Empty;
        }
        else
        {
            ImportStatus = $"Failed: {result.ErrorMessage}";
        }
    }

    [RelayCommand]
    private async Task ValidateDataAsync()
    {
        IsValidating = true;
        ValidationStatus = "Validating data...";

        try
        {
            var report = await _validationService.ValidateAllDataAsync();
            ValidationStatus = report.IsValid
                ? $"All data valid! {report.TotalMonsters} monsters, {report.TotalSpells} spells, {report.TotalEquipment} equipment, {report.TotalMagicItems} magic items, {report.TotalConditions} conditions."
                : $"Found issues: {report.DuplicateMonsters + report.DuplicateSpells + report.DuplicateEquipment + report.DuplicateMagicItems} duplicates. Use 'Remove Duplicates' to fix.";
        }
        catch (Exception ex)
        {
            ValidationStatus = $"Error validating: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
        }
    }

    [RelayCommand]
    private async Task RemoveDuplicatesAsync()
    {
        IsValidating = true;
        ValidationStatus = "Removing duplicates...";

        try
        {
            var removed = await _validationService.RemoveAllDuplicatesAsync();
            ValidationStatus = removed > 0
                ? $"Removed {removed} duplicate entries. Data is now clean!"
                : "No duplicates found. Data is already clean!";
        }
        catch (Exception ex)
        {
            ValidationStatus = $"Error removing duplicates: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllDataAsync()
    {
        IsExporting = true;
        ExportStatus = "Exporting all data...";

        try
        {
            var json = await _exportImportService.ExportAllDataAsync();
            var filename = $"sirsquints_backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.json";
            await _exportImportService.SaveExportToFileAsync(json, filename);

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ExportStatus = $"Exported to Documents/SirSquintsExports/{filename}";
        }
        catch (Exception ex)
        {
            ExportStatus = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        IsExporting = true;
        ExportStatus = "Importing...";

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select backup file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.json" } }
                })
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                // Try to determine if it's a campaign or full export
                if (json.Contains("\"campaigns\""))
                {
                    // Full export - not directly supported for import yet
                    ExportStatus = "Full backup import not yet supported. Export campaigns individually.";
                }
                else if (json.Contains("\"campaign\""))
                {
                    var importResult = await _exportImportService.ImportCampaignAsync(json);
                    ExportStatus = importResult.Success
                        ? $"Imported {importResult.ItemsImported} items successfully!"
                        : $"Import failed: {importResult.ErrorMessage}";
                }
                else if (json.Contains("\"encounter\""))
                {
                    var importResult = await _exportImportService.ImportEncounterAsync(json);
                    ExportStatus = importResult.Success
                        ? "Encounter imported successfully!"
                        : $"Import failed: {importResult.ErrorMessage}";
                }
                else
                {
                    ExportStatus = "Unknown file format";
                }
            }
            else
            {
                ExportStatus = "No file selected";
            }
        }
        catch (Exception ex)
        {
            ExportStatus = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }
}
