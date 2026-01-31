using SQLite;

namespace SirSquintsDndAssistant.Models.Homebrew;

/// <summary>
/// A user-created custom item (weapon, armor, or magic item).
/// </summary>
public class HomebrewItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public string Category { get; set; } = string.Empty; // "Weapon", "Armor", "Potion", "Ring", "Wondrous Item", etc.

    // Rarity (for magic items)
    public bool IsMagic { get; set; }
    public string Rarity { get; set; } = "Common"; // Common, Uncommon, Rare, Very Rare, Legendary, Artifact

    // Attunement
    public bool RequiresAttunement { get; set; }
    public string AttunementRequirement { get; set; } = string.Empty; // e.g., "by a spellcaster", "by a cleric"

    // Basic properties
    public string Description { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
    public string Cost { get; set; } = string.Empty;

    // Weapon properties
    public bool IsWeapon { get; set; }
    public string WeaponType { get; set; } = string.Empty; // "Simple Melee", "Martial Ranged", etc.
    public string DamageDice { get; set; } = string.Empty; // e.g., "1d8"
    public string DamageType { get; set; } = string.Empty; // "Slashing", "Piercing", "Bludgeoning"
    public string WeaponPropertiesJson { get; set; } = "[]"; // ["Finesse", "Light", "Versatile (1d10)"]
    public string Range { get; set; } = string.Empty; // For ranged weapons: "80/320"

    // Armor properties
    public bool IsArmor { get; set; }
    public string ArmorType { get; set; } = string.Empty; // "Light", "Medium", "Heavy", "Shield"
    public int BaseAC { get; set; }
    public bool AddDexModifier { get; set; }
    public int MaxDexModifier { get; set; } // 0 for no limit, 2 for medium armor
    public int StrengthRequirement { get; set; }
    public bool HasStealthDisadvantage { get; set; }

    // Magic item properties
    public string MagicPropertiesJson { get; set; } = "[]"; // List of special properties
    public int BonusToAttack { get; set; }
    public int BonusToAC { get; set; }
    public int BonusToDamage { get; set; }

    // Charges (for items with uses)
    public bool HasCharges { get; set; }
    public int MaxCharges { get; set; }
    public string RechargeRate { get; set; } = string.Empty; // "1d6+1 at dawn", "1d4 at dawn"
    public string ChargeEffect { get; set; } = string.Empty; // What happens when you use a charge

    // Cursed items
    public bool IsCursed { get; set; }
    public string CurseDescription { get; set; } = string.Empty;

    // Consumable
    public bool IsConsumable { get; set; }

    // Metadata
    public string ImagePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsFavorite { get; set; }

    // Helper properties
    public string RarityColor => Rarity?.ToLower() switch
    {
        "common" => "#808080",
        "uncommon" => "#1EFF00",
        "rare" => "#0070DD",
        "very rare" => "#A335EE",
        "legendary" => "#FF8000",
        "artifact" => "#E6CC80",
        _ => "#808080"
    };

    public string TypeDisplay
    {
        get
        {
            if (IsWeapon) return $"Weapon ({WeaponType})";
            if (IsArmor) return $"Armor ({ArmorType})";
            return Category;
        }
    }

    public string AttunementDisplay => RequiresAttunement
        ? string.IsNullOrEmpty(AttunementRequirement)
            ? "Requires Attunement"
            : $"Requires Attunement {AttunementRequirement}"
        : "";
}

public enum ItemType
{
    Weapon,
    Armor,
    Shield,
    Potion,
    Scroll,
    Wand,
    Rod,
    Staff,
    Ring,
    Amulet,
    WondrousItem,
    Ammunition,
    Tool,
    AdventuringGear,
    Other
}
