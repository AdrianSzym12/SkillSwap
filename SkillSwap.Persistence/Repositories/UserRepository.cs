using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly PersistenceContext _context;

        public UserRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);
        }

        public async Task<List<User>> GetByIdsAsync(IReadOnlyCollection<int> userIds, CancellationToken ct = default)
        {
            if (userIds == null || userIds.Count == 0)
                return new List<User>();

            return await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync(ct);
        }
    }
}
