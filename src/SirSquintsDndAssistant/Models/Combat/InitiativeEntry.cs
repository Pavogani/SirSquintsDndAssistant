using SQLite;

namespace SirSquintsDndAssistant.Models.Combat;

public class InitiativeEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CombatEncounterId { get; set; }
    public string CombatantType { get; set; } = string.Empty; // "Monster", "NPC", "Player"
    public int? ReferenceId { get; set; } // MonsterID, NPCID, etc.
    public string Name { get; set; } = string.Empty;
    public int Initiative { get; set; }
    public int InitiativeBonus { get; set; }
    public int CurrentHitPoints { get; set; }
    public int MaxHitPoints { get; set; }
    public int TempHitPoints { get; set; }
    public int ArmorClass { get; set; }
    public string ConditionsJson { get; set; } = string.Empty;
    public bool IsDefeated { get; set; }
    public int SortOrder { get; set; }

    // Concentration tracking
    public bool IsConcentrating { get; set; }
    public string ConcentrationSpell { get; set; } = string.Empty;

    // Death saves (for players at 0 HP)
    public int DeathSaveSuccesses { get; set; }
    public int DeathSaveFailures { get; set; }

    [Ignore]
    public bool HasTempHp => TempHitPoints > 0;

    [Ignore]
    public bool NeedsDeathSaves => CombatantType == "Player" && CurrentHitPoints <= 0 && !IsDefeated;

    [Ignore]
    public bool IsStabilized => DeathSaveSuccesses >= 3;

    [Ignore]
    public bool IsDead => DeathSaveFailures >= 3;
}
