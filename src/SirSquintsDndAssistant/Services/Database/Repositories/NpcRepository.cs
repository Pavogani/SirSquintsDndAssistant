using SirSquintsDndAssistant.Models.Creatures;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface INpcRepository : IRepository<NPC>
{
    Task<List<NPC>> GetByCampaignAsync(int campaignId);
    Task<List<NPC>> SearchAsync(string query);
}

public class NpcRepository : INpcRepository
{
    private readonly IDatabaseService _database;

    public NpcRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<NPC>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<NPC>()
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<NPC?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<NPC>(id);
    }

    public async Task<int> SaveAsync(NPC item)
    {
        item.Modified = DateTime.Now;
        if (item.Id == 0)
            item.Created = DateTime.Now;

        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(NPC item)
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

    public async Task<List<NPC>> GetByCampaignAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<NPC>()
            .Where(n => n.CampaignId == campaignId)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<List<NPC>> SearchAsync(string query)
    {
        var conn = _database.GetConnection();
        return await conn.Table<NPC>()
            .Where(n => n.Name.Contains(query) || n.Description.Contains(query))
            .OrderBy(n => n.Name)
            .ToListAsync();
    }
}
