using SQLite;

namespace SirSquintsDndAssistant.Models.Combat;

/// <summary>
/// Tracks spell slots for a combatant during combat.
/// </summary>
public class SpellSlotTracker
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int InitiativeEntryId { get; set; }
    public string CombatantName { get; set; } = string.Empty;

    // Spell slot maximums (based on class/level)
    public int Level1Max { get; set; }
    public int Level2Max { get; set; }
    public int Level3Max { get; set; }
    public int Level4Max { get; set; }
    public int Level5Max { get; set; }
    public int Level6Max { get; set; }
    public int Level7Max { get; set; }
    public int Level8Max { get; set; }
    public int Level9Max { get; set; }

    // Current spell slots remaining
    public int Level1Current { get; set; }
    public int Level2Current { get; set; }
    public int Level3Current { get; set; }
    public int Level4Current { get; set; }
    public int Level5Current { get; set; }
    public int Level6Current { get; set; }
    public int Level7Current { get; set; }
    public int Level8Current { get; set; }
    public int Level9Current { get; set; }

    // Warlock pact slots (separate from regular slots)
    public int PactSlotMax { get; set; }
    public int PactSlotCurrent { get; set; }
    public int PactSlotLevel { get; set; } // What level the pact slot is

    // Sorcery points for Sorcerers
    public int SorceryPointsMax { get; set; }
    public int SorceryPointsCurrent { get; set; }

    // Helper methods
    public int GetCurrentSlots(int level) => level switch
    {
        1 => Level1Current,
        2 => Level2Current,
        3 => Level3Current,
        4 => Level4Current,
        5 => Level5Current,
        6 => Level6Current,
        7 => Level7Current,
        8 => Level8Current,
        9 => Level9Current,
        _ => 0
    };

    public int GetMaxSlots(int level) => level switch
    {
        1 => Level1Max,
        2 => Level2Max,
        3 => Level3Max,
        4 => Level4Max,
        5 => Level5Max,
        6 => Level6Max,
        7 => Level7Max,
        8 => Level8Max,
        9 => Level9Max,
        _ => 0
    };

    public void UseSlot(int level)
    {
        switch (level)
        {
            case 1: if (Level1Current > 0) Level1Current--; break;
            case 2: if (Level2Current > 0) Level2Current--; break;
            case 3: if (Level3Current > 0) Level3Current--; break;
            case 4: if (Level4Current > 0) Level4Current--; break;
            case 5: if (Level5Current > 0) Level5Current--; break;
            case 6: if (Level6Current > 0) Level6Current--; break;
            case 7: if (Level7Current > 0) Level7Current--; break;
            case 8: if (Level8Current > 0) Level8Current--; break;
            case 9: if (Level9Current > 0) Level9Current--; break;
        }
    }

    public void RestoreSlot(int level)
    {
        switch (level)
        {
            case 1: if (Level1Current < Level1Max) Level1Current++; break;
            case 2: if (Level2Current < Level2Max) Level2Current++; break;
            case 3: if (Level3Current < Level3Max) Level3Current++; break;
            case 4: if (Level4Current < Level4Max) Level4Current++; break;
            case 5: if (Level5Current < Level5Max) Level5Current++; break;
            case 6: if (Level6Current < Level6Max) Level6Current++; break;
            case 7: if (Level7Current < Level7Max) Level7Current++; break;
            case 8: if (Level8Current < Level8Max) Level8Current++; break;
            case 9: if (Level9Current < Level9Max) Level9Current++; break;
        }
    }

    public void LongRest()
    {
        Level1Current = Level1Max;
        Level2Current = Level2Max;
        Level3Current = Level3Max;
        Level4Current = Level4Max;
        Level5Current = Level5Max;
        Level6Current = Level6Max;
        Level7Current = Level7Max;
        Level8Current = Level8Max;
        Level9Current = Level9Max;
        PactSlotCurrent = PactSlotMax;
        SorceryPointsCurrent = SorceryPointsMax;
    }

    public void ShortRest()
    {
        // Only Warlock pact slots recover on short rest
        PactSlotCurrent = PactSlotMax;
    }

    /// <summary>
    /// Initializes spell slots based on class and level using standard 5e progression.
    /// </summary>
    public static SpellSlotTracker CreateForClass(string className, int level)
    {
        var tracker = new SpellSlotTracker();
        var slots = GetSpellSlotsByClassLevel(className, level);

        tracker.Level1Max = tracker.Level1Current = slots[0];
        tracker.Level2Max = tracker.Level2Current = slots[1];
        tracker.Level3Max = tracker.Level3Current = slots[2];
        tracker.Level4Max = tracker.Level4Current = slots[3];
        tracker.Level5Max = tracker.Level5Current = slots[4];
        tracker.Level6Max = tracker.Level6Current = slots[5];
        tracker.Level7Max = tracker.Level7Current = slots[6];
        tracker.Level8Max = tracker.Level8Current = slots[7];
        tracker.Level9Max = tracker.Level9Current = slots[8];

        // Handle Warlock pact magic separately
        if (className.Equals("Warlock", StringComparison.OrdinalIgnoreCase))
        {
            var (pactSlots, pactLevel) = GetWarlockPactSlots(level);
            tracker.PactSlotMax = tracker.PactSlotCurrent = pactSlots;
            tracker.PactSlotLevel = pactLevel;
            // Warlocks don't use regular spell slots
            tracker.Level1Max = tracker.Level2Max = tracker.Level3Max = tracker.Level4Max = 0;
            tracker.Level5Max = tracker.Level6Max = tracker.Level7Max = tracker.Level8Max = tracker.Level9Max = 0;
            tracker.Level1Current = tracker.Level2Current = tracker.Level3Current = tracker.Level4Current = 0;
            tracker.Level5Current = tracker.Level6Current = tracker.Level7Current = tracker.Level8Current = tracker.Level9Current = 0;
        }

        // Handle Sorcerer sorcery points
        if (className.Equals("Sorcerer", StringComparison.OrdinalIgnoreCase) && level >= 2)
        {
            tracker.SorceryPointsMax = tracker.SorceryPointsCurrent = level;
        }

        return tracker;
    }

    private static int[] GetSpellSlotsByClassLevel(string className, int level)
    {
        // Full casters: Bard, Cleric, Druid, Sorcerer, Wizard
        // Half casters: Paladin, Ranger (start at level 2)
        // Third casters: Eldritch Knight Fighter, Arcane Trickster Rogue

        var isFullCaster = className.Equals("Bard", StringComparison.OrdinalIgnoreCase) ||
                          className.Equals("Cleric", StringComparison.OrdinalIgnoreCase) ||
                          className.Equals("Druid", StringComparison.OrdinalIgnoreCase) ||
                          className.Equals("Sorcerer", StringComparison.OrdinalIgnoreCase) ||
                          className.Equals("Wizard", StringComparison.OrdinalIgnoreCase);

        var isHalfCaster = className.Equals("Paladin", StringComparison.OrdinalIgnoreCase) ||
                          className.Equals("Ranger", StringComparison.OrdinalIgnoreCase);

        if (isFullCaster)
        {
            return GetFullCasterSlots(level);
        }
        else if (isHalfCaster)
        {
            return GetHalfCasterSlots(level);
        }

        // Default: no spell slots
        return new int[9];
    }

    private static int[] GetFullCasterSlots(int level) => level switch
    {
        1 => new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 },
        2 => new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 },
        3 => new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 },
        4 => new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 },
        5 => new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 },
        6 => new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 },
        7 => new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 },
        8 => new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 },
        9 => new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 },
        10 => new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 },
        11 => new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 },
        12 => new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 },
        13 => new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 },
        14 => new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 },
        15 => new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 },
        16 => new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 },
        17 => new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 },
        18 => new[] { 4, 3, 3, 3, 3, 1, 1, 1, 1 },
        19 => new[] { 4, 3, 3, 3, 3, 2, 1, 1, 1 },
        20 => new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 },
        _ => new int[9]
    };

    private static int[] GetHalfCasterSlots(int level) => level switch
    {
        2 => new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 },
        3 or 4 => new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 },
        5 or 6 => new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 },
        7 or 8 => new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 },
        9 or 10 => new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 },
        11 or 12 => new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 },
        13 or 14 => new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 },
        15 or 16 => new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 },
        17 or 18 => new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 },
        19 or 20 => new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 },
        _ => new int[9]
    };

    private static (int slots, int level) GetWarlockPactSlots(int level) => level switch
    {
        1 => (1, 1),
        2 => (2, 1),
        3 or 4 => (2, 2),
        5 or 6 => (2, 3),
        7 or 8 => (2, 4),
        9 or 10 => (2, 5),
        11 or 12 or 13 or 14 or 15 or 16 => (3, 5),
        17 or 18 or 19 or 20 => (4, 5),
        _ => (0, 0)
    };
}
