using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IKanbanTaskAnswerService
    {
        Task<Result<KanbanTaskAnswerDTO>> GetAsync(int id);
        Task<Result<List<KanbanTaskAnswerDTO>>> GetAsync();
        Task<Result<KanbanTaskAnswerDTO>> AddAsync(KanbanTaskAnswerDTO dto, int currentUserId);
        Task<Result<KanbanTaskAnswerDTO>> UpdateAsync(KanbanTaskAnswerDTO dto, int currentUserId);
        Task<Result<string>> DeleteAsync(int id, int currentUserId);
        Task<Result<List<KanbanTaskAnswerDTO>>> GetByTaskAsync(int taskId, int currentUserId);
    }
}
