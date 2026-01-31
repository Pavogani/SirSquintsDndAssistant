using SirSquintsDndAssistant.Models.Combat;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface ICombatRepository : IRepository<CombatEncounter>
{
    Task<CombatEncounter?> GetActiveCombatAsync();
    Task<List<CombatEncounter>> GetRecentCombatsAsync(int count = 10);
    Task<List<InitiativeEntry>> GetInitiativeEntriesAsync(int combatEncounterId);
    Task<int> SaveInitiativeEntryAsync(InitiativeEntry entry);
    Task<int> DeleteInitiativeEntryAsync(InitiativeEntry entry);
}

public class CombatRepository : ICombatRepository
{
    private readonly IDatabaseService _database;

    public CombatRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<CombatEncounter>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<CombatEncounter>()
            .OrderByDescending(c => c.StartTime)
            .ToListAsync();
    }

    public async Task<CombatEncounter?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<CombatEncounter>(id);
    }

    public async Task<int> SaveAsync(CombatEncounter item)
    {
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(CombatEncounter item)
    {
        return await _database.DeleteItemAsync(item);
    }

    public async Task<int> DeleteByIdAsync(int id)
    {
        var item = await GetByIdAsync(id);
        if (item != null)
            return await DeleteAsync(item);
        return 0;
    }

    public async Task<CombatEncounter?> GetActiveCombatAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<CombatEncounter>()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.StartTime)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CombatEncounter>> GetRecentCombatsAsync(int count = 10)
    {
        var conn = _database.GetConnection();
        return await conn.Table<CombatEncounter>()
            .OrderByDescending(c => c.StartTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<InitiativeEntry>> GetInitiativeEntriesAsync(int combatEncounterId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<InitiativeEntry>()
            .Where(e => e.CombatEncounterId == combatEncounterId)
            .OrderByDescending(e => e.Initiative)
            .ThenBy(e => e.SortOrder)
            .ToListAsync();
    }

    public async Task<int> SaveInitiativeEntryAsync(InitiativeEntry entry)
    {
        return await _database.SaveItemAsync(entry);
    }

    public async Task<int> DeleteInitiativeEntryAsync(InitiativeEntry entry)
    {
        return await _database.DeleteItemAsync(entry);
    }
}
