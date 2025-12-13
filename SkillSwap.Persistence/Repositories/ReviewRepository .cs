using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class ReviewRepository : BaseRepository<Review>, IReviewRepository
    {
        private readonly PersistenceContext _context;

        public ReviewRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetByToProfileIdAsync(int toProfileId)
        {
            return await _context.Reviews
                .Where(r => r.ToProfileId == toProfileId && !r.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Review>> GetByMatchIdAsync(int matchId)
        {
            return await _context.Reviews
                .Where(r => r.MatchId == matchId && !r.IsDeleted)
                .ToListAsync();
        }
        public async Task<Review?> GetByFromProfileAndMatchAsync(int fromProfileId, int matchId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r =>
                    r.FromProfileId == fromProfileId &&
                    r.MatchId == matchId &&
                    !r.IsDeleted);
        }
    }
}
