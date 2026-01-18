using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IKanbanTaskAnswerService
    {
        Task<Result<KanbanTaskAnswerDTO>> GetAsync(int id, CancellationToken ct);

        Task<Result<List<KanbanTaskAnswerDTO>>> GetByTaskAsync(int taskId, int currentUserId, CancellationToken ct);

        Task<Result<KanbanTaskAnswerDTO>> AddAsync(KanbanTaskAnswerCreateDTO dto, int currentUserId, CancellationToken ct);

        Task<Result<KanbanTaskAnswerDTO>> UpdateAsync(int id, KanbanTaskAnswerUpdateDTO dto, int currentUserId, CancellationToken ct);

        Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct);

        //  weryfikacja
        Task<Result<KanbanTaskAnswerDTO>> VerifyAsync(int id, int currentUserId, CancellationToken ct);
    }
}
