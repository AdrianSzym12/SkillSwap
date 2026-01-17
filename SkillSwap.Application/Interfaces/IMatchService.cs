using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IMatchService
    {
        Task<Result<MatchDTO>> GetAsync(int id, CancellationToken ct = default);
        Task<Result<List<MatchDTO>>> GetAsync(CancellationToken ct = default);
        Task<Result<MatchDTO>> AddAsync(MatchDTO dto, CancellationToken ct = default);
        Task<Result<MatchDTO>> UpdateAsync(MatchDTO dto, int currentUserId, CancellationToken ct = default);
        Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct = default);
        Task<Result<List<MatchDTO>>> GetMyAsync(int currentUserId, CancellationToken ct = default);

        // Swipe API (dating-app style)
        Task<Result<SwipeResultDTO>> LikeAsync(int targetProfileId, int currentUserId, CancellationToken ct = default);
        Task<Result<string>> DislikeAsync(int targetProfileId, int currentUserId, CancellationToken ct = default);
    }
}
