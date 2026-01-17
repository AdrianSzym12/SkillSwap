public interface IBaseRepository<T> where T : class
{
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task<List<T>> GetAsync(CancellationToken ct = default);
    Task<T?> GetAsync(int id, CancellationToken ct = default);
    Task<T> UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
