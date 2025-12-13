using System.ComponentModel.DataAnnotations;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Application.DTO
{
    public class SkillDTO
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        // NOWE:
        public SkillCategory Category { get; set; }

        [MaxLength(300)]
        public string Tags { get; set; }
    }
}
