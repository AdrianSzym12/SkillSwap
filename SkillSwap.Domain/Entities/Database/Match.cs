using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class Match : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public int Profile1Id { get; set; }
        public int Profile2Id { get; set; }
        public MatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(Profile1Id))]
        public virtual Profile Profile1 { get; set; }

        [ForeignKey(nameof(Profile2Id))]
        public virtual Profile Profile2 { get; set; }
    }
}
