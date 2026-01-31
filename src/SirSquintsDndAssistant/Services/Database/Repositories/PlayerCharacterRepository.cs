using SirSquintsDndAssistant.Models.Creatures;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IPlayerCharacterRepository : IRepository<PlayerCharacter>
{
    Task<List<PlayerCharacter>> GetByCampaignAsync(int campaignId);
}

public class PlayerCharacterRepository : IPlayerCharacterRepository
{
    private readonly IDatabaseService _database;

    public PlayerCharacterRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<PlayerCharacter>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<PlayerCharacter>().OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<PlayerCharacter?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<PlayerCharacter>(id);
    }

    public async Task<int> SaveAsync(PlayerCharacter item)
    {
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(PlayerCharacter item)
    {
        return await _database.DeleteItemAsync(item);
    }

    public async Task<int> DeleteByIdAsync(int id)
    {
        var item = await GetByIdAsync(id);
        return item != null ? await DeleteAsync(item) : 0;
    }

    public async Task<List<PlayerCharacter>> GetByCampaignAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<PlayerCharacter>()
            .Where(p => p.CampaignId == campaignId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
