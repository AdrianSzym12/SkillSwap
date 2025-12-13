using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class MatchSwipeRepository : BaseRepository<MatchSwipe>, IMatchSwipeRepository
    {
        private readonly PersistenceContext _context;

        public MatchSwipeRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<MatchSwipe?> GetByPairAsync(int fromProfileId, int toProfileId)
        {
            return await _context.MatchSwipes
                .FirstOrDefaultAsync(s =>
                    s.FromProfileId == fromProfileId &&
                    s.ToProfileId == toProfileId &&
                    !s.IsDeleted);
        }

        public async Task<MatchSwipe?> GetLikeAsync(int fromProfileId, int toProfileId)
        {
            return await _context.MatchSwipes
                .FirstOrDefaultAsync(s =>
                    s.FromProfileId == fromProfileId &&
                    s.ToProfileId == toProfileId &&
                    s.Direction == SwipeDirection.Like &&
                    !s.IsDeleted);
        }
    }
}
