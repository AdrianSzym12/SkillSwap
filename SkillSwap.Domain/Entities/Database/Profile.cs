using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class Profile : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Bio { get; set; }
        public byte[] Avatar { get; set; }
        public string Country { get; set; }
        public bool IsDeleted { get; set; }

        public MeetingType PreferredMeetingType { get; set; }
        public LearningStyle PreferredLearningStyle { get; set; }

        public AvailabilitySlot Availability { get; set; }

        public bool IsOnboardingComplete { get; set; }
        public double ProfileCompletion { get; set; }

        public string Language { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
    }
}
