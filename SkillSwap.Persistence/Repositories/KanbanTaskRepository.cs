using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Persistence.Repositories
{
    public class KanbanTaskRepository : BaseRepository<KanbanTask>, IKanbanTaskRepository
    {
        public KanbanTaskRepository(PersistenceContext context) : base(context)
        {
        }
    }
}
