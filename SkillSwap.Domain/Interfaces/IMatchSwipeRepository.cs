using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IMatchSwipeRepository : IBaseRepository<MatchSwipe>
    {
        Task<MatchSwipe?> GetByPairAsync(int fromProfileId, int toProfileId);
        Task<MatchSwipe?> GetLikeAsync(int fromProfileId, int toProfileId);
    }
}
