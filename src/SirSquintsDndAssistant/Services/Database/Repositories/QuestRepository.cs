using SirSquintsDndAssistant.Models.Campaign;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IQuestRepository : IRepository<Quest>
{
    Task<List<Quest>> GetByCampaignAsync(int campaignId);
    Task<List<Quest>> GetActiveQuestsAsync(int campaignId);
    Task<List<Quest>> GetCompletedQuestsAsync(int campaignId);
}

public class QuestRepository : IQuestRepository
{
    private readonly IDatabaseService _database;

    public QuestRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Quest>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Quest>().ToListAsync();
    }

    public async Task<Quest?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<Quest>(id);
    }

    public async Task<int> SaveAsync(Quest item)
    {
        if (item.Id == 0)
            item.Created = DateTime.Now;
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(Quest item)
    {
        return await _database.DeleteItemAsync(item);
    }

    public async Task<int> DeleteByIdAsync(int id)
    {
        var item = await GetByIdAsync(id);
        return item != null ? await DeleteAsync(item) : 0;
    }

    public async Task<List<Quest>> GetByCampaignAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Quest>()
            .Where(q => q.CampaignId == campaignId)
            .OrderBy(q => q.Status)
            .ThenByDescending(q => q.Created)
            .ToListAsync();
    }

    public async Task<List<Quest>> GetActiveQuestsAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Quest>()
            .Where(q => q.CampaignId == campaignId && q.Status == "Active")
            .ToListAsync();
    }

    public async Task<List<Quest>> GetCompletedQuestsAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Quest>()
            .Where(q => q.CampaignId == campaignId && q.Status == "Completed")
            .ToListAsync();
    }
}
