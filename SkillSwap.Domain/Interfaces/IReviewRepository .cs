using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IReviewRepository : IBaseRepository<Review>
    {
        Task<List<Review>> GetByToProfileIdAsync(int toProfileId);
        Task<List<Review>> GetByFromProfileIdAsync(int fromProfileId);
        Task<List<Review>> GetByMatchIdAsync(int matchId);

        Task<Review?> GetByFromProfileAndMatchAsync(int fromProfileId, int matchId);
    }
}
