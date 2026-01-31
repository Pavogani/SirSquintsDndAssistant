using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;
using System.Text.Json;

namespace SirSquintsDndAssistant.Services.Combat;

public class CombatService : ICombatService
{
    private readonly ICombatRepository _combatRepository;
    private CombatEncounter? _activeCombat;
    private List<InitiativeEntry> _combatants = new();

    public CombatEncounter? ActiveCombat => _activeCombat;

    public CombatService(ICombatRepository combatRepository)
    {
        _combatRepository = combatRepository;
    }

    public async Task<CombatEncounter> StartNewCombatAsync(string encounterName)
    {
        // End existing combat if any
        if (_activeCombat != null)
        {
            await EndCombatAsync();
        }

        _activeCombat = new CombatEncounter
        {
            Name = encounterName,
            StartTime = DateTime.Now,
            IsActive = true,
            CurrentRound = 1,
            CurrentTurnIndex = 0
        };

        await _combatRepository.SaveAsync(_activeCombat);
        _combatants.Clear();

        return _activeCombat;
    }

    public async Task EndCombatAsync()
    {
        if (_activeCombat != null)
        {
            _activeCombat.IsActive = false;
            _activeCombat.EndTime = DateTime.Now;
            await _combatRepository.SaveAsync(_activeCombat);
            _activeCombat = null;
            _combatants.Clear();
        }
    }

    public async Task<InitiativeEntry> AddCombatantAsync(string name, string type, int ac, int maxHp, int initiativeBonus, int? referenceId = null)
    {
        if (_activeCombat == null)
            throw new InvalidOperationException("No active combat. Start a combat first.");

        var entry = new InitiativeEntry
        {
            CombatEncounterId = _activeCombat.Id,
            Name = name,
            CombatantType = type,
            ReferenceId = referenceId,
            ArmorClass = ac,
            MaxHitPoints = maxHp,
            CurrentHitPoints = maxHp,
            InitiativeBonus = initiativeBonus,
            Initiative = 0,
            SortOrder = _combatants.Count
        };

        await _combatRepository.SaveInitiativeEntryAsync(entry);
        _combatants.Add(entry);

        return entry;
    }

    public async Task RemoveCombatantAsync(InitiativeEntry entry)
    {
        await _combatRepository.DeleteInitiativeEntryAsync(entry);
        _combatants.Remove(entry);
    }

    public async Task RollInitiativeAsync(InitiativeEntry entry)
    {
        // Use thread-safe Random.Shared (.NET 6+)
        var roll = Random.Shared.Next(1, 21); // 1d20
        entry.Initiative = roll + entry.InitiativeBonus;
        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task SortByInitiativeAsync()
    {
        _combatants = _combatants
            .OrderByDescending(c => c.Initiative)
            .ThenByDescending(c => c.InitiativeBonus)
            .ToList();

        // Update sort order
        for (int i = 0; i < _combatants.Count; i++)
        {
            _combatants[i].SortOrder = i;
            await _combatRepository.SaveInitiativeEntryAsync(_combatants[i]);
        }
    }

    public async Task NextTurnAsync()
    {
        if (_activeCombat == null || _combatants.Count == 0)
            return;

        _activeCombat.CurrentTurnIndex++;

        if (_activeCombat.CurrentTurnIndex >= _combatants.Count)
        {
            _activeCombat.CurrentTurnIndex = 0;
            _activeCombat.CurrentRound++;
        }

        await _combatRepository.SaveAsync(_activeCombat);
    }

    public async Task PreviousTurnAsync()
    {
        if (_activeCombat == null || _combatants.Count == 0)
            return;

        _activeCombat.CurrentTurnIndex--;

        if (_activeCombat.CurrentTurnIndex < 0)
        {
            _activeCombat.CurrentTurnIndex = _combatants.Count - 1;
            _activeCombat.CurrentRound = Math.Max(1, _activeCombat.CurrentRound - 1);
        }

        await _combatRepository.SaveAsync(_activeCombat);
    }

    public async Task ApplyDamageAsync(InitiativeEntry entry, int damage)
    {
        int remainingDamage = damage;

        // Temp HP absorbs damage first
        if (entry.TempHitPoints > 0)
        {
            if (entry.TempHitPoints >= remainingDamage)
            {
                entry.TempHitPoints -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= entry.TempHitPoints;
                entry.TempHitPoints = 0;
            }
        }

        // Apply remaining damage to HP
        if (remainingDamage > 0)
        {
            entry.CurrentHitPoints = Math.Max(0, entry.CurrentHitPoints - remainingDamage);
        }

        if (entry.CurrentHitPoints == 0)
        {
            // Players go to death saves, monsters are defeated
            if (entry.CombatantType != "Player")
            {
                entry.IsDefeated = true;
            }
            // End concentration if at 0 HP
            if (entry.IsConcentrating)
            {
                entry.IsConcentrating = false;
                entry.ConcentrationSpell = string.Empty;
            }
        }

        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task ApplyHealingAsync(InitiativeEntry entry, int healing)
    {
        entry.CurrentHitPoints = Math.Min(entry.MaxHitPoints, entry.CurrentHitPoints + healing);

        if (entry.CurrentHitPoints > 0)
        {
            entry.IsDefeated = false;
        }

        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public Task<List<InitiativeEntry>> GetCombatantsAsync()
    {
        return Task.FromResult(_combatants);
    }

    public async Task AddConditionAsync(InitiativeEntry entry, string conditionName)
    {
        var conditions = GetConditions(entry);
        if (!conditions.Contains(conditionName, StringComparer.OrdinalIgnoreCase))
        {
            conditions.Add(conditionName);
            entry.ConditionsJson = JsonSerializer.Serialize(conditions);
            await _combatRepository.SaveInitiativeEntryAsync(entry);
        }
    }

    public async Task RemoveConditionAsync(InitiativeEntry entry, string conditionName)
    {
        var conditions = GetConditions(entry);
        var toRemove = conditions.FirstOrDefault(c => c.Equals(conditionName, StringComparison.OrdinalIgnoreCase));
        if (toRemove != null)
        {
            conditions.Remove(toRemove);
            entry.ConditionsJson = JsonSerializer.Serialize(conditions);
            await _combatRepository.SaveInitiativeEntryAsync(entry);
        }
    }

    public List<string> GetConditions(InitiativeEntry entry)
    {
        if (string.IsNullOrEmpty(entry.ConditionsJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(entry.ConditionsJson) ?? new List<string>();
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing conditions JSON: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task SaveCombatStateAsync()
    {
        if (_activeCombat != null)
        {
            await _combatRepository.SaveAsync(_activeCombat);

            foreach (var combatant in _combatants)
            {
                await _combatRepository.SaveInitiativeEntryAsync(combatant);
            }
        }
    }

    public async Task LoadActiveCombatAsync()
    {
        _activeCombat = await _combatRepository.GetActiveCombatAsync();

        if (_activeCombat != null)
        {
            _combatants = await _combatRepository.GetInitiativeEntriesAsync(_activeCombat.Id);
        }
    }

    // Temp HP Methods
    public async Task AddTempHpAsync(InitiativeEntry entry, int tempHp)
    {
        // Temp HP doesn't stack - take the higher value
        entry.TempHitPoints = Math.Max(entry.TempHitPoints, tempHp);
        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task RemoveTempHpAsync(InitiativeEntry entry)
    {
        entry.TempHitPoints = 0;
        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    // Concentration Methods
    public async Task StartConcentrationAsync(InitiativeEntry entry, string spellName)
    {
        entry.IsConcentrating = true;
        entry.ConcentrationSpell = spellName;
        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task EndConcentrationAsync(InitiativeEntry entry)
    {
        entry.IsConcentrating = false;
        entry.ConcentrationSpell = string.Empty;
        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task<bool> ConcentrationCheckAsync(InitiativeEntry entry, int damage)
    {
        if (!entry.IsConcentrating)
            return true;

        // DC is 10 or half damage, whichever is higher
        int dc = Math.Max(10, damage / 2);

        // Roll d20 + Constitution modifier
        // TODO: Get actual CON modifier from character data when available
        int roll = Random.Shared.Next(1, 21);
        int conMod = 0; // Placeholder - should be derived from character stats

        bool success = (roll + conMod) >= dc;

        if (!success)
        {
            await EndConcentrationAsync(entry);
        }

        return success;
    }

    // Death Save Methods
    public async Task AddDeathSaveSuccessAsync(InitiativeEntry entry)
    {
        entry.DeathSaveSuccesses = Math.Min(3, entry.DeathSaveSuccesses + 1);

        if (entry.DeathSaveSuccesses >= 3)
        {
            await AddConditionAsync(entry, "Unconscious");
        }

        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task AddDeathSaveFailureAsync(InitiativeEntry entry)
    {
        entry.DeathSaveFailures = Math.Min(3, entry.DeathSaveFailures + 1);

        if (entry.DeathSaveFailures >= 3)
        {
            entry.IsDefeated = true;
        }

        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }

    public async Task ResetDeathSavesAsync(InitiativeEntry entry)
    {
        entry.DeathSaveSuccesses = 0;
        entry.DeathSaveFailures = 0;
        await _combatRepository.SaveInitiativeEntryAsync(entry);
    }
}
