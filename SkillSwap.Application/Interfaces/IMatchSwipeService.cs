using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IMatchSwipeService
    {
        Task<Result<string>> LikeAsync(int currentUserId, int targetProfileId);
        Task<Result<string>> DislikeAsync(int currentUserId, int targetProfileId);
    }
}