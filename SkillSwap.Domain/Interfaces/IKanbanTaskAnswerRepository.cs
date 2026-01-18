using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IKanbanTaskAnswerRepository : IBaseRepository<KanbanTaskAnswer>
    {
        Task<List<KanbanTaskAnswer>> GetByTaskIdAsync(int taskId, CancellationToken ct);
    }
}
