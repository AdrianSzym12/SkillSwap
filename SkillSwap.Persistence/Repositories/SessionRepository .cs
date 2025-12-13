using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;


namespace SkillSwap.Persistence.Repositories
{
    public class SessionRepository : BaseRepository<Session>, ISessionRepository
    {
        private readonly PersistenceContext _context;

        public SessionRepository(PersistenceContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Session?> GetByTokenAsync(string token)
        {
            return await _context.Sessions.FirstOrDefaultAsync(s => s.JwtToken == token);
        }
    }
}
