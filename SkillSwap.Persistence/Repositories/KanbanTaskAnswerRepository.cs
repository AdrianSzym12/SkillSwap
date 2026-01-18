using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class KanbanTaskAnswerRepository : BaseRepository<KanbanTaskAnswer>, IKanbanTaskAnswerRepository
    {
        private readonly PersistenceContext _context;

        public KanbanTaskAnswerRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<KanbanTaskAnswer>> GetByTaskIdAsync(int taskId, CancellationToken ct)
        {
            return await _context.KanbanTaskAnswers
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.Id == taskId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync(ct);
        }
    }
}
