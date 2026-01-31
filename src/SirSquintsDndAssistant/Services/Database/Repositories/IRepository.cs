namespace SirSquintsDndAssistant.Services.Database.Repositories;

public interface IRepository<T> where T : new()
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<int> SaveAsync(T item);
    Task<int> DeleteAsync(T item);
    Task<int> DeleteByIdAsync(int id);
}
