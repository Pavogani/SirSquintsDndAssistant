using SirSquintsDndAssistant.Models.Content;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IMagicItemRepository : IRepository<MagicItem>
{
    Task<List<MagicItem>> SearchAsync(string query);
    Task<List<MagicItem>> GetByRarityAsync(string rarity);
    Task<int> BulkInsertAsync(List<MagicItem> items);

    // Pagination methods
    Task<PagedResult<MagicItem>> GetPagedAsync(int page, int pageSize);
    Task<PagedResult<MagicItem>> SearchPagedAsync(string query, int page, int pageSize);
    Task<PagedResult<MagicItem>> GetByRarityPagedAsync(string rarity, int page, int pageSize);
    Task<int> GetTotalCountAsync();
}

public class MagicItemRepository : IMagicItemRepository
{
    private readonly IDatabaseService _database;

    public MagicItemRepository(IDatabaseService database) { _database = database; }

    public async Task<List<MagicItem>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<MagicItem>().OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<MagicItem?> GetByIdAsync(int id) => await _database.GetItemAsync<MagicItem>(id);
    public async Task<int> SaveAsync(MagicItem item) => await _database.SaveItemAsync(item);
    public async Task<int> DeleteAsync(MagicItem item) => await _database.DeleteItemAsync(item);
    public async Task<int> DeleteByIdAsync(int id)
    {
        var item = await GetByIdAsync(id);
        return item != null ? await DeleteAsync(item) : 0;
    }

    public async Task<List<MagicItem>> SearchAsync(string query)
    {
        var conn = _database.GetConnection();
        var lowerQuery = query.ToLowerInvariant();
        // Use raw SQL for OR conditions since sqlite-net LINQ doesn't support them well
        return await conn.QueryAsync<MagicItem>(
            "SELECT * FROM MagicItem WHERE LOWER(Name) LIKE ? OR LOWER(Rarity) LIKE ? OR LOWER(Type) LIKE ? OR LOWER(Description) LIKE ? ORDER BY Name",
            $"%{lowerQuery}%", $"%{lowerQuery}%", $"%{lowerQuery}%", $"%{lowerQuery}%");
    }

    public async Task<List<MagicItem>> GetByRarityAsync(string rarity)
    {
        var conn = _database.GetConnection();
        return await conn.Table<MagicItem>().Where(m => m.Rarity == rarity).OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<int> BulkInsertAsync(List<MagicItem> items)
    {
        var conn = _database.GetConnection();
        return await conn.InsertAllAsync(items);
    }

    // Pagination implementations
    public async Task<int> GetTotalCountAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<MagicItem>().CountAsync();
    }

    public async Task<PagedResult<MagicItem>> GetPagedAsync(int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var totalCount = await conn.Table<MagicItem>().CountAsync();

        var items = await conn.Table<MagicItem>()
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MagicItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<MagicItem>> SearchPagedAsync(string query, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var lowerQuery = query.ToLowerInvariant();
        var offset = (page - 1) * pageSize;

        // Use raw SQL for OR conditions since sqlite-net LINQ doesn't support them well
        var countResult = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MagicItem WHERE LOWER(Name) LIKE ? OR LOWER(Rarity) LIKE ? OR LOWER(Type) LIKE ? OR LOWER(Description) LIKE ?",
            $"%{lowerQuery}%", $"%{lowerQuery}%", $"%{lowerQuery}%", $"%{lowerQuery}%");

        var items = await conn.QueryAsync<MagicItem>(
            "SELECT * FROM MagicItem WHERE LOWER(Name) LIKE ? OR LOWER(Rarity) LIKE ? OR LOWER(Type) LIKE ? OR LOWER(Description) LIKE ? ORDER BY Name LIMIT ? OFFSET ?",
            $"%{lowerQuery}%", $"%{lowerQuery}%", $"%{lowerQuery}%", $"%{lowerQuery}%", pageSize, offset);

        return new PagedResult<MagicItem>
        {
            Items = items,
            TotalCount = countResult,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<MagicItem>> GetByRarityPagedAsync(string rarity, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var baseQuery = conn.Table<MagicItem>()
            .Where(m => m.Rarity == rarity);

        var totalCount = await baseQuery.CountAsync();

        var items = await conn.Table<MagicItem>()
            .Where(m => m.Rarity == rarity)
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MagicItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
