using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class KanbanTaskAnswerRepository : BaseRepository<KanbanTaskAnswer>, IKanbanTaskAnswerRepository
    {
        public KanbanTaskAnswerRepository(PersistenceContext dbContext) : base(dbContext)
        {
        }
    }
    
}
