using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

public interface IMatchService
{
    Task<Result<MatchDTO>> GetAsync(int id);
    Task<Result<List<MatchDTO>>> GetAsync();
    Task<Result<MatchDTO>> AddAsync(MatchDTO dto);
    Task<Result<MatchDTO>> UpdateAsync(MatchDTO dto, int currentUserId);
    Task<Result<string>> DeleteAsync(int id, int currentUserId);
    Task<Result<List<MatchDTO>>> GetMyAsync(int currentUserId);



    Task<Result<SwipeResultDTO>> LikeAsync(int targetProfileId, int currentUserId);
    Task<Result<string>> DislikeAsync(int targetProfileId, int currentUserId);
}
