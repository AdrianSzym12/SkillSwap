using System.ComponentModel.DataAnnotations;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Application.DTO
{
    public class MatchDTO
    {
        public int Id { get; set; }

        [Required]
        public ProfileDTO profile1 { get; set; }

        [Required]
        public ProfileDTO profile2 { get; set; }

        public MatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
