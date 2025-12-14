using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        private readonly PersistenceContext _context;

        public MatchRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Match?> GetBetweenProfilesAsync(int profile1Id, int profile2Id)
        {
            return await _context.Matches
                .FirstOrDefaultAsync(m =>
                    !m.IsDeleted &&
                    (
                        (m.Profile1Id == profile1Id && m.Profile2Id == profile2Id) ||
                        (m.Profile1Id == profile2Id && m.Profile2Id == profile1Id)
                    ));
        }
        public async Task<HashSet<int>> GetPartnerProfileIdsAsync(int myProfileId)
        {
            var partners = await _context.Matches
                .Where(m => !m.IsDeleted && (m.Profile1Id == myProfileId || m.Profile2Id == myProfileId))
                .Select(m => m.Profile1Id == myProfileId ? m.Profile2Id : m.Profile1Id)
                .ToListAsync();

            return partners.ToHashSet();
        }

    }
}
