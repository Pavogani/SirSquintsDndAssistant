using System.Text.Json.Serialization;

namespace SirSquintsDndAssistant.Models.Api;

/// <summary>
/// Models for parsing 5e.tools JSON format (bestiary, spells, items).
/// </summary>

#region Monster Models

/// <summary>
/// Root object for 5e.tools bestiary JSON files.
/// </summary>
public class FiveEToolsBestiary
{
    [JsonPropertyName("monster")]
    public List<FiveEToolsMonster>? Monster { get; set; }
}

/// <summary>
/// 5e.tools monster format.
/// </summary>
public class FiveEToolsMonster
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("size")]
    public List<string>? Size { get; set; }

    [JsonPropertyName("type")]
    public object? Type { get; set; } // Can be string or object

    [JsonPropertyName("alignment")]
    public List<object>? Alignment { get; set; }

    [JsonPropertyName("ac")]
    public List<object>? Ac { get; set; }

    [JsonPropertyName("hp")]
    public FiveEToolsHp? Hp { get; set; }

    [JsonPropertyName("speed")]
    public FiveEToolsSpeed? Speed { get; set; }

    [JsonPropertyName("str")]
    public int Str { get; set; }

    [JsonPropertyName("dex")]
    public int Dex { get; set; }

    [JsonPropertyName("con")]
    public int Con { get; set; }

    [JsonPropertyName("int")]
    public int Int { get; set; }

    [JsonPropertyName("wis")]
    public int Wis { get; set; }

    [JsonPropertyName("cha")]
    public int Cha { get; set; }

    [JsonPropertyName("save")]
    public Dictionary<string, string>? Save { get; set; }

    [JsonPropertyName("skill")]
    public Dictionary<string, string>? Skill { get; set; }

    [JsonPropertyName("resist")]
    public List<object>? Resist { get; set; }

    [JsonPropertyName("immune")]
    public List<object>? Immune { get; set; }

    [JsonPropertyName("vulnerable")]
    public List<object>? Vulnerable { get; set; }

    [JsonPropertyName("conditionImmune")]
    public List<string>? ConditionImmune { get; set; }

    [JsonPropertyName("senses")]
    public List<string>? Senses { get; set; }

    [JsonPropertyName("passive")]
    public int? Passive { get; set; }

    [JsonPropertyName("languages")]
    public List<string>? Languages { get; set; }

    [JsonPropertyName("cr")]
    public object? Cr { get; set; } // Can be string or object

    [JsonPropertyName("trait")]
    public List<FiveEToolsEntry>? Trait { get; set; }

    [JsonPropertyName("action")]
    public List<FiveEToolsEntry>? Action { get; set; }

    [JsonPropertyName("reaction")]
    public List<FiveEToolsEntry>? Reaction { get; set; }

    [JsonPropertyName("legendary")]
    public List<FiveEToolsEntry>? Legendary { get; set; }

    [JsonPropertyName("legendaryActions")]
    public int? LegendaryActions { get; set; }

    [JsonPropertyName("spellcasting")]
    public List<FiveEToolsSpellcasting>? Spellcasting { get; set; }

    [JsonPropertyName("environment")]
    public List<string>? Environment { get; set; }
}

public class FiveEToolsHp
{
    [JsonPropertyName("average")]
    public int Average { get; set; }

    [JsonPropertyName("formula")]
    public string? Formula { get; set; }
}

public class FiveEToolsSpeed
{
    [JsonPropertyName("walk")]
    public object? Walk { get; set; } // Can be int or object

    [JsonPropertyName("fly")]
    public object? Fly { get; set; }

    [JsonPropertyName("swim")]
    public object? Swim { get; set; }

    [JsonPropertyName("climb")]
    public object? Climb { get; set; }

    [JsonPropertyName("burrow")]
    public object? Burrow { get; set; }

    [JsonPropertyName("hover")]
    public bool? Hover { get; set; }

    [JsonPropertyName("canHover")]
    public bool? CanHover { get; set; }
}

public class FiveEToolsEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<object>? Entries { get; set; }
}

public class FiveEToolsSpellcasting
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("headerEntries")]
    public List<string>? HeaderEntries { get; set; }

    [JsonPropertyName("spells")]
    public Dictionary<string, FiveEToolsSpellLevel>? Spells { get; set; }

    [JsonPropertyName("will")]
    public List<string>? Will { get; set; }

    [JsonPropertyName("daily")]
    public Dictionary<string, List<string>>? Daily { get; set; }
}

public class FiveEToolsSpellLevel
{
    [JsonPropertyName("slots")]
    public int? Slots { get; set; }

    [JsonPropertyName("spells")]
    public List<string>? Spells { get; set; }
}

#endregion

#region Spell Models

/// <summary>
/// Root object for 5e.tools spell JSON files.
/// </summary>
public class FiveEToolsSpellbook
{
    [JsonPropertyName("spell")]
    public List<FiveEToolsSpell>? Spell { get; set; }
}

public class FiveEToolsSpell
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("school")]
    public string School { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public List<FiveEToolsTime>? Time { get; set; }

    [JsonPropertyName("range")]
    public FiveEToolsRange? Range { get; set; }

    [JsonPropertyName("components")]
    public FiveEToolsComponents? Components { get; set; }

    [JsonPropertyName("duration")]
    public List<FiveEToolsDuration>? Duration { get; set; }

    [JsonPropertyName("entries")]
    public List<object>? Entries { get; set; }

    [JsonPropertyName("entriesHigherLevel")]
    public List<FiveEToolsEntry>? EntriesHigherLevel { get; set; }

    [JsonPropertyName("classes")]
    public FiveEToolsClasses? Classes { get; set; }
}

public class FiveEToolsTime
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class FiveEToolsRange
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public FiveEToolsDistance? Distance { get; set; }
}

public class FiveEToolsDistance
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int? Amount { get; set; }
}

public class FiveEToolsComponents
{
    [JsonPropertyName("v")]
    public bool V { get; set; }

    [JsonPropertyName("s")]
    public bool S { get; set; }

    [JsonPropertyName("m")]
    public object? M { get; set; } // Can be string or object
}

public class FiveEToolsDuration
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public FiveEToolsDurationDetails? Duration { get; set; }

    [JsonPropertyName("concentration")]
    public bool? Concentration { get; set; }
}

public class FiveEToolsDurationDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int? Amount { get; set; }
}

public class FiveEToolsClasses
{
    [JsonPropertyName("fromClassList")]
    public List<FiveEToolsClassRef>? FromClassList { get; set; }
}

public class FiveEToolsClassRef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

#endregion

#region Item Models

/// <summary>
/// Root object for 5e.tools item JSON files.
/// </summary>
public class FiveEToolsItems
{
    [JsonPropertyName("item")]
    public List<FiveEToolsItem>? Item { get; set; }

    [JsonPropertyName("baseitem")]
    public List<FiveEToolsItem>? BaseItem { get; set; }

    [JsonPropertyName("magicvariant")]
    public List<FiveEToolsMagicVariant>? MagicVariant { get; set; }
}

public class FiveEToolsItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; } // In copper pieces

    [JsonPropertyName("entries")]
    public List<object>? Entries { get; set; }

    [JsonPropertyName("reqAttune")]
    public object? ReqAttune { get; set; }

    [JsonPropertyName("wondrous")]
    public bool? Wondrous { get; set; }

    [JsonPropertyName("weapon")]
    public bool? Weapon { get; set; }

    [JsonPropertyName("weaponCategory")]
    public string? WeaponCategory { get; set; }

    [JsonPropertyName("dmg1")]
    public string? Dmg1 { get; set; }

    [JsonPropertyName("dmgType")]
    public string? DmgType { get; set; }

    [JsonPropertyName("property")]
    public List<string>? Property { get; set; }

    [JsonPropertyName("armor")]
    public bool? Armor { get; set; }

    [JsonPropertyName("ac")]
    public int? Ac { get; set; }

    [JsonPropertyName("strength")]
    public int? Strength { get; set; }

    [JsonPropertyName("stealth")]
    public bool? Stealth { get; set; }
}

public class FiveEToolsMagicVariant
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("requires")]
    public List<Dictionary<string, object>>? Requires { get; set; }

    [JsonPropertyName("inherits")]
    public FiveEToolsInherits? Inherits { get; set; }
}

public class FiveEToolsInherits
{
    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("entries")]
    public List<object>? Entries { get; set; }

    [JsonPropertyName("namePrefix")]
    public string? NamePrefix { get; set; }

    [JsonPropertyName("nameSuffix")]
    public string? NameSuffix { get; set; }
}

#endregion

#region Kobold Fight Club / CritterDB Models

/// <summary>
/// Kobold Fight Club encounter/monster export format.
/// </summary>
public class KoboldFightClubExport
{
    public List<KfcMonster>? Monsters { get; set; }
}

public class KfcMonster
{
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
    public string Ac { get; set; } = string.Empty;
    public string Hp { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Cha { get; set; }
    public string? Cr { get; set; }
    public string? Traits { get; set; }
    public string? Actions { get; set; }
    public string? LegendaryActions { get; set; }
}

/// <summary>
/// CritterDB export format.
/// </summary>
public class CritterDbExport
{
    public List<CritterDbCreature>? Creatures { get; set; }
}

public class CritterDbCreature
{
    public string Name { get; set; } = string.Empty;
    public CritterDbStats? Stats { get; set; }
    public string? Flavor { get; set; }
}

public class CritterDbStats
{
    public string Size { get; set; } = string.Empty;
    public string Race { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
    public int ArmorClass { get; set; }
    public int HitPoints { get; set; }
    public string? HitPointsStr { get; set; }
    public int Speed { get; set; }
    public CritterDbAbilityScores? AbilityScores { get; set; }
    public double ChallengeRating { get; set; }
    public List<CritterDbAbility>? Abilities { get; set; }
    public List<CritterDbAbility>? Actions { get; set; }
    public List<CritterDbAbility>? Reactions { get; set; }
    public List<CritterDbAbility>? LegendaryActions { get; set; }
}

public class CritterDbAbilityScores
{
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
}

public class CritterDbAbility
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

#endregion
