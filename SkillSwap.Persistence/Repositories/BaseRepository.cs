using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillSwap.Persistence.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly PersistenceContext _dbContext;

        public BaseRepository(PersistenceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<List<T>> GetAsync()
        {
            var query = _dbContext.Set<T>().AsQueryable();

            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
            }

            return await query.ToListAsync();
        }

        public virtual async Task<T?> GetAsync(int id)
        {
            var entity = await _dbContext.Set<T>().FindAsync(id);

            if (entity is ISoftDeletable soft && soft.IsDeleted)
                return null;

            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            _dbContext.Set<T>().Update(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public virtual async Task DeleteAsync(T entity)
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

            await _dbContext.SaveChangesAsync();
        }
    }
}
