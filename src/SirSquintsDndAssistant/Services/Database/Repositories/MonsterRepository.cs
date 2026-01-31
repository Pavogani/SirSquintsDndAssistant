using SirSquintsDndAssistant.Models.Creatures;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IMonsterRepository : IRepository<Monster>
{
    Task<List<Monster>> SearchAsync(string query);
    Task<List<Monster>> GetByChallengeRatingAsync(double minCR, double maxCR);
    Task<List<Monster>> GetByTypeAsync(string type);
    Task<List<Monster>> GetFavoritesAsync();
    Task<int> BulkInsertAsync(List<Monster> monsters);

    // Pagination methods
    Task<PagedResult<Monster>> GetPagedAsync(int page, int pageSize);
    Task<PagedResult<Monster>> SearchPagedAsync(string query, int page, int pageSize);
    Task<PagedResult<Monster>> GetByChallengeRatingPagedAsync(double minCR, double maxCR, int page, int pageSize);
    Task<PagedResult<Monster>> GetByTypePagedAsync(string type, int page, int pageSize);
    Task<int> GetTotalCountAsync();
}

public class MonsterRepository : IMonsterRepository
{
    private readonly IDatabaseService _database;

    public MonsterRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Monster>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Monster>()
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<Monster?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<Monster>(id);
    }

    public async Task<int> SaveAsync(Monster item)
    {
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(Monster item)
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

    public async Task<List<Monster>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 100)
            return new List<Monster>();

        var conn = _database.GetConnection();
        var lowerQuery = query.ToLowerInvariant();
        return await conn.Table<Monster>()
            .Where(m => m.Name.ToLower().Contains(lowerQuery) || m.Type.ToLower().Contains(lowerQuery))
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<List<Monster>> GetByChallengeRatingAsync(double minCR, double maxCR)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Monster>()
            .Where(m => m.ChallengeRating >= minCR && m.ChallengeRating <= maxCR)
            .OrderBy(m => m.ChallengeRating)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<List<Monster>> GetByTypeAsync(string type)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Monster>()
            .Where(m => m.Type == type)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<List<Monster>> GetFavoritesAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Monster>()
            .Where(m => m.IsFavorite)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<int> BulkInsertAsync(List<Monster> monsters)
    {
        var conn = _database.GetConnection();
        return await conn.InsertAllAsync(monsters);
    }

    // Pagination implementations
    public async Task<int> GetTotalCountAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Monster>().CountAsync();
    }

    public async Task<PagedResult<Monster>> GetPagedAsync(int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var totalCount = await conn.Table<Monster>().CountAsync();

        var items = await conn.Table<Monster>()
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Monster>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Monster>> SearchPagedAsync(string query, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var lowerQuery = query.ToLowerInvariant();

        // Use case-insensitive search by comparing lowercase versions
        var allMatching = await conn.Table<Monster>().ToListAsync();
        var filtered = allMatching
            .Where(m => m.Name.ToLowerInvariant().Contains(lowerQuery) ||
                       (m.Type?.ToLowerInvariant().Contains(lowerQuery) ?? false))
            .OrderBy(m => m.Name)
            .ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Monster>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Monster>> GetByChallengeRatingPagedAsync(double minCR, double maxCR, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var baseQuery = conn.Table<Monster>()
            .Where(m => m.ChallengeRating >= minCR && m.ChallengeRating <= maxCR);

        var totalCount = await baseQuery.CountAsync();

        var items = await conn.Table<Monster>()
            .Where(m => m.ChallengeRating >= minCR && m.ChallengeRating <= maxCR)
            .OrderBy(m => m.ChallengeRating)
            .ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Monster>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Monster>> GetByTypePagedAsync(string type, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var baseQuery = conn.Table<Monster>()
            .Where(m => m.Type == type);

        var totalCount = await baseQuery.CountAsync();

        var items = await conn.Table<Monster>()
            .Where(m => m.Type == type)
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Monster>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
