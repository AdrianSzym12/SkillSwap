using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class UserSkill : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public int SkillId { get; set; }
        public bool Learned { get; set; }
        public SkillLevel Level { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public virtual Profile Profile { get; set; }

        [ForeignKey(nameof(SkillId))]
        public virtual Skill Skill { get; set; }
    }
}
