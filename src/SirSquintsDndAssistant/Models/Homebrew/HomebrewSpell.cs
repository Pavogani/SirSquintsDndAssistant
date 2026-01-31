using SQLite;

namespace SirSquintsDndAssistant.Models.Homebrew;

/// <summary>
/// A user-created custom spell.
/// </summary>
public class HomebrewSpell
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } // 0 = cantrip
    public string School { get; set; } = "Evocation"; // Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion, Necromancy, Transmutation

    public bool IsRitual { get; set; }

    // Casting
    public string CastingTime { get; set; } = "1 action";
    public string Range { get; set; } = "60 feet";
    public bool RequiresVerbal { get; set; } = true;
    public bool RequiresSomatic { get; set; } = true;
    public bool RequiresMaterial { get; set; }
    public string MaterialComponents { get; set; } = string.Empty;
    public bool MaterialConsumed { get; set; }
    public string MaterialCost { get; set; } = string.Empty; // e.g., "500 gp"

    // Duration
    public string Duration { get; set; } = "Instantaneous";
    public bool RequiresConcentration { get; set; }

    // Effect
    public string Description { get; set; } = string.Empty;
    public string HigherLevels { get; set; } = string.Empty;

    // Damage/Healing (if applicable)
    public bool DealsDamage { get; set; }
    public string DamageType { get; set; } = string.Empty;
    public string DamageDice { get; set; } = string.Empty;
    public string DamageScaling { get; set; } = string.Empty; // For cantrips: "character level", for leveled: "spell level"

    public bool Heals { get; set; }
    public string HealingDice { get; set; } = string.Empty;

    // Save/Attack
    public bool RequiresSave { get; set; }
    public string SaveType { get; set; } = string.Empty; // STR, DEX, CON, INT, WIS, CHA
    public string SaveEffect { get; set; } = string.Empty; // "half damage", "negates", etc.

    public bool IsSpellAttack { get; set; }
    public string SpellAttackType { get; set; } = string.Empty; // "melee", "ranged"

    // Target
    public string TargetType { get; set; } = "Single"; // "Single", "Multiple", "Area", "Self"
    public string AreaShape { get; set; } = string.Empty; // "Cone", "Cube", "Cylinder", "Line", "Sphere"
    public string AreaSize { get; set; } = string.Empty; // e.g., "20-foot radius"

    // Classes (stored as JSON array)
    public string ClassesJson { get; set; } = "[]";

    // Metadata
    public string Notes { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsFavorite { get; set; }

    // Helper properties
    public string LevelDisplay => Level == 0 ? "Cantrip" : $"Level {Level}";

    public string ComponentsDisplay
    {
        get
        {
            var components = new List<string>();
            if (RequiresVerbal) components.Add("V");
            if (RequiresSomatic) components.Add("S");
            if (RequiresMaterial) components.Add($"M ({MaterialComponents})");
            return string.Join(", ", components);
        }
    }

    public string ConcentrationDisplay => RequiresConcentration ? "Concentration, " + Duration : Duration;
}
