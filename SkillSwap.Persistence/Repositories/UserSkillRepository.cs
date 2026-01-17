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

        public async Task<UserSkill?> GetByProfileAndSkillAsync(int profileId, int skillId, CancellationToken ct = default)
        {
            return await _context.UserSkills
                .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.SkillId == skillId, ct);
        }

        public async Task<List<UserSkill>> GetByProfileIdAsync(int profileId, CancellationToken ct = default)
        {
            return await _context.UserSkills
                .Where(us => us.ProfileId == profileId && !us.IsDeleted)
                .ToListAsync(ct);
        }

        public async Task<List<UserSkill>> GetByProfileIdsAsync(IReadOnlyCollection<int> profileIds, CancellationToken ct = default)
        {
            if (profileIds == null || profileIds.Count == 0)
                return new List<UserSkill>();

            return await _context.UserSkills
                .Where(us => profileIds.Contains(us.ProfileId) && !us.IsDeleted)
                .ToListAsync(ct);
        }
    }
}
