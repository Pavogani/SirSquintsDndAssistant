using SirSquintsDndAssistant.Models.Combat;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.Combat;

public interface ISpellSlotService
{
    Task<SpellSlotTracker?> GetTrackerForCombatantAsync(int initiativeEntryId);
    Task<SpellSlotTracker> CreateTrackerAsync(int initiativeEntryId, string combatantName, string className, int level);
    Task<SpellSlotTracker> CreateCustomTrackerAsync(int initiativeEntryId, string combatantName, int[] maxSlots);
    Task UpdateTrackerAsync(SpellSlotTracker tracker);
    Task DeleteTrackerAsync(int trackerId);

    Task UseSpellSlotAsync(int trackerId, int slotLevel);
    Task RestoreSpellSlotAsync(int trackerId, int slotLevel);
    Task UsePactSlotAsync(int trackerId);
    Task UseSorceryPointsAsync(int trackerId, int points);
    Task LongRestAsync(int trackerId);
    Task ShortRestAsync(int trackerId);
}

public class SpellSlotService : ISpellSlotService
{
    private readonly IDatabaseService _databaseService;

    public SpellSlotService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<SpellSlotTracker?> GetTrackerForCombatantAsync(int initiativeEntryId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<SpellSlotTracker>()
                .Where(t => t.InitiativeEntryId == initiativeEntryId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting spell slot tracker: {ex.Message}");
            return null;
        }
    }

    public async Task<SpellSlotTracker> CreateTrackerAsync(int initiativeEntryId, string combatantName, string className, int level)
    {
        var tracker = SpellSlotTracker.CreateForClass(className, level);
        tracker.InitiativeEntryId = initiativeEntryId;
        tracker.CombatantName = combatantName;

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(tracker);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating spell slot tracker: {ex.Message}");
        }

        return tracker;
    }

    public async Task<SpellSlotTracker> CreateCustomTrackerAsync(int initiativeEntryId, string combatantName, int[] maxSlots)
    {
        var tracker = new SpellSlotTracker
        {
            InitiativeEntryId = initiativeEntryId,
            CombatantName = combatantName
        };

        if (maxSlots.Length >= 1) tracker.Level1Max = tracker.Level1Current = maxSlots[0];
        if (maxSlots.Length >= 2) tracker.Level2Max = tracker.Level2Current = maxSlots[1];
        if (maxSlots.Length >= 3) tracker.Level3Max = tracker.Level3Current = maxSlots[2];
        if (maxSlots.Length >= 4) tracker.Level4Max = tracker.Level4Current = maxSlots[3];
        if (maxSlots.Length >= 5) tracker.Level5Max = tracker.Level5Current = maxSlots[4];
        if (maxSlots.Length >= 6) tracker.Level6Max = tracker.Level6Current = maxSlots[5];
        if (maxSlots.Length >= 7) tracker.Level7Max = tracker.Level7Current = maxSlots[6];
        if (maxSlots.Length >= 8) tracker.Level8Max = tracker.Level8Current = maxSlots[7];
        if (maxSlots.Length >= 9) tracker.Level9Max = tracker.Level9Current = maxSlots[8];

        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.InsertAsync(tracker);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating custom spell slot tracker: {ex.Message}");
        }

        return tracker;
    }

    public async Task UpdateTrackerAsync(SpellSlotTracker tracker)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.UpdateAsync(tracker);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating spell slot tracker: {ex.Message}");
        }
    }

    public async Task DeleteTrackerAsync(int trackerId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<SpellSlotTracker>(trackerId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting spell slot tracker: {ex.Message}");
        }
    }

    public async Task UseSpellSlotAsync(int trackerId, int slotLevel)
    {
        var tracker = await GetTrackerByIdAsync(trackerId);
        if (tracker == null) return;

        tracker.UseSlot(slotLevel);
        await UpdateTrackerAsync(tracker);
    }

    public async Task RestoreSpellSlotAsync(int trackerId, int slotLevel)
    {
        var tracker = await GetTrackerByIdAsync(trackerId);
        if (tracker == null) return;

        tracker.RestoreSlot(slotLevel);
        await UpdateTrackerAsync(tracker);
    }

    public async Task UsePactSlotAsync(int trackerId)
    {
        var tracker = await GetTrackerByIdAsync(trackerId);
        if (tracker == null) return;

        if (tracker.PactSlotCurrent > 0)
        {
            tracker.PactSlotCurrent--;
            await UpdateTrackerAsync(tracker);
        }
    }

    public async Task UseSorceryPointsAsync(int trackerId, int points)
    {
        var tracker = await GetTrackerByIdAsync(trackerId);
        if (tracker == null) return;

        tracker.SorceryPointsCurrent = Math.Max(0, tracker.SorceryPointsCurrent - points);
        await UpdateTrackerAsync(tracker);
    }

    public async Task LongRestAsync(int trackerId)
    {
        var tracker = await GetTrackerByIdAsync(trackerId);
        if (tracker == null) return;

        tracker.LongRest();
        await UpdateTrackerAsync(tracker);
    }

    public async Task ShortRestAsync(int trackerId)
    {
        var tracker = await GetTrackerByIdAsync(trackerId);
        if (tracker == null) return;

        tracker.ShortRest();
        await UpdateTrackerAsync(tracker);
    }

    private async Task<SpellSlotTracker?> GetTrackerByIdAsync(int trackerId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.GetAsync<SpellSlotTracker>(trackerId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting spell slot tracker by ID: {ex.Message}");
            return null;
        }
    }
}
