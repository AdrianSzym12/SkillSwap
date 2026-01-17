using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IKanbanTaskRepository : IBaseRepository<KanbanTask>
    {
        Task<List<KanbanTask>> GetByBoardIdAsync(int boardId, CancellationToken ct);

    }
}
