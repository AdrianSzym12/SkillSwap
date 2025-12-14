using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IUserSkillRepository : IBaseRepository<UserSkill>
    {
        Task<List<UserSkill>> GetByProfileIdAsync(int profileId);
        Task<UserSkill?> GetByProfileAndSkillAsync(int profileId, int skillId);
        Task<List<UserSkill>> GetByProfileIdsAsync(List<int> profileIds);

    }
}
