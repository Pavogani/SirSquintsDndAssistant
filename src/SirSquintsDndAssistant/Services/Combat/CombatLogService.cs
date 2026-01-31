using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.Combat;

public interface ICombatLogService
{
    event EventHandler<CombatLogEntry>? LogEntryAdded;

    Task LogAttackAsync(int encounterId, int round, string attacker, string target);
    Task LogDamageAsync(int encounterId, int round, string attacker, string target, int damage);
    Task LogHealingAsync(int encounterId, int round, string healer, string target, int healing);
    Task LogKillAsync(int encounterId, int round, string attacker, string target);
    Task LogDeathAsync(int encounterId, int round, string creature);
    Task LogConditionAppliedAsync(int encounterId, int round, string target, string condition);
    Task LogConditionRemovedAsync(int encounterId, int round, string target, string condition);
    Task LogTurnStartAsync(int encounterId, int round, string creature);
    Task LogRoundStartAsync(int encounterId, int round);
    Task LogCombatStartAsync(int encounterId, string description);
    Task LogCombatEndAsync(int encounterId, int round);
    Task LogInitiativeRollAsync(int encounterId, string creature, int roll, int total);
    Task LogSavingThrowAsync(int encounterId, int round, string creature, int dc, int roll, bool success);
    Task LogSpellCastAsync(int encounterId, int round, string caster, string spellName);
    Task LogConcentrationAsync(int encounterId, int round, string caster, string description);
    Task LogDeathSaveAsync(int encounterId, int round, string creature, string result);
    Task LogCustomAsync(int encounterId, int round, string description);

    Task<List<CombatLogEntry>> GetLogsForEncounterAsync(int encounterId);
    Task<List<CombatLogEntry>> GetLogsForRoundAsync(int encounterId, int round);
    Task ClearLogsForEncounterAsync(int encounterId);

    // In-memory log for current session (not persisted until combat ends)
    List<CombatLogEntry> CurrentSessionLog { get; }
    void ClearCurrentSessionLog();
}

public class CombatLogService : ICombatLogService
{
    private readonly IDatabaseService _databaseService;
    private readonly List<CombatLogEntry> _currentSessionLog = new();

    public event EventHandler<CombatLogEntry>? LogEntryAdded;

    public List<CombatLogEntry> CurrentSessionLog => _currentSessionLog.ToList();

    public CombatLogService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task LogAttackAsync(int encounterId, int round, string attacker, string target)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = attacker,
            TargetName = target,
            LogType = CombatLogType.Attack
        });
    }

    public async Task LogDamageAsync(int encounterId, int round, string attacker, string target, int damage)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = attacker,
            TargetName = target,
            LogType = CombatLogType.Damage,
            DamageDealt = damage
        });
    }

    public async Task LogHealingAsync(int encounterId, int round, string healer, string target, int healing)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = healer,
            TargetName = target,
            LogType = CombatLogType.Heal,
            HealingDone = healing
        });
    }

    public async Task LogKillAsync(int encounterId, int round, string attacker, string target)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = attacker,
            TargetName = target,
            LogType = CombatLogType.Kill
        });
    }

    public async Task LogDeathAsync(int encounterId, int round, string creature)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = creature,
            LogType = CombatLogType.Death
        });
    }

    public async Task LogConditionAppliedAsync(int encounterId, int round, string target, string condition)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            TargetName = target,
            LogType = CombatLogType.ConditionApplied,
            ConditionApplied = condition
        });
    }

    public async Task LogConditionRemovedAsync(int encounterId, int round, string target, string condition)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            TargetName = target,
            LogType = CombatLogType.ConditionRemoved,
            ConditionRemoved = condition
        });
    }

    public async Task LogTurnStartAsync(int encounterId, int round, string creature)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = creature,
            LogType = CombatLogType.TurnStart
        });
    }

    public async Task LogRoundStartAsync(int encounterId, int round)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            LogType = CombatLogType.RoundStart
        });
    }

    public async Task LogCombatStartAsync(int encounterId, string description)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = 0,
            Timestamp = DateTime.Now,
            LogType = CombatLogType.CombatStart,
            Description = description
        });
    }

    public async Task LogCombatEndAsync(int encounterId, int round)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            LogType = CombatLogType.CombatEnd
        });
    }

    public async Task LogInitiativeRollAsync(int encounterId, string creature, int roll, int total)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = 0,
            Timestamp = DateTime.Now,
            ActorName = creature,
            LogType = CombatLogType.InitiativeRoll,
            DiceRoll = roll,
            DiceTotal = total
        });
    }

    public async Task LogSavingThrowAsync(int encounterId, int round, string creature, int dc, int roll, bool success)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = creature,
            LogType = CombatLogType.SavingThrow,
            DiceRoll = dc,
            DiceTotal = roll,
            Description = success ? "Success" : "Failure"
        });
    }

    public async Task LogSpellCastAsync(int encounterId, int round, string caster, string spellName)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = caster,
            LogType = CombatLogType.SpellCast,
            Description = spellName
        });
    }

    public async Task LogConcentrationAsync(int encounterId, int round, string caster, string description)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = caster,
            LogType = CombatLogType.Concentration,
            Description = description
        });
    }

    public async Task LogDeathSaveAsync(int encounterId, int round, string creature, string result)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            ActorName = creature,
            LogType = CombatLogType.DeathSave,
            Description = result
        });
    }

    public async Task LogCustomAsync(int encounterId, int round, string description)
    {
        await AddLogEntryAsync(new CombatLogEntry
        {
            CombatEncounterId = encounterId,
            Round = round,
            Timestamp = DateTime.Now,
            LogType = CombatLogType.Custom,
            Description = description
        });
    }

    public async Task<List<CombatLogEntry>> GetLogsForEncounterAsync(int encounterId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<CombatLogEntry>()
                .Where(e => e.CombatEncounterId == encounterId)
                .OrderBy(e => e.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting combat logs: {ex.Message}");
            return new List<CombatLogEntry>();
        }
    }

    public async Task<List<CombatLogEntry>> GetLogsForRoundAsync(int encounterId, int round)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<CombatLogEntry>()
                .Where(e => e.CombatEncounterId == encounterId && e.Round == round)
                .OrderBy(e => e.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting combat logs for round: {ex.Message}");
            return new List<CombatLogEntry>();
        }
    }

    public async Task ClearLogsForEncounterAsync(int encounterId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.ExecuteAsync("DELETE FROM CombatLogEntry WHERE CombatEncounterId = ?", encounterId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing combat logs: {ex.Message}");
        }
    }

    public void ClearCurrentSessionLog()
    {
        _currentSessionLog.Clear();
    }

    private async Task AddLogEntryAsync(CombatLogEntry entry)
    {
        _currentSessionLog.Add(entry);
        LogEntryAdded?.Invoke(this, entry);

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(entry);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving combat log entry: {ex.Message}");
        }
    }
}
