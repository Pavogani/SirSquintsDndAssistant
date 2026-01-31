using SQLite;

namespace SirSquintsDndAssistant.Models.Content;

public class MagicItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ApiId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool RequiresAttunement { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty; // Local cached/custom image
    public string Source { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
