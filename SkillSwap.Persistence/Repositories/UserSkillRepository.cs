using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class UserSkillRepository : BaseRepository<UserSkill>, IUserSkillRepository
    {
        private readonly PersistenceContext _context;

        public UserSkillRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<UserSkill?> GetByProfileAndSkillAsync(int profileId, int skillId)
        {
            return await _context.UserSkills
                .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.SkillId == skillId);
        }


        public async Task<List<UserSkill>> GetByProfileIdAsync(int profileId)
        {
            return await _context.UserSkills
                .Where(us => us.ProfileId == profileId && !us.IsDeleted)
                .ToListAsync();
        }
    }
}
