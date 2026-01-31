namespace SirSquintsDndAssistant.Services.Utilities;

public class DiceRoller
{
    public int Roll(int sides)
    {
        return Random.Shared.Next(1, sides + 1);
    }

    public int RollMultiple(int count, int sides)
    {
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            total += Roll(sides);
        }
        return total;
    }

    public (int total, List<int> rolls) RollWithDetails(int count, int sides)
    {
        var rolls = new List<int>();
        for (int i = 0; i < count; i++)
        {
            rolls.Add(Roll(sides));
        }
        return (rolls.Sum(), rolls);
    }

    public int RollWithModifier(int count, int sides, int modifier)
    {
        return RollMultiple(count, sides) + modifier;
    }

    // Parse dice notation like "2d6+3" or "1d20"
    public int RollNotation(string notation)
    {
        try
        {
            var parts = notation.ToLower().Replace(" ", "").Split('d');
            if (parts.Length != 2) return 0;

            int count = int.Parse(parts[0]);
            int modifier = 0;
            int sides;

            if (parts[1].Contains('+'))
            {
                var sideParts = parts[1].Split('+');
                sides = int.Parse(sideParts[0]);
                modifier = int.Parse(sideParts[1]);
            }
            else if (parts[1].Contains('-'))
            {
                var sideParts = parts[1].Split('-');
                sides = int.Parse(sideParts[0]);
                modifier = -int.Parse(sideParts[1]);
            }
            else
            {
                sides = int.Parse(parts[1]);
            }

            return RollWithModifier(count, sides, modifier);
        }
        catch
        {
            return 0;
        }
    }
}
