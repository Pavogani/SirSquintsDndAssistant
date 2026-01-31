namespace SirSquintsDndAssistant.Services.Encounter;

public interface IDifficultyCalculator
{
    int GetXpThreshold(int partyLevel, int partySize, string difficulty);
    string CalculateDifficulty(int totalXp, int partyLevel, int partySize);
    int CalculateAdjustedXp(int baseXp, int monsterCount);
}
