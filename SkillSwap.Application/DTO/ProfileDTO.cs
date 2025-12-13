

using SkillSwap.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class ProfileDTO
    {
        public int id { get; set; }
        public UserDTO User { get; set; }
        [Required, MaxLength(50)]
        public string UserName { get; set; }

        [MaxLength(500)]
        public string Bio { get; set; }

        public byte[] Avatar { get; set; }
        public MeetingType PreferredMeetingType { get; set; }
        public LearningStyle PreferredLearningStyle { get; set; }
        public string Language { get; set; }

        public AvailabilitySlot Availability { get; set; }

        [Required, MaxLength(100)]
        public string Country { get; set; }
    }
}
