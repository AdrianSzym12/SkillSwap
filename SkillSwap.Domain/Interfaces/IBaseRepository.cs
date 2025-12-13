public interface IBaseRepository<T> where T : class
{
    Task<T> AddAsync(T entity);
    Task<List<T>> GetAsync();
    Task<T?> GetAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}