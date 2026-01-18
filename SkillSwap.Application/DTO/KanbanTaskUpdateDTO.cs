using System.ComponentModel.DataAnnotations;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Application.DTO
{
    public class KanbanTaskUpdateDTO
    {
        public int? AssignedId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public KanbanTaskStatus Status { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
