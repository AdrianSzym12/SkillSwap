using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Domain.Interfaces
{
    public interface ISkillRepository : IBaseRepository<Skill>
    {
        Task<List<Skill>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    }
}
