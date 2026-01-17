using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Interfaces;
using System.Linq;

namespace SkillSwap.Persistence.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly PersistenceContext _dbContext;

        public BaseRepository(PersistenceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _dbContext.Set<T>().AddAsync(entity, ct);
            await _dbContext.SaveChangesAsync(ct);
            return entity;
        }

        public virtual async Task<List<T>> GetAsync(CancellationToken ct = default)
        {
            var query = _dbContext.Set<T>().AsQueryable();

            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
            }

            return await query.ToListAsync(ct);
        }

        public virtual async Task<T?> GetAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<T>().FindAsync(new object[] { id }, ct);

            if (entity is ISoftDeletable soft && soft.IsDeleted)
                return null;

            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity, CancellationToken ct = default)
        {
            _dbContext.Set<T>().Update(entity);
            await _dbContext.SaveChangesAsync(ct);
            return entity;
        }

        public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            if (entity is ISoftDeletable soft)
            {
                soft.IsDeleted = true;

                var deletedAtProp = entity.GetType().GetProperty("DeletedAt");
                if (deletedAtProp != null && deletedAtProp.PropertyType == typeof(DateTime?))
                {
                    deletedAtProp.SetValue(entity, DateTime.UtcNow);
                }

                _dbContext.Set<T>().Update(entity);
            }
            else
            {
                _dbContext.Set<T>().Remove(entity);
            }

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
