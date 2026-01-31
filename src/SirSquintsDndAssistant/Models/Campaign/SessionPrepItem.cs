using SQLite;

namespace SirSquintsDndAssistant.Models.Campaign;

/// <summary>
/// Represents a session preparation item/agenda point.
/// </summary>
public class SessionPrepItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int SessionId { get; set; }
    public int CampaignId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public PrepItemType ItemType { get; set; }
    public PrepItemPriority Priority { get; set; } = PrepItemPriority.Normal;

    // For encounters
    public int? EncounterId { get; set; }

    // For NPCs
    public int? NpcId { get; set; }

    // For locations
    public string LocationName { get; set; } = string.Empty;
    public string LocationDescription { get; set; } = string.Empty;

    // For scenes
    public string ReadAloudText { get; set; } = string.Empty;

    // Tracking
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Notes
    public string Notes { get; set; } = string.Empty;
}

public enum PrepItemType
{
    Agenda,
    Scene,
    Encounter,
    NpcInteraction,
    Location,
    PlotPoint,
    Treasure,
    Puzzle,
    Roleplay,
    Combat,
    Note,
    Other
}

public enum PrepItemPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Wiki entry for campaign lore.
/// </summary>
public class WikiEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // Markdown supported
    public WikiCategory Category { get; set; }

    // Cross-references (stored as JSON)
    public string RelatedEntriesJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";

    // For locations
    public string? ParentLocationId { get; set; }

    // For NPCs/Characters
    public int? LinkedNpcId { get; set; }

    // For factions
    public string FactionAlignment { get; set; } = string.Empty;
    public string FactionGoals { get; set; } = string.Empty;

    // Visibility
    public bool IsSecret { get; set; } // DM only
    public bool IsPlayerKnown { get; set; } // Players have discovered this

    public string ImagePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public enum WikiCategory
{
    Location,
    Character,
    Faction,
    Deity,
    Item,
    Lore,
    History,
    Quest,
    Event,
    Creature,
    Other
}
