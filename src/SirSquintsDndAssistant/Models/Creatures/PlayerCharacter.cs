using SQLite;

namespace SirSquintsDndAssistant.Models.Creatures;

public class PlayerCharacter
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Race { get; set; } = string.Empty;
    public int ArmorClass { get; set; }
    public int MaxHitPoints { get; set; }
    public int PassivePerception { get; set; }
    public string DndBeyondCharacterId { get; set; } = string.Empty;
    public string FullDataJson { get; set; } = string.Empty; // Complete import data
    public DateTime LastImported { get; set; }
}
