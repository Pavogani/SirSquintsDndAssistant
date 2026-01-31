namespace SirSquintsDndAssistant.Services.Encounter;

public class DifficultyCalculator : IDifficultyCalculator
{
    // XP thresholds per character level (Easy, Medium, Hard, Deadly)
    private readonly Dictionary<int, (int Easy, int Medium, int Hard, int Deadly)> _xpThresholds = new()
    {
        { 1, (25, 50, 75, 100) },
        { 2, (50, 100, 150, 200) },
        { 3, (75, 150, 225, 400) },
        { 4, (125, 250, 375, 500) },
        { 5, (250, 500, 750, 1100) },
        { 6, (300, 600, 900, 1400) },
        { 7, (350, 750, 1100, 1700) },
        { 8, (450, 900, 1400, 2100) },
        { 9, (550, 1100, 1600, 2400) },
        { 10, (600, 1200, 1900, 2800) },
        { 11, (800, 1600, 2400, 3600) },
        { 12, (1000, 2000, 3000, 4500) },
        { 13, (1100, 2200, 3400, 5100) },
        { 14, (1250, 2500, 3800, 5700) },
        { 15, (1400, 2800, 4300, 6400) },
        { 16, (1600, 3200, 4800, 7200) },
        { 17, (2000, 3900, 5900, 8800) },
        { 18, (2100, 4200, 6300, 9500) },
        { 19, (2400, 4900, 7300, 10900) },
        { 20, (2800, 5700, 8500, 12700) }
    };

    // Multipliers based on number of monsters
    private readonly Dictionary<int, double> _multipliers = new()
    {
        { 1, 1.0 },
        { 2, 1.5 },
        { 3, 2.0 },
        { 4, 2.0 },
        { 5, 2.0 },
        { 6, 2.0 },
        { 7, 2.5 },
        { 8, 2.5 },
        { 9, 2.5 },
        { 10, 2.5 },
        { 11, 3.0 },
        { 12, 3.0 },
        { 13, 3.0 },
        { 14, 3.0 },
        { 15, 4.0 }
    };

    public int GetXpThreshold(int partyLevel, int partySize, string difficulty)
    {
        if (!_xpThresholds.ContainsKey(partyLevel))
            return 0;

        var thresholds = _xpThresholds[partyLevel];
        var perCharacter = difficulty.ToLower() switch
        {
            "easy" => thresholds.Easy,
            "medium" => thresholds.Medium,
            "hard" => thresholds.Hard,
            "deadly" => thresholds.Deadly,
            _ => 0
        };

        return perCharacter * partySize;
    }

    public string CalculateDifficulty(int totalXp, int partyLevel, int partySize)
    {
        if (!_xpThresholds.ContainsKey(partyLevel))
            return "Unknown";

        var thresholds = _xpThresholds[partyLevel];
        var totalEasy = thresholds.Easy * partySize;
        var totalMedium = thresholds.Medium * partySize;
        var totalHard = thresholds.Hard * partySize;
        var totalDeadly = thresholds.Deadly * partySize;

        if (totalXp < totalEasy)
            return "Trivial";
        else if (totalXp < totalMedium)
            return "Easy";
        else if (totalXp < totalHard)
            return "Medium";
        else if (totalXp < totalDeadly)
            return "Hard";
        else
            return "Deadly";
    }

    public int CalculateAdjustedXp(int baseXp, int monsterCount)
    {
        double multiplier = 1.0;

        if (monsterCount >= 15)
            multiplier = 4.0;
        else if (monsterCount >= 11)
            multiplier = 3.0;
        else if (monsterCount >= 7)
            multiplier = 2.5;
        else if (monsterCount >= 3)
            multiplier = 2.0;
        else if (monsterCount == 2)
            multiplier = 1.5;
        else
            multiplier = 1.0;

        return (int)(baseXp * multiplier);
    }
}
