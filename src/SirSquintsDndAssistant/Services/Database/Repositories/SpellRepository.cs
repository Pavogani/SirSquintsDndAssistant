using SirSquintsDndAssistant.Models.Content;

namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface ISpellRepository : IRepository<Spell>
{
    Task<List<Spell>> SearchAsync(string query);
    Task<List<Spell>> GetByLevelAsync(int level);
    Task<List<Spell>> GetBySchoolAsync(string school);
    Task<List<Spell>> GetFavoritesAsync();
    Task<int> BulkInsertAsync(List<Spell> spells);

    // Pagination methods
    Task<PagedResult<Spell>> GetPagedAsync(int page, int pageSize);
    Task<PagedResult<Spell>> SearchPagedAsync(string query, int page, int pageSize);
    Task<PagedResult<Spell>> GetByLevelPagedAsync(int level, int page, int pageSize);
    Task<int> GetTotalCountAsync();
}

public class SpellRepository : ISpellRepository
{
    private readonly IDatabaseService _database;

    public SpellRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Spell>> GetAllAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Spell>()
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Spell?> GetByIdAsync(int id)
    {
        return await _database.GetItemAsync<Spell>(id);
    }

    public async Task<int> SaveAsync(Spell item)
    {
        return await _database.SaveItemAsync(item);
    }

    public async Task<int> DeleteAsync(Spell item)
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

    public async Task<List<Spell>> SearchAsync(string query)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Spell>()
            .Where(s => s.Name.Contains(query) || s.Description.Contains(query))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Spell>> GetByLevelAsync(int level)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Spell>()
            .Where(s => s.Level == level)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Spell>> GetBySchoolAsync(string school)
    {
        var conn = _database.GetConnection();
        return await conn.Table<Spell>()
            .Where(s => s.School == school)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Spell>> GetFavoritesAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Spell>()
            .Where(s => s.IsFavorite)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<int> BulkInsertAsync(List<Spell> spells)
    {
        var conn = _database.GetConnection();
        return await conn.InsertAllAsync(spells);
    }

    // Pagination implementations
    public async Task<int> GetTotalCountAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<Spell>().CountAsync();
    }

    public async Task<PagedResult<Spell>> GetPagedAsync(int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var totalCount = await conn.Table<Spell>().CountAsync();

        var items = await conn.Table<Spell>()
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Spell>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Spell>> SearchPagedAsync(string query, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var baseQuery = conn.Table<Spell>()
            .Where(s => s.Name.Contains(query) || s.Description.Contains(query));

        var totalCount = await baseQuery.CountAsync();

        var items = await conn.Table<Spell>()
            .Where(s => s.Name.Contains(query) || s.Description.Contains(query))
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Spell>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Spell>> GetByLevelPagedAsync(int level, int page, int pageSize)
    {
        var conn = _database.GetConnection();
        var baseQuery = conn.Table<Spell>()
            .Where(s => s.Level == level);

        var totalCount = await baseQuery.CountAsync();

        var items = await conn.Table<Spell>()
            .Where(s => s.Level == level)
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Spell>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
