using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

public interface IKanbanBoardService
{
    Task<Result<KanbanBoardDTO>> GetAsync(int id);
    Task<Result<List<KanbanBoardDTO>>> GetAsync();
    Task<Result<KanbanBoardDTO>> AddAsync(KanbanBoardDTO dto, int currentUserId);
    Task<Result<KanbanBoardDTO>> UpdateAsync(KanbanBoardDTO dto, int currentUserId);
    Task<Result<string>> DeleteAsync(int id, int currentUserId);

    Task<Result<List<KanbanBoardDTO>>> GetByMatchAsync(int matchId, int currentUserId);
}
