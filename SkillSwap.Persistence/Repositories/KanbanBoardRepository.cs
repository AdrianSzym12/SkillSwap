using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class KanbanBoardRepository : BaseRepository<KanbanBoard>, IKanbanBoardRepository
    {
        private readonly PersistenceContext _context;

        public KanbanBoardRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<KanbanBoard>> GetByMatchIdAsync(int matchId)
        {
            return await _context.KanbanBoards
                .Where(b => !b.IsDeleted && b.MatchId == matchId)
                .ToListAsync();
        }
    }
}
