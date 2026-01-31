using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.Combat;

public interface IStatusEffectService
{
    event EventHandler<StatusEffect>? EffectAdded;
    event EventHandler<StatusEffect>? EffectRemoved;
    event EventHandler<StatusEffect>? EffectExpired;

    Task<List<StatusEffect>> GetEffectsForCombatantAsync(int initiativeEntryId);
    Task<StatusEffect> AddEffectAsync(StatusEffect effect);
    Task RemoveEffectAsync(int effectId);
    Task RemoveAllEffectsForCombatantAsync(int initiativeEntryId);

    Task<List<StatusEffect>> ProcessRoundStartAsync(int combatEncounterId);
    Task<List<StatusEffect>> ProcessTurnStartAsync(string creatureName, int initiativeEntryId);
    Task<List<StatusEffect>> ProcessTurnEndAsync(string creatureName, int initiativeEntryId);

    Task<StatusEffect> AddConditionAsync(int initiativeEntryId, string conditionName, string targetName, string sourceName, int durationRounds = 0);
    Task<StatusEffect> AddSpellEffectAsync(int initiativeEntryId, string spellName, string targetName, string casterName, int durationRounds, bool isConcentration, bool isBeneficial = false);

    Task RemoveConcentrationEffectsAsync(string casterName);
}

public class StatusEffectService : IStatusEffectService
{
    private readonly IDatabaseService _databaseService;

    public event EventHandler<StatusEffect>? EffectAdded;
    public event EventHandler<StatusEffect>? EffectRemoved;
    public event EventHandler<StatusEffect>? EffectExpired;

    public StatusEffectService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<StatusEffect>> GetEffectsForCombatantAsync(int initiativeEntryId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<StatusEffect>()
                .Where(e => e.InitiativeEntryId == initiativeEntryId)
                .OrderBy(e => e.AppliedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting status effects: {ex.Message}");
            return new List<StatusEffect>();
        }
    }

    public async Task<StatusEffect> AddEffectAsync(StatusEffect effect)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(effect);
            EffectAdded?.Invoke(this, effect);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding status effect: {ex.Message}");
        }

        return effect;
    }

    public async Task RemoveEffectAsync(int effectId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var effect = await db.GetAsync<StatusEffect>(effectId);
            if (effect != null)
            {
                await db.DeleteAsync(effect);
                EffectRemoved?.Invoke(this, effect);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing status effect: {ex.Message}");
        }
    }

    public async Task RemoveAllEffectsForCombatantAsync(int initiativeEntryId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var effects = await GetEffectsForCombatantAsync(initiativeEntryId);
            foreach (var effect in effects)
            {
                await db.DeleteAsync(effect);
                EffectRemoved?.Invoke(this, effect);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing all status effects: {ex.Message}");
        }
    }

    public async Task<List<StatusEffect>> ProcessRoundStartAsync(int combatEncounterId)
    {
        var expiredEffects = new List<StatusEffect>();

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var allEffects = await db.Table<StatusEffect>().ToListAsync();

            foreach (var effect in allEffects)
            {
                if (effect.OnRoundStart())
                {
                    await db.DeleteAsync(effect);
                    expiredEffects.Add(effect);
                    EffectExpired?.Invoke(this, effect);
                }
                else
                {
                    await db.UpdateAsync(effect);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing round start: {ex.Message}");
        }

        return expiredEffects;
    }

    public async Task<List<StatusEffect>> ProcessTurnStartAsync(string creatureName, int initiativeEntryId)
    {
        var expiredEffects = new List<StatusEffect>();

        try
        {
            var effects = await GetEffectsForCombatantAsync(initiativeEntryId);

            foreach (var effect in effects)
            {
                if (effect.OnTurnStart(creatureName))
                {
                    await RemoveEffectAsync(effect.Id);
                    expiredEffects.Add(effect);
                    EffectExpired?.Invoke(this, effect);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing turn start: {ex.Message}");
        }

        return expiredEffects;
    }

    public async Task<List<StatusEffect>> ProcessTurnEndAsync(string creatureName, int initiativeEntryId)
    {
        var expiredEffects = new List<StatusEffect>();

        try
        {
            var effects = await GetEffectsForCombatantAsync(initiativeEntryId);

            foreach (var effect in effects)
            {
                if (effect.OnTurnEnd(creatureName))
                {
                    await RemoveEffectAsync(effect.Id);
                    expiredEffects.Add(effect);
                    EffectExpired?.Invoke(this, effect);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing turn end: {ex.Message}");
        }

        return expiredEffects;
    }

    public async Task<StatusEffect> AddConditionAsync(int initiativeEntryId, string conditionName, string targetName, string sourceName, int durationRounds = 0)
    {
        var effect = StatusEffect.CreateCondition(conditionName, targetName, sourceName, durationRounds);
        effect.InitiativeEntryId = initiativeEntryId;
        return await AddEffectAsync(effect);
    }

    public async Task<StatusEffect> AddSpellEffectAsync(int initiativeEntryId, string spellName, string targetName, string casterName, int durationRounds, bool isConcentration, bool isBeneficial = false)
    {
        var effect = StatusEffect.CreateSpellEffect(spellName, targetName, casterName, durationRounds, isConcentration, isBeneficial);
        effect.InitiativeEntryId = initiativeEntryId;
        return await AddEffectAsync(effect);
    }

    public async Task RemoveConcentrationEffectsAsync(string casterName)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var concentrationEffects = await db.Table<StatusEffect>()
                .Where(e => e.IsConcentration && e.SourceName == casterName)
                .ToListAsync();

            foreach (var effect in concentrationEffects)
            {
                await db.DeleteAsync(effect);
                EffectRemoved?.Invoke(this, effect);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing concentration effects: {ex.Message}");
        }
    }
}
