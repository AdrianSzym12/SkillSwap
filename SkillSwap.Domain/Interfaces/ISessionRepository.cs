using SkillSwap.Domain.Entities.Database;


namespace SkillSwap.Domain.Interfaces
{
    public interface ISessionRepository : IBaseRepository<Session>
    {
        Task<Session?> GetByTokenAsync(string token);
    }
}
