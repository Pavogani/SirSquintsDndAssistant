using SirSquintsDndAssistant.Models.Content;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IEquipmentRepository : IRepository<Equipment>
{
    Task<List<Equipment>> SearchAsync(string query);
    Task<List<Equipment>> GetByCategoryAsync(string category);
    Task<int> BulkInsertAsync(List<Equipment> equipment);

    // Pagination methods
    Task<PagedResult<Equipment>> GetPagedAsync(int page, int pageSize);
    Task<PagedResult<Equipment>> SearchPagedAsync(string query, int page, int pageSize);
    Task<PagedResult<Equipment>> GetByCategoryPagedAsync(string category, int page, int pageSize);
    Task<int> GetTotalCountAsync();
}

public class EquipmentRepository : IEquipmentRepository
{
    private readonly IDatabaseService _database;

    public EquipmentRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Equipment>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Equipment>().OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Equipment?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<Equipment>(id);
    }

    public async Task<int> SaveAsync(Equipment item)
    {
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(Equipment item)
    {
        return await _database.DeleteItemAsync(item);
    }

    public async Task<int> DeleteByIdAsync(int id)
    {
        var item = await GetByIdAsync(id);
        return item != null ? await DeleteAsync(item) : 0;
    }

    public async Task<List<Equipment>> SearchAsync(string query)
    {
        var conn = _database.GetConnection();
        var lowerQuery = query.ToLowerInvariant();
        // Use raw SQL for OR conditions since sqlite-net LINQ doesn't support them well
        return await conn.QueryAsync<Equipment>(
            "SELECT * FROM Equipment WHERE LOWER(Name) LIKE ? OR LOWER(EquipmentCategory) LIKE ? ORDER BY Name",
            $"%{lowerQuery}%", $"%{lowerQuery}%");
    }

    public async Task<List<Equipment>> GetByCategoryAsync(string category)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Equipment>()
            .Where(e => e.EquipmentCategory == category)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<int> BulkInsertAsync(List<Equipment> equipment)
    {
        var conn = _database.GetConnection();
        return await conn.InsertAllAsync(equipment);
    }

    // Pagination implementations
    public async Task<int> GetTotalCountAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Equipment>().CountAsync();
    }

    public async Task<PagedResult<Equipment>> GetPagedAsync(int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var totalCount = await conn.Table<Equipment>().CountAsync();

        var items = await conn.Table<Equipment>()
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Equipment>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Equipment>> SearchPagedAsync(string query, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var lowerQuery = query.ToLowerInvariant();
        var offset = (page - 1) * pageSize;

        // Use raw SQL for OR conditions since sqlite-net LINQ doesn't support them well
        var countResult = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Equipment WHERE LOWER(Name) LIKE ? OR LOWER(EquipmentCategory) LIKE ?",
            $"%{lowerQuery}%", $"%{lowerQuery}%");

        var items = await conn.QueryAsync<Equipment>(
            "SELECT * FROM Equipment WHERE LOWER(Name) LIKE ? OR LOWER(EquipmentCategory) LIKE ? ORDER BY Name LIMIT ? OFFSET ?",
            $"%{lowerQuery}%", $"%{lowerQuery}%", pageSize, offset);

        return new PagedResult<Equipment>
        {
            Items = items,
            TotalCount = countResult,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Equipment>> GetByCategoryPagedAsync(string category, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var baseQuery = conn.Table<Equipment>()
            .Where(e => e.EquipmentCategory == category);

        var totalCount = await baseQuery.CountAsync();

        var items = await conn.Table<Equipment>()
            .Where(e => e.EquipmentCategory == category)
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Equipment>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
