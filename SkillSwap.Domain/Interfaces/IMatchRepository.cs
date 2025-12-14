using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IMatchRepository : IBaseRepository<Match>
    {
        Task<Match?> GetBetweenProfilesAsync(int profile1Id, int profile2Id);
        Task<HashSet<int>> GetPartnerProfileIdsAsync(int myProfileId);
        Task<List<Match>> GetByProfileIdWithProfilesAsync(int profileId);

    }
}
