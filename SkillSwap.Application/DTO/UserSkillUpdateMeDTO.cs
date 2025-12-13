using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class UserSkillUpdateMeDTO
    {
        public bool Learned { get; set; }

        [Range(1, 4)]
        public int Level { get; set; } = 1;
    }
}
