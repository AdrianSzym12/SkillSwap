using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Domain.Entities.Database
{
    public class MatchSwipe
    {
        [Key]
        public int Id { get; set; }

        public int FromProfileId { get; set; }
        public int ToProfileId { get; set; }

        public SwipeDirection Direction { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(FromProfileId))]
        public virtual Profile FromProfile { get; set; }

        [ForeignKey(nameof(ToProfileId))]
        public virtual Profile ToProfile { get; set; }
    }
}
