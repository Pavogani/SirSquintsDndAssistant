using System.Text.Json;
using System.Text.Json.Serialization;

namespace SirSquintsDndAssistant.Models.Api;

// Open5e uses pagination
public class Open5eMonsterListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("results")]
    public List<Open5eMonster> Results { get; set; } = new();
}

public class Open5eMonster
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    [JsonPropertyName("alignment")]
    public string Alignment { get; set; } = string.Empty;

    [JsonPropertyName("armor_class")]
    public int ArmorClass { get; set; }

    [JsonPropertyName("armor_desc")]
    public string? ArmorDesc { get; set; }

    [JsonPropertyName("hit_points")]
    public int HitPoints { get; set; }

    [JsonPropertyName("hit_dice")]
    public string HitDice { get; set; } = string.Empty;

    [JsonPropertyName("challenge_rating")]
    public string ChallengeRating { get; set; } = string.Empty;

    [JsonPropertyName("cr")]
    public double? CR { get; set; }

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

    // Speed can have mixed types (int for distances, bool for hover)
    // Use JsonElement to handle this flexibility
    [JsonPropertyName("speed")]
    public JsonElement? Speed { get; set; }

    [JsonPropertyName("senses")]
    public string? Senses { get; set; }

    [JsonPropertyName("languages")]
    public string? Languages { get; set; }

    [JsonPropertyName("damage_vulnerabilities")]
    public string? DamageVulnerabilities { get; set; }

    [JsonPropertyName("damage_resistances")]
    public string? DamageResistances { get; set; }

    [JsonPropertyName("damage_immunities")]
    public string? DamageImmunities { get; set; }

    [JsonPropertyName("condition_immunities")]
    public string? ConditionImmunities { get; set; }

    // Actions is an array of action objects
    [JsonPropertyName("actions")]
    public List<Open5eAction>? Actions { get; set; }

    // Special abilities is an array of ability objects
    [JsonPropertyName("special_abilities")]
    public List<Open5eSpecialAbility>? SpecialAbilities { get; set; }

    [JsonPropertyName("legendary_actions")]
    public List<Open5eLegendaryAction>? LegendaryActions { get; set; }

    [JsonPropertyName("legendary_desc")]
    public string? LegendaryDesc { get; set; }

    [JsonPropertyName("reactions")]
    public List<Open5eReaction>? Reactions { get; set; }

    [JsonPropertyName("document__slug")]
    public string DocumentSlug { get; set; } = string.Empty;

    [JsonPropertyName("document__title")]
    public string? DocumentTitle { get; set; }

    [JsonPropertyName("desc")]
    public string? Description { get; set; }

    [JsonPropertyName("img_main")]
    public string? ImageUrl { get; set; }

    // Helper to get speed as a readable string
    public string GetSpeedString()
    {
        if (Speed == null || Speed.Value.ValueKind == JsonValueKind.Null)
            return "30 ft.";

        var speeds = new List<string>();
        foreach (var prop in Speed.Value.EnumerateObject())
        {
            if (prop.Name == "hover")
                continue; // Skip hover, it's a modifier

            var value = prop.Value.ValueKind == JsonValueKind.Number
                ? prop.Value.GetInt32()
                : 0;

            if (prop.Name == "walk")
                speeds.Insert(0, $"{value} ft.");
            else
                speeds.Add($"{prop.Name} {value} ft.");
        }

        // Check for hover
        if (Speed.Value.TryGetProperty("hover", out var hover) &&
            hover.ValueKind == JsonValueKind.True)
        {
            speeds.Add("(hover)");
        }

        return speeds.Count > 0 ? string.Join(", ", speeds) : "30 ft.";
    }
}

public class Open5eAction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("attack_bonus")]
    public int? AttackBonus { get; set; }

    [JsonPropertyName("damage_dice")]
    public string? DamageDice { get; set; }
}

public class Open5eSpecialAbility
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;
}

public class Open5eLegendaryAction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;
}

public class Open5eReaction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;
}
