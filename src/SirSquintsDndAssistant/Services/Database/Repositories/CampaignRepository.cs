using SirSquintsDndAssistant.Models.Campaign;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface ICampaignRepository : IRepository<Campaign>
{
    Task<List<Campaign>> GetActiveCampaignsAsync();
    Task<Campaign?> GetActiveCampaignAsync();
}

public class CampaignRepository : ICampaignRepository
{
    private readonly IDatabaseService _database;

    public CampaignRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Campaign>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Campaign>()
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<Campaign?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<Campaign>(id);
    }

    public async Task<int> SaveAsync(Campaign item)
    {
        item.Modified = DateTime.Now;
        if (item.Id == 0)
            item.Created = DateTime.Now;

        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(Campaign item)
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

    public async Task<List<Campaign>> GetActiveCampaignsAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Campaign>()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<Campaign?> GetActiveCampaignAsync()
    {
        var campaigns = await GetActiveCampaignsAsync();
        return campaigns.FirstOrDefault();
    }
}
