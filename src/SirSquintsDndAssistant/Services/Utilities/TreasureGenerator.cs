namespace SirSquintsDndAssistant.Services.Utilities;

public class TreasureGenerator
{
    private readonly Random _random = new();
    private readonly DiceRoller _diceRoller = new();

    public string GenerateTreasure(int challengeRating)
    {
        int copper = 0, silver = 0, gold = 0, platinum = 0;

        if (challengeRating <= 4)
        {
            copper = _diceRoller.RollMultiple(5, 6) * 100;
            silver = _diceRoller.RollMultiple(4, 6) * 10;
            gold = _diceRoller.RollMultiple(3, 6);
        }
        else if (challengeRating <= 10)
        {
            silver = _diceRoller.RollMultiple(1, 6) * 100;
            gold = _diceRoller.RollMultiple(2, 6) * 10;
            platinum = _diceRoller.RollMultiple(3, 6);
        }
        else if (challengeRating <= 16)
        {
            gold = _diceRoller.RollMultiple(2, 6) * 100;
            platinum = _diceRoller.RollMultiple(1, 6) * 10;
        }
        else
        {
            gold = _diceRoller.RollMultiple(1, 6) * 1000;
            platinum = _diceRoller.RollMultiple(1, 6) * 100;
        }

        var result = "Treasure Found:\n";
        if (copper > 0) result += $"{copper} CP  ";
        if (silver > 0) result += $"{silver} SP  ";
        if (gold > 0) result += $"{gold} GP  ";
        if (platinum > 0) result += $"{platinum} PP";

        if (challengeRating >= 5 && _random.Next(100) < 30)
            result += "\n\nMagic Item Found!";

        return result;
    }
}
