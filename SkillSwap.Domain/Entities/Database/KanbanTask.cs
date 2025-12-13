using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class KanbanTask : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public int BoardId { get; set; }
        public int AssignedId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public KanbanTaskStatus Status { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(BoardId))]
        public virtual KanbanBoard Board { get; set; }

        [ForeignKey(nameof(AssignedId))]
        public virtual User Assigned { get; set; }
    }
}
