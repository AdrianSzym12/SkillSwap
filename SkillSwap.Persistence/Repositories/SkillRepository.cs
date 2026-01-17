using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class SkillRepository : BaseRepository<Skill>, ISkillRepository
    {
        private readonly PersistenceContext _context;

        public SkillRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Skill>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        {
            if (ids == null || ids.Count == 0)
                return new List<Skill>();

            return await _context.Skills
                .Where(s => ids.Contains(s.Id))
                .ToListAsync(ct);
        }
    }
}
