using SQLite;

namespace SirSquintsDndAssistant.Models.Content;

public class Condition
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ApiId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
