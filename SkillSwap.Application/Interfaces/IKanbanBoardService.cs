using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

public interface IKanbanBoardService
{
    Task<Result<KanbanBoardDTO>> GetAsync(int id, CancellationToken ct);

    Task<Result<List<KanbanBoardDTO>>> GetByMatchAsync(int matchId, int currentUserId, CancellationToken ct);

    Task<Result<KanbanBoardDTO>> AddAsync(KanbanBoardCreateDTO dto, int currentUserId, CancellationToken ct);

    Task<Result<KanbanBoardDTO>> UpdateAsync(int id, KanbanBoardUpdateDTO dto, int currentUserId, CancellationToken ct);

    Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct);
}
