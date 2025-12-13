using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class UserSkillCreateMeDTO
    {
        [Required]
        public int SkillId { get; set; }

        public bool Learned { get; set; }

        [Range(1, 4)]
        public int Level { get; set; } = 1;
    }
}
