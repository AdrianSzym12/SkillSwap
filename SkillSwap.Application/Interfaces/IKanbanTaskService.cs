using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IKanbanTaskService
    {
        Task<Result<KanbanTaskDTO>> GetAsync(int id, CancellationToken ct);

        Task<Result<List<KanbanTaskDTO>>> GetByBoardAsync(int boardId, int currentUserId, CancellationToken ct);

        Task<Result<KanbanTaskDTO>> AddAsync(KanbanTaskCreateDTO dto, int currentUserId, CancellationToken ct);

        Task<Result<KanbanTaskDTO>> UpdateAsync(int id, KanbanTaskUpdateDTO dto, int currentUserId, CancellationToken ct);

        Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct);
    }
}
