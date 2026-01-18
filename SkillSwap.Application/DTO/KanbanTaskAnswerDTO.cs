using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class KanbanTaskAnswerDTO
    {
        public int Id { get; set; }

        [Required]
        public KanbanTaskDTO kanbanTask { get; set; }

        [Required]
        public ProfileDTO profile { get; set; }

        [Required, MaxLength(2000)]
        public string Content { get; set; }

        public int CheckerId { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
