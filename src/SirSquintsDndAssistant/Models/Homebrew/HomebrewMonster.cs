using SQLite;

namespace SirSquintsDndAssistant.Models.Homebrew;

/// <summary>
/// A user-created custom monster.
/// </summary>
public class HomebrewMonster
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = "Medium";
    public string Type { get; set; } = "Humanoid";
    public string Alignment { get; set; } = "Unaligned";

    // Stats
    public int ArmorClass { get; set; } = 10;
    public string ArmorType { get; set; } = string.Empty; // e.g., "natural armor", "leather armor"
    public int HitPoints { get; set; }
    public string HitDice { get; set; } = string.Empty; // e.g., "4d8+8"

    // Speed
    public int WalkSpeed { get; set; } = 30;
    public int FlySpeed { get; set; }
    public int SwimSpeed { get; set; }
    public int ClimbSpeed { get; set; }
    public int BurrowSpeed { get; set; }

    // Ability Scores
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;

    // Proficiencies and Resistances (stored as JSON)
    public string SavingThrowsJson { get; set; } = "[]";
    public string SkillsJson { get; set; } = "[]";
    public string DamageResistancesJson { get; set; } = "[]";
    public string DamageImmunitiesJson { get; set; } = "[]";
    public string DamageVulnerabilitiesJson { get; set; } = "[]";
    public string ConditionImmunitiesJson { get; set; } = "[]";
    public string SensesJson { get; set; } = "[]";
    public string LanguagesJson { get; set; } = "[]";

    // Challenge Rating
    public double ChallengeRating { get; set; }
    public int ExperiencePoints { get; set; }

    // Abilities (stored as JSON arrays)
    public string SpecialAbilitiesJson { get; set; } = "[]";
    public string ActionsJson { get; set; } = "[]";
    public string BonusActionsJson { get; set; } = "[]";
    public string ReactionsJson { get; set; } = "[]";
    public string LegendaryActionsJson { get; set; } = "[]";
    public string LairActionsJson { get; set; } = "[]";

    // Lair and Regional Effects
    public string LairDescription { get; set; } = string.Empty;
    public string RegionalEffectsJson { get; set; } = "[]";

    // Metadata
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty; // Comma-separated tags for organization

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsFavorite { get; set; }

    // Helper properties
    public int StrengthModifier => (Strength - 10) / 2;
    public int DexterityModifier => (Dexterity - 10) / 2;
    public int ConstitutionModifier => (Constitution - 10) / 2;
    public int IntelligenceModifier => (Intelligence - 10) / 2;
    public int WisdomModifier => (Wisdom - 10) / 2;
    public int CharismaModifier => (Charisma - 10) / 2;

    public string SpeedDisplay
    {
        get
        {
            var speeds = new List<string>();
            if (WalkSpeed > 0) speeds.Add($"{WalkSpeed} ft.");
            if (FlySpeed > 0) speeds.Add($"fly {FlySpeed} ft.");
            if (SwimSpeed > 0) speeds.Add($"swim {SwimSpeed} ft.");
            if (ClimbSpeed > 0) speeds.Add($"climb {ClimbSpeed} ft.");
            if (BurrowSpeed > 0) speeds.Add($"burrow {BurrowSpeed} ft.");
            return string.Join(", ", speeds);
        }
    }

    public string ChallengeRatingDisplay => ChallengeRating switch
    {
        0 => "0",
        0.125 => "1/8",
        0.25 => "1/4",
        0.5 => "1/2",
        _ => ChallengeRating.ToString()
    };
}

/// <summary>
/// Represents an ability, action, or reaction for a homebrew monster.
/// </summary>
public class MonsterAbility
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // For attacks
    public bool IsAttack { get; set; }
    public string AttackType { get; set; } = string.Empty; // "Melee Weapon Attack", "Ranged Weapon Attack", "Melee Spell Attack"
    public int AttackBonus { get; set; }
    public string Reach { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string HitDamage { get; set; } = string.Empty; // e.g., "2d6+4 slashing"

    // For recharge abilities
    public bool HasRecharge { get; set; }
    public int RechargeMin { get; set; } // Recharge on 5-6 = RechargeMin: 5
    public int RechargeMax { get; set; } = 6;

    // For limited use abilities
    public bool HasLimitedUse { get; set; }
    public int UsesPerDay { get; set; }
    public string UseResetTiming { get; set; } = string.Empty; // "Long Rest", "Short Rest", "Dawn"
}
