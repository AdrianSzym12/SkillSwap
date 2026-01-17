using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class KanbanTaskRepository : BaseRepository<KanbanTask>, IKanbanTaskRepository
    {
        private readonly PersistenceContext _context;

        public KanbanTaskRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<KanbanTask>> GetByBoardIdAsync(int boardId, CancellationToken ct)
        {
            return await _context.KanbanTasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.BoardId == boardId)
                .ToListAsync(ct);
        }
    }
}
