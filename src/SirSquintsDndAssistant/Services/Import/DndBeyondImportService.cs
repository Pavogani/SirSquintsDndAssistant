using System.Text.Json;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;

namespace SirSquintsDndAssistant.Services.Import;

public class DndBeyondImportService : IDndBeyondImportService
{
    private readonly HttpClient _httpClient;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IPlayerCharacterRepository _playerCharacterRepository;

    public DndBeyondImportService(HttpClient httpClient, ICampaignRepository campaignRepository, IPlayerCharacterRepository playerCharacterRepository)
    {
        _httpClient = httpClient;
        _campaignRepository = campaignRepository;
        _playerCharacterRepository = playerCharacterRepository;
    }

    public async Task<CharacterImportResult> ImportCharacterAsync(string characterIdOrUrl)
    {
        try
        {
            // Extract ID from URL if a full URL was provided
            string characterId = characterIdOrUrl;
            if (characterIdOrUrl.Contains("dndbeyond.com/characters/"))
            {
                var parts = characterIdOrUrl.Split('/');
                characterId = parts[^1].Split('?')[0]; // Get last part, remove query string
            }

            var url = $"https://character-service.dndbeyond.com/character/v5/character/{characterId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new CharacterImportResult
                {
                    Success = false,
                    ErrorMessage = "Unable to fetch character. Ensure the character is publicly shared and the ID is correct."
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var ddbData = JsonSerializer.Deserialize<JsonElement>(json);

            // Parse basic character info
            var data = ddbData.GetProperty("data");
            var name = data.GetProperty("name").GetString() ?? "Unknown";
            var race = data.TryGetProperty("race", out var raceElem) ? raceElem.GetProperty("fullName").GetString() : "Unknown";

            // Get level from classes
            int level = 0;
            string className = "Unknown";
            if (data.TryGetProperty("classes", out var classes))
            {
                foreach (var cls in classes.EnumerateArray())
                {
                    level += cls.GetProperty("level").GetInt32();
                    if (className == "Unknown")
                        className = cls.GetProperty("definition").GetProperty("name").GetString() ?? "Unknown";
                }
            }

            // Get stats
            int ac = 10;
            int maxHp = 0;
            int passivePerception = 10;

            if (data.TryGetProperty("stats", out var stats))
            {
                foreach (var stat in stats.EnumerateArray())
                {
                    if (stat.GetProperty("id").GetInt32() == 1) // AC
                        ac = stat.GetProperty("value").GetInt32();
                }
            }

            if (data.TryGetProperty("baseHitPoints", out var baseHp))
                maxHp = baseHp.GetInt32();

            if (data.TryGetProperty("passivePerception", out var perception))
                passivePerception = perception.GetInt32();

            // Get active campaign (if any)
            var activeCampaign = await _campaignRepository.GetActiveCampaignAsync();

            var character = new PlayerCharacter
            {
                Name = name,
                Class = className,
                Level = level,
                Race = race,
                ArmorClass = ac,
                MaxHitPoints = maxHp,
                PassivePerception = passivePerception,
                DndBeyondCharacterId = characterId,
                FullDataJson = json,
                LastImported = DateTime.Now,
                CampaignId = activeCampaign?.Id ?? 0
            };

            // SAVE TO DATABASE!
            await _playerCharacterRepository.SaveAsync(character);

            return new CharacterImportResult
            {
                Success = true,
                Character = character
            };
        }
        catch (Exception ex)
        {
            return new CharacterImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}. Note: This uses an unofficial D&D Beyond endpoint that may change."
            };
        }
    }
}
