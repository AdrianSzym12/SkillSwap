using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IProfileRepository :IBaseRepository<Profile>
    {
        Task<Profile?> GetByUserIdAsync(int userId);
        Task<Profile?> GetAnyByUserIdAsync(int userId);
        Task<bool> ExistsByUserNameAsync(string userName);
    }
  
}
