using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class UserSkillCreateDTO
    {
        [Required]
        public int ProfileId { get; set; }

        [Required]
        public int SkillId { get; set; }

        public bool Learned { get; set; }

        [Range(1, 4)]
        public int Level { get; set; } = 1;
    }
}
