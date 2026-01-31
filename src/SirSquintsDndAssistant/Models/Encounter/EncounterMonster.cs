namespace SirSquintsDndAssistant.Models.Encounter;

public class EncounterMonster
{
    public int MonsterId { get; set; }
    public string MonsterName { get; set; } = string.Empty;
    public double ChallengeRating { get; set; }
    public int ExperiencePoints { get; set; }
    public int Quantity { get; set; } = 1;
    public int TotalXp => ExperiencePoints * Quantity;
}
