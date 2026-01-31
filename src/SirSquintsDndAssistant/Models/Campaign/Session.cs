using SQLite;

namespace SirSquintsDndAssistant.Models.Campaign;

public class Session
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public int SessionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string NotesMarkdown { get; set; } = string.Empty;
    public string SummaryMarkdown { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}
