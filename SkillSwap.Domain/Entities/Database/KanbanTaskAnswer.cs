using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class KanbanTaskAnswer : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        public int KanbanTaskId { get; set; }

        [ForeignKey(nameof(KanbanTaskId))]
        public virtual KanbanTask KanbanTask { get; set; } = null!;

        public int ProfileId { get; set; }

        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public int? CheckerId { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public virtual Profile Profile { get; set; } = null!;

        [ForeignKey(nameof(CheckerId))]
        public virtual User? Checker { get; set; } // ✅ nullable, bo CheckerId nullable
    }
}
