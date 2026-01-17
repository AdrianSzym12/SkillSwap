using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IProfileRepository : IBaseRepository<Profile>
    {
        Task<Profile?> GetByUserIdAsync(int userId, CancellationToken ct = default);
        Task<Profile?> GetAnyByUserIdAsync(int userId, CancellationToken ct = default);
        Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default);
    }
}
