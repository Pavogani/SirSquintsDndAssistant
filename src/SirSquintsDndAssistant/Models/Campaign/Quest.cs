using SQLite;

namespace SirSquintsDndAssistant.Models.Campaign;

public class Quest
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Completed, Failed
    public int? ParentQuestId { get; set; } // For quest chains
    public DateTime? CompletedDate { get; set; }
    public DateTime Created { get; set; }
}
