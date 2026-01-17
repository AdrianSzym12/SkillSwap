using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IKanbanBoardRepository : IBaseRepository<KanbanBoard>
    {
        Task<List<KanbanBoard>> GetByMatchIdAsync(int matchId, CancellationToken ct);
    }
}
