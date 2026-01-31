using SirSquintsDndAssistant.Models.Campaign;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface ISessionRepository : IRepository<Session>
{
    Task<List<Session>> GetByCampaignAsync(int campaignId);
    Task<Session?> GetLatestSessionAsync(int campaignId);
}

public class SessionRepository : ISessionRepository
{
    private readonly IDatabaseService _database;

    public SessionRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Session>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Session>()
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync();
    }

    public async Task<Session?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<Session>(id);
    }

    public async Task<int> SaveAsync(Session item)
    {
        item.Modified = DateTime.Now;
        if (item.Id == 0)
            item.Created = DateTime.Now;

        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(Session item)
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

    public async Task<List<Session>> GetByCampaignAsync(int campaignId)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Session>()
            .Where(s => s.CampaignId == campaignId)
            .OrderBy(s => s.SessionNumber)
            .ToListAsync();
    }

    public async Task<Session?> GetLatestSessionAsync(int campaignId)
    {
        var sessions = await GetByCampaignAsync(campaignId);
        return sessions.LastOrDefault();
    }
}
