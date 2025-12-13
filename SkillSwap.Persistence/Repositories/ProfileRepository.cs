using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class ProfileRepository : BaseRepository<Profile>, IProfileRepository
    {
        private readonly PersistenceContext _context;

        public ProfileRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Profile?> GetByUserIdAsync(int userId)
        {
            return await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId &&!p.IsDeleted);
        }
        public async Task<Profile?> GetAnyByUserIdAsync(int userId)
        {
            return await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
        public async Task<bool> ExistsByUserNameAsync(string userName)
        {
            return await _context.Profiles.AnyAsync(p => p.UserName == userName && !p.IsDeleted);
        }
    }
    
}
