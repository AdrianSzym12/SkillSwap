using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class KanbanBoard : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int AuthorId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(MatchId))]
        public virtual Match Match { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public virtual User Author { get; set; }
    }
}
