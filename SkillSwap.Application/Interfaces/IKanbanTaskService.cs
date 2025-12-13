using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IKanbanTaskService
    {
        Task<Result<KanbanTaskDTO>> GetAsync(int id);
        Task<Result<List<KanbanTaskDTO>>> GetAsync();

        Task<Result<KanbanTaskDTO>> AddAsync(KanbanTaskDTO dto, int currentUserId);
        Task<Result<KanbanTaskDTO>> UpdateAsync(KanbanTaskDTO dto, int currentUserId);
        Task<Result<string>> DeleteAsync(int id, int currentUserId);
        Task<Result<List<KanbanTaskDTO>>> GetByBoardAsync(int boardId, int currentUserId);
    }


}
