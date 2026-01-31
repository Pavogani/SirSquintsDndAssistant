using System.Text.Json;
using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Models.Encounter;
using SirSquintsDndAssistant.Services.Database.Repositories;

namespace SirSquintsDndAssistant.Services.Export;

public interface IExportImportService
{
    Task<string> ExportCampaignAsync(int campaignId);
    Task<ExportResult> ImportCampaignAsync(string json);
    Task<string> ExportEncounterAsync(int encounterId);
    Task<ExportResult> ImportEncounterAsync(string json);
    Task<string> ExportAllDataAsync();
    Task SaveExportToFileAsync(string json, string filename);
}

public class ExportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsImported { get; set; }
}

public class CampaignExportData
{
    public string ExportVersion { get; set; } = "1.0";
    public DateTime ExportDate { get; set; } = DateTime.Now;
    public Campaign? Campaign { get; set; }
    public List<Session> Sessions { get; set; } = new();
    public List<Quest> Quests { get; set; } = new();
    public List<NPC> Npcs { get; set; } = new();
    public List<PlayerCharacter> PlayerCharacters { get; set; } = new();
}

public class EncounterExportData
{
    public string ExportVersion { get; set; } = "1.0";
    public DateTime ExportDate { get; set; } = DateTime.Now;
    public EncounterTemplate? Encounter { get; set; }
}

public class FullExportData
{
    public string ExportVersion { get; set; } = "1.0";
    public DateTime ExportDate { get; set; } = DateTime.Now;
    public List<CampaignExportData> Campaigns { get; set; } = new();
    public List<EncounterTemplate> Encounters { get; set; } = new();
}

public class ExportImportService : IExportImportService
{
    private readonly ICampaignRepository _campaignRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly IQuestRepository _questRepo;
    private readonly INpcRepository _npcRepo;
    private readonly IPlayerCharacterRepository _playerCharacterRepo;
    private readonly IEncounterRepository _encounterRepo;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExportImportService(
        ICampaignRepository campaignRepo,
        ISessionRepository sessionRepo,
        IQuestRepository questRepo,
        INpcRepository npcRepo,
        IPlayerCharacterRepository playerCharacterRepo,
        IEncounterRepository encounterRepo)
    {
        _campaignRepo = campaignRepo;
        _sessionRepo = sessionRepo;
        _questRepo = questRepo;
        _npcRepo = npcRepo;
        _playerCharacterRepo = playerCharacterRepo;
        _encounterRepo = encounterRepo;
    }

    public async Task<string> ExportCampaignAsync(int campaignId)
    {
        var campaign = await _campaignRepo.GetByIdAsync(campaignId);
        if (campaign == null)
            throw new ArgumentException($"Campaign with ID {campaignId} not found");

        var exportData = new CampaignExportData
        {
            Campaign = campaign,
            Sessions = (await _sessionRepo.GetByCampaignAsync(campaignId)).ToList(),
            Quests = (await _questRepo.GetByCampaignAsync(campaignId)).ToList(),
            Npcs = (await _npcRepo.GetByCampaignAsync(campaignId)).ToList(),
            PlayerCharacters = (await _playerCharacterRepo.GetByCampaignAsync(campaignId)).ToList()
        };

        return JsonSerializer.Serialize(exportData, _jsonOptions);
    }

    public async Task<ExportResult> ImportCampaignAsync(string json)
    {
        try
        {
            var exportData = JsonSerializer.Deserialize<CampaignExportData>(json, _jsonOptions);
            if (exportData?.Campaign == null)
            {
                return new ExportResult { Success = false, ErrorMessage = "Invalid campaign data format" };
            }

            int itemsImported = 0;

            // Create new campaign (reset ID to create new record)
            var campaign = exportData.Campaign;
            campaign.Id = 0;
            campaign.Name = $"{campaign.Name} (Imported)";
            await _campaignRepo.SaveAsync(campaign);
            itemsImported++;

            // Import sessions
            foreach (var session in exportData.Sessions)
            {
                session.Id = 0;
                session.CampaignId = campaign.Id;
                await _sessionRepo.SaveAsync(session);
                itemsImported++;
            }

            // Import quests
            foreach (var quest in exportData.Quests)
            {
                quest.Id = 0;
                quest.CampaignId = campaign.Id;
                await _questRepo.SaveAsync(quest);
                itemsImported++;
            }

            // Import NPCs
            foreach (var npc in exportData.Npcs)
            {
                npc.Id = 0;
                npc.CampaignId = campaign.Id;
                await _npcRepo.SaveAsync(npc);
                itemsImported++;
            }

            // Import player characters
            foreach (var pc in exportData.PlayerCharacters)
            {
                pc.Id = 0;
                pc.CampaignId = campaign.Id;
                await _playerCharacterRepo.SaveAsync(pc);
                itemsImported++;
            }

            return new ExportResult { Success = true, ItemsImported = itemsImported };
        }
        catch (JsonException ex)
        {
            return new ExportResult { Success = false, ErrorMessage = $"Invalid JSON: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<string> ExportEncounterAsync(int encounterId)
    {
        var encounter = await _encounterRepo.GetByIdAsync(encounterId);
        if (encounter == null)
            throw new ArgumentException($"Encounter with ID {encounterId} not found");

        var exportData = new EncounterExportData
        {
            Encounter = encounter
        };

        return JsonSerializer.Serialize(exportData, _jsonOptions);
    }

    public async Task<ExportResult> ImportEncounterAsync(string json)
    {
        try
        {
            var exportData = JsonSerializer.Deserialize<EncounterExportData>(json, _jsonOptions);
            if (exportData?.Encounter == null)
            {
                return new ExportResult { Success = false, ErrorMessage = "Invalid encounter data format" };
            }

            var encounter = exportData.Encounter;
            encounter.Id = 0;
            encounter.Name = $"{encounter.Name} (Imported)";
            await _encounterRepo.SaveAsync(encounter);

            return new ExportResult { Success = true, ItemsImported = 1 };
        }
        catch (JsonException ex)
        {
            return new ExportResult { Success = false, ErrorMessage = $"Invalid JSON: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<string> ExportAllDataAsync()
    {
        var exportData = new FullExportData();

        var campaigns = await _campaignRepo.GetAllAsync();
        foreach (var campaign in campaigns)
        {
            var campaignData = new CampaignExportData
            {
                Campaign = campaign,
                Sessions = (await _sessionRepo.GetByCampaignAsync(campaign.Id)).ToList(),
                Quests = (await _questRepo.GetByCampaignAsync(campaign.Id)).ToList(),
                Npcs = (await _npcRepo.GetByCampaignAsync(campaign.Id)).ToList(),
                PlayerCharacters = (await _playerCharacterRepo.GetByCampaignAsync(campaign.Id)).ToList()
            };
            exportData.Campaigns.Add(campaignData);
        }

        exportData.Encounters = (await _encounterRepo.GetAllAsync()).ToList();

        return JsonSerializer.Serialize(exportData, _jsonOptions);
    }

    public async Task SaveExportToFileAsync(string json, string filename)
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var exportDir = Path.Combine(documentsPath, "SirSquintsExports");

        if (!Directory.Exists(exportDir))
        {
            Directory.CreateDirectory(exportDir);
        }

        var filePath = Path.Combine(exportDir, filename);
        await File.WriteAllTextAsync(filePath, json);
    }
}
