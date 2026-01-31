using SQLite;

namespace SirSquintsDndAssistant.Models.Creatures;

public class NPC
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Race { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;

    // Can reference a monster stat block
    public int? BaseMonsterTemplateId { get; set; }

    // Or custom stats
    public string StatBlockJson { get; set; } = string.Empty;

    public int? CampaignId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }

    [Ignore]
    public bool HasImage => !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);
}
