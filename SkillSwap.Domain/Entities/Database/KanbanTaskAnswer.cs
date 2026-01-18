using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class KanbanTaskAnswer : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public KanbanTask kanbanTask { get; set; }
        public int ProfileId { get; set; }
        public string Content { get; set; }
        public int CheckerId { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public virtual Profile Profile { get; set; }

        [ForeignKey(nameof(CheckerId))]
        public virtual User Checker { get; set; }
    }
}
