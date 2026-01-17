using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IUserSkillRepository : IBaseRepository<UserSkill>
    {
        Task<List<UserSkill>> GetByProfileIdAsync(int profileId, CancellationToken ct = default);
        Task<UserSkill?> GetByProfileAndSkillAsync(int profileId, int skillId, CancellationToken ct = default);
        Task<List<UserSkill>> GetByProfileIdsAsync(IReadOnlyCollection<int> profileIds, CancellationToken ct = default);
    }
}
