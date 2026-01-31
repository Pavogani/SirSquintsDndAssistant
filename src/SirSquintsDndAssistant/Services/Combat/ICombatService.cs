using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Models.Creatures;

namespace SirSquintsDndAssistant.Services.Combat;

public interface ICombatService
{
    CombatEncounter? ActiveCombat { get; }
    Task<CombatEncounter> StartNewCombatAsync(string encounterName);
    Task EndCombatAsync();
    Task<InitiativeEntry> AddCombatantAsync(string name, string type, int ac, int maxHp, int initiativeBonus, int? referenceId = null);
    Task RemoveCombatantAsync(InitiativeEntry entry);
    Task RollInitiativeAsync(InitiativeEntry entry);
    Task SortByInitiativeAsync();
    Task NextTurnAsync();
    Task PreviousTurnAsync();
    Task ApplyDamageAsync(InitiativeEntry entry, int damage);
    Task ApplyHealingAsync(InitiativeEntry entry, int healing);
    Task AddConditionAsync(InitiativeEntry entry, string conditionName);
    Task RemoveConditionAsync(InitiativeEntry entry, string conditionName);
    List<string> GetConditions(InitiativeEntry entry);
    Task<List<InitiativeEntry>> GetCombatantsAsync();

    // Temp HP
    Task AddTempHpAsync(InitiativeEntry entry, int tempHp);
    Task RemoveTempHpAsync(InitiativeEntry entry);

    // Concentration
    Task StartConcentrationAsync(InitiativeEntry entry, string spellName);
    Task EndConcentrationAsync(InitiativeEntry entry);
    Task<bool> ConcentrationCheckAsync(InitiativeEntry entry, int damage);

    // Death Saves
    Task AddDeathSaveSuccessAsync(InitiativeEntry entry);
    Task AddDeathSaveFailureAsync(InitiativeEntry entry);
    Task ResetDeathSavesAsync(InitiativeEntry entry);
    Task SaveCombatStateAsync();
    Task LoadActiveCombatAsync();
}
