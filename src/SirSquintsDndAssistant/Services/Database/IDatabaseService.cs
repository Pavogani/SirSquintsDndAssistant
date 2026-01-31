using SQLite;

namespace SirSquintsDndAssistant.Services.Database;

public interface IDatabaseService
{
    Task InitializeAsync();
    SQLiteAsyncConnection GetConnection();
    Task<SQLiteAsyncConnection> GetConnectionAsync();
    Task<int> SaveItemAsync<T>(T item) where T : new();
    Task<int> DeleteItemAsync<T>(T item) where T : new();
    Task<int> DeleteAllAsync<T>() where T : new();
    Task<T?> GetItemAsync<T>(int id) where T : new();
    Task<List<T>> GetItemsAsync<T>() where T : new();
}
