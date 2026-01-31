using System.Text.Json.Serialization;

namespace SirSquintsDndAssistant.Models.Api;

// List response for endpoints like /monsters, /spells
public class Dnd5eApiListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<Dnd5eApiReference> Results { get; set; } = new();
}

public class Dnd5eApiReference
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

// Monster detail response
public class Dnd5eMonsterResponse
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("alignment")]
    public string Alignment { get; set; } = string.Empty;

    [JsonPropertyName("armor_class")]
    public List<Dnd5eArmorClass> ArmorClass { get; set; } = new();

    [JsonPropertyName("hit_points")]
    public int HitPoints { get; set; }

    [JsonPropertyName("hit_dice")]
    public string HitDice { get; set; } = string.Empty;

    [JsonPropertyName("challenge_rating")]
    public double ChallengeRating { get; set; }

    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    [JsonPropertyName("strength")]
    public int Strength { get; set; }

    [JsonPropertyName("dexterity")]
    public int Dexterity { get; set; }

    [JsonPropertyName("constitution")]
    public int Constitution { get; set; }

    [JsonPropertyName("intelligence")]
    public int Intelligence { get; set; }

    [JsonPropertyName("wisdom")]
    public int Wisdom { get; set; }

    [JsonPropertyName("charisma")]
    public int Charisma { get; set; }

    [JsonPropertyName("speed")]
    public Dictionary<string, string> Speed { get; set; } = new();

    [JsonPropertyName("proficiencies")]
    public List<Dnd5eProficiency> Proficiencies { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<Dnd5eAction> Actions { get; set; } = new();

    [JsonPropertyName("special_abilities")]
    public List<Dnd5eSpecialAbility> SpecialAbilities { get; set; } = new();
}

public class Dnd5eArmorClass
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class Dnd5eProficiency
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("proficiency")]
    public Dnd5eApiReference Proficiency { get; set; } = new();
}

public class Dnd5eAction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;
}

public class Dnd5eSpecialAbility
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;
}

// Condition detail response
public class Dnd5eConditionResponse
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public List<string> Desc { get; set; } = new();
}

// Spell detail response
public class Dnd5eSpellResponse
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("school")]
    public Dnd5eApiReference School { get; set; } = new();

    [JsonPropertyName("casting_time")]
    public string CastingTime { get; set; } = string.Empty;

    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = new();

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public List<string> Desc { get; set; } = new();

    [JsonPropertyName("higher_level")]
    public List<string> HigherLevel { get; set; } = new();

    [JsonPropertyName("classes")]
    public List<Dnd5eApiReference> Classes { get; set; } = new();
}

// Equipment detail response
public class Dnd5eEquipmentDetailResponse
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("equipment_category")]
    public Dnd5eApiReference? EquipmentCategory { get; set; }

    [JsonPropertyName("weapon_category")]
    public string? WeaponCategory { get; set; }

    [JsonPropertyName("weapon_range")]
    public string? WeaponRange { get; set; }

    [JsonPropertyName("armor_category")]
    public string? ArmorCategory { get; set; }

    [JsonPropertyName("cost")]
    public Dnd5eCost? Cost { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("desc")]
    public List<string> Desc { get; set; } = new();

    [JsonPropertyName("properties")]
    public List<Dnd5eApiReference> Properties { get; set; } = new();

    [JsonPropertyName("damage")]
    public Dnd5eDamage? Damage { get; set; }

    [JsonPropertyName("range")]
    public Dnd5eRange? Range { get; set; }

    [JsonPropertyName("armor_class")]
    public Dnd5eArmorClassEquipment? ArmorClass { get; set; }

    [JsonPropertyName("str_minimum")]
    public int StrMinimum { get; set; }

    [JsonPropertyName("stealth_disadvantage")]
    public bool StealthDisadvantage { get; set; }
}

public class Dnd5eCost
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "gp";
}

public class Dnd5eDamage
{
    [JsonPropertyName("damage_dice")]
    public string DamageDice { get; set; } = string.Empty;

    [JsonPropertyName("damage_type")]
    public Dnd5eApiReference? DamageType { get; set; }
}

public class Dnd5eRange
{
    [JsonPropertyName("normal")]
    public int Normal { get; set; }

    [JsonPropertyName("long")]
    public int? Long { get; set; }
}

public class Dnd5eArmorClassEquipment
{
    [JsonPropertyName("base")]
    public int Base { get; set; }

    [JsonPropertyName("dex_bonus")]
    public bool DexBonus { get; set; }

    [JsonPropertyName("max_bonus")]
    public int? MaxBonus { get; set; }
}

// Magic Item detail response
public class Dnd5eMagicItemDetailResponse
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("equipment_category")]
    public Dnd5eApiReference? EquipmentCategory { get; set; }

    [JsonPropertyName("rarity")]
    public Dnd5eApiReference? Rarity { get; set; }

    [JsonPropertyName("desc")]
    public List<string> Desc { get; set; } = new();

    [JsonPropertyName("variants")]
    public List<Dnd5eApiReference> Variants { get; set; } = new();

    [JsonPropertyName("variant")]
    public bool Variant { get; set; }
}
