using SkillSwap.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class UserSkillDTO
    {
        public int Id { get; set; }

        [Required]
        public ProfileDTO profile { get; set; }

        [Required]
        public SkillDTO skill { get; set; }

        public bool Learned { get; set; }

        public SkillLevel Level { get; set; }
    }
}
