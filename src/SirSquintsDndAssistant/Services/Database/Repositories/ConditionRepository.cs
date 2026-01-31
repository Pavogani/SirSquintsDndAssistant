using SirSquintsDndAssistant.Models.Content;
using DndCondition = SirSquintsDndAssistant.Models.Content.Condition;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IConditionRepository : IRepository<DndCondition>
{
    Task<List<DndCondition>> SearchAsync(string query);
    Task BulkInsertAsync(List<DndCondition> conditions);
}

public class ConditionRepository : IConditionRepository
{
    private readonly IDatabaseService _database;

    public ConditionRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<DndCondition>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<DndCondition>().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<DndCondition?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<DndCondition>(id);
    }

    public async Task<int> SaveAsync(DndCondition item)
    {
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(DndCondition item)
    {
        return await _database.DeleteItemAsync(item);
    }

    public async Task<int> DeleteByIdAsync(int id)
    {
        var item = await GetByIdAsync(id);
        return item != null ? await DeleteAsync(item) : 0;
    }

    public async Task<List<DndCondition>> SearchAsync(string query)
    {
        var conn = _database.GetConnection();
        var searchTerm = query.ToLower();
        return await conn.Table<DndCondition>()
            .Where(c => c.Name.ToLower().Contains(searchTerm))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task BulkInsertAsync(List<DndCondition> conditions)
    {
        var conn = _database.GetConnection();
        await conn.InsertAllAsync(conditions);
    }
}
