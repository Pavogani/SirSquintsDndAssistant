using SQLite;

namespace SirSquintsDndAssistant.Models.Encounter;

public class EncounterTemplate
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty; // Easy, Medium, Hard, Deadly
    public int PartyLevel { get; set; }
    public int PartySize { get; set; }
    public string MonstersJson { get; set; } = string.Empty; // Array of {monsterId, quantity}
    public int? CampaignId { get; set; }
    public DateTime Created { get; set; }
}
