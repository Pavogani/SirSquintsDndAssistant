using SQLite;

namespace SirSquintsDndAssistant.Models.Combat;

/// <summary>
/// Tracks a status effect on a combatant with duration and source information.
/// </summary>
public class StatusEffect
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int InitiativeEntryId { get; set; }
    public string TargetName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Source of the effect
    public string SourceName { get; set; } = string.Empty;
    public string SourceSpell { get; set; } = string.Empty;

    // Duration tracking
    public EffectDurationType DurationType { get; set; }
    public int DurationRounds { get; set; }
    public int RoundsRemaining { get; set; }

    // When the effect ends (for "until end of X's next turn" effects)
    public EffectEndTiming EndTiming { get; set; }
    public string EndOnTurnOf { get; set; } = string.Empty;

    // Save information
    public bool RequiresSave { get; set; }
    public string SaveType { get; set; } = string.Empty; // STR, DEX, CON, INT, WIS, CHA
    public int SaveDC { get; set; }
    public SaveTiming SaveTiming { get; set; }

    // Effect properties
    public bool IsConcentration { get; set; }
    public bool IsBeneficial { get; set; }
    public bool IsHidden { get; set; } // For DM-only effects

    // Round and turn tracking
    public int AppliedOnRound { get; set; }
    public int AppliedOnTurn { get; set; }

    // Timestamp
    public DateTime AppliedAt { get; set; } = DateTime.Now;

    // Helper for display
    public string DurationDisplay => DurationType switch
    {
        EffectDurationType.Instantaneous => "Instantaneous",
        EffectDurationType.Rounds => RoundsRemaining == 1 ? "1 round" : $"{RoundsRemaining} rounds",
        EffectDurationType.Minutes => $"{DurationRounds} minute(s)",
        EffectDurationType.Hours => $"{DurationRounds} hour(s)",
        EffectDurationType.UntilDispelled => "Until dispelled",
        EffectDurationType.UntilEndOfTurn => $"Until end of {EndOnTurnOf}'s turn",
        EffectDurationType.UntilStartOfTurn => $"Until start of {EndOnTurnOf}'s turn",
        EffectDurationType.SaveEnds => $"Save ends (DC {SaveDC} {SaveType})",
        EffectDurationType.Permanent => "Permanent",
        _ => "Unknown"
    };

    public string SaveDisplay => RequiresSave
        ? $"DC {SaveDC} {SaveType} ({SaveTiming})"
        : "No save";

    /// <summary>
    /// Called at the start of a round. Returns true if the effect has expired.
    /// </summary>
    public bool OnRoundStart()
    {
        if (DurationType == EffectDurationType.Rounds)
        {
            RoundsRemaining--;
            return RoundsRemaining <= 0;
        }
        return false;
    }

    /// <summary>
    /// Called when a creature's turn starts. Returns true if the effect has expired.
    /// </summary>
    public bool OnTurnStart(string creatureName)
    {
        if (DurationType == EffectDurationType.UntilStartOfTurn &&
            EndOnTurnOf.Equals(creatureName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Called when a creature's turn ends. Returns true if the effect has expired.
    /// </summary>
    public bool OnTurnEnd(string creatureName)
    {
        if (DurationType == EffectDurationType.UntilEndOfTurn &&
            EndOnTurnOf.Equals(creatureName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Creates common status effects.
    /// </summary>
    public static StatusEffect CreateCondition(string name, string targetName, string sourceName, int durationRounds = 0)
    {
        return new StatusEffect
        {
            Name = name,
            TargetName = targetName,
            SourceName = sourceName,
            DurationType = durationRounds > 0 ? EffectDurationType.Rounds : EffectDurationType.UntilDispelled,
            DurationRounds = durationRounds,
            RoundsRemaining = durationRounds,
            Description = GetConditionDescription(name)
        };
    }

    public static StatusEffect CreateSpellEffect(string spellName, string targetName, string casterName,
        int durationRounds, bool isConcentration, bool isBeneficial = false)
    {
        return new StatusEffect
        {
            Name = spellName,
            TargetName = targetName,
            SourceName = casterName,
            SourceSpell = spellName,
            DurationType = EffectDurationType.Rounds,
            DurationRounds = durationRounds,
            RoundsRemaining = durationRounds,
            IsConcentration = isConcentration,
            IsBeneficial = isBeneficial
        };
    }

    private static string GetConditionDescription(string conditionName) => conditionName.ToLower() switch
    {
        "blinded" => "Can't see. Attack rolls against have advantage, attacks have disadvantage.",
        "charmed" => "Can't attack the charmer. Charmer has advantage on social checks.",
        "deafened" => "Can't hear. Automatically fails hearing-based checks.",
        "frightened" => "Disadvantage on ability checks and attacks while source is visible. Can't willingly move closer.",
        "grappled" => "Speed becomes 0. Ends if grappler is incapacitated or moved apart.",
        "incapacitated" => "Can't take actions or reactions.",
        "invisible" => "Impossible to see without special senses. Attacks have advantage, attacks against have disadvantage.",
        "paralyzed" => "Incapacitated. Can't move or speak. Auto-fail STR/DEX saves. Attacks have advantage. Hits within 5ft are crits.",
        "petrified" => "Transformed to stone. Incapacitated. Resistant to all damage. Immune to poison and disease.",
        "poisoned" => "Disadvantage on attack rolls and ability checks.",
        "prone" => "Can only crawl. Disadvantage on attacks. Attacks within 5ft have advantage, ranged attacks have disadvantage.",
        "restrained" => "Speed 0. Attacks have disadvantage. Attacks against have advantage. Disadvantage on DEX saves.",
        "stunned" => "Incapacitated. Can't move. Can only speak falteringly. Auto-fail STR/DEX saves. Attacks have advantage.",
        "unconscious" => "Incapacitated. Can't move or speak. Unaware of surroundings. Drops held items. Falls prone. Auto-fail STR/DEX saves. Attacks have advantage. Hits within 5ft are crits.",
        "exhaustion" => "Varies by level. See exhaustion rules.",
        _ => ""
    };
}

public enum EffectDurationType
{
    Instantaneous,
    Rounds,
    Minutes,
    Hours,
    UntilDispelled,
    UntilEndOfTurn,
    UntilStartOfTurn,
    SaveEnds,
    Permanent
}

public enum EffectEndTiming
{
    None,
    EndOfSourceTurn,
    StartOfSourceTurn,
    EndOfTargetTurn,
    StartOfTargetTurn
}

public enum SaveTiming
{
    None,
    WhenApplied,
    StartOfTurn,
    EndOfTurn,
    WhenDamaged
}
