using SirSquintsDndAssistant.Models.Encounter;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IEncounterRepository : IRepository<EncounterTemplate>
{
    Task<List<EncounterTemplate>> GetByCampaignAsync(int campaignId);
    Task<List<EncounterTemplate>> GetByDifficultyAsync(string difficulty);
}

public class EncounterRepository : IEncounterRepository
{
    private readonly IDatabaseService _database;

    public EncounterRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<EncounterTemplate>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<EncounterTemplate>()
            .OrderByDescending(e => e.Created)
            .ToListAsync();
    }

    public async Task<EncounterTemplate?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<EncounterTemplate>(id);
    }

    public async Task<int> SaveAsync(EncounterTemplate item)
    {
        if (item.Id == 0)
            item.Created = DateTime.Now;

        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(EncounterTemplate item)
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

    public async Task<List<EncounterTemplate>> GetByCampaignAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<EncounterTemplate>()
            .Where(e => e.CampaignId == campaignId)
            .OrderByDescending(e => e.Created)
            .ToListAsync();
    }

    public async Task<List<EncounterTemplate>> GetByDifficultyAsync(string difficulty)
    {
        var conn = _database.GetConnection();
        return await conn.Table<EncounterTemplate>()
            .Where(e => e.Difficulty == difficulty)
            .OrderByDescending(e => e.Created)
            .ToListAsync();
    }
}
