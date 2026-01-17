using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<List<User>> GetByIdsAsync(IReadOnlyCollection<int> userIds, CancellationToken ct = default);
    }
}
