using SQLite;

namespace SirSquintsDndAssistant.Models.Combat;

public class CombatLogEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CombatEncounterId { get; set; }
    public int Round { get; set; }
    public DateTime Timestamp { get; set; }

    public string ActorName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;

    public CombatLogType LogType { get; set; }
    public string Description { get; set; } = string.Empty;

    // Numerical data
    public int? DamageDealt { get; set; }
    public int? HealingDone { get; set; }
    public int? DiceRoll { get; set; }
    public int? DiceTotal { get; set; }

    // Status changes
    public string? ConditionApplied { get; set; }
    public string? ConditionRemoved { get; set; }

    // For display
    public string FormattedEntry => FormatEntry();

    private string FormatEntry()
    {
        return LogType switch
        {
            CombatLogType.Attack => $"{ActorName} attacks {TargetName}",
            CombatLogType.Damage => $"{ActorName} deals {DamageDealt} damage to {TargetName}",
            CombatLogType.Heal => $"{ActorName} heals {TargetName} for {HealingDone} HP",
            CombatLogType.Kill => $"{ActorName} defeats {TargetName}!",
            CombatLogType.Death => $"{ActorName} has fallen!",
            CombatLogType.ConditionApplied => $"{TargetName} is now {ConditionApplied}",
            CombatLogType.ConditionRemoved => $"{TargetName} is no longer {ConditionRemoved}",
            CombatLogType.TurnStart => $"--- {ActorName}'s Turn ---",
            CombatLogType.RoundStart => $"=== ROUND {Round} ===",
            CombatLogType.CombatStart => $"*** COMBAT BEGINS: {Description} ***",
            CombatLogType.CombatEnd => $"*** COMBAT ENDS ***",
            CombatLogType.InitiativeRoll => $"{ActorName} rolls {DiceRoll} + bonus = {DiceTotal} initiative",
            CombatLogType.SavingThrow => $"{ActorName} {(DiceTotal >= DiceRoll ? "succeeds" : "fails")} saving throw ({DiceTotal})",
            CombatLogType.SpellCast => $"{ActorName} casts {Description}",
            CombatLogType.Concentration => $"{ActorName} {Description}",
            CombatLogType.DeathSave => $"{ActorName} death save: {Description}",
            CombatLogType.Custom => Description,
            _ => Description
        };
    }
}

public enum CombatLogType
{
    Attack,
    Damage,
    Heal,
    Kill,
    Death,
    ConditionApplied,
    ConditionRemoved,
    TurnStart,
    RoundStart,
    CombatStart,
    CombatEnd,
    InitiativeRoll,
    SavingThrow,
    SpellCast,
    Concentration,
    DeathSave,
    Custom
}
