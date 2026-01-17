using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IMatchSwipeRepository : IBaseRepository<MatchSwipe>
    {
        Task<MatchSwipe?> GetByPairAsync(int fromProfileId, int toProfileId, CancellationToken ct = default);
        Task<MatchSwipe?> GetLikeAsync(int fromProfileId, int toProfileId, CancellationToken ct = default);
        Task<List<int>> GetSwipedToProfileIdsAsync(int fromProfileId, CancellationToken ct = default);
    }
}
