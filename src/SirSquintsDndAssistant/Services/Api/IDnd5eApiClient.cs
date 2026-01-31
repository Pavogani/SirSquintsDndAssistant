using SirSquintsDndAssistant.Models.Api;

namespace SirSquintsDndAssistant.Services.Api;

public interface IDnd5eApiClient
{
    Task<Dnd5eApiListResponse?> GetMonstersAsync();
    Task<Dnd5eMonsterResponse?> GetMonsterAsync(string index);
    Task<Dnd5eApiListResponse?> GetSpellsAsync();
    Task<Dnd5eSpellResponse?> GetSpellAsync(string index);
    Task<Dnd5eApiListResponse?> GetEquipmentAsync();
    Task<Dnd5eEquipmentDetailResponse?> GetEquipmentDetailAsync(string index);
    Task<Dnd5eApiListResponse?> GetMagicItemsAsync();
    Task<Dnd5eMagicItemDetailResponse?> GetMagicItemDetailAsync(string index);
    Task<Dnd5eApiListResponse?> GetConditionsAsync();
    Task<Dnd5eConditionResponse?> GetConditionAsync(string index);
}
