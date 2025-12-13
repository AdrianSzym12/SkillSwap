using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Domain.Entities.Database
{
    public class Skill : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        // NOWE:
        public SkillCategory Category { get; set; } = SkillCategory.Other;

        
        [MaxLength(300)]
        public string Tags { get; set; }

        public bool IsDeleted { get; set; }
    }
}
