using System.ComponentModel.DataAnnotations;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Application.DTO
{
    public class KanbanTaskDTO
    {
        public int Id { get; set; }

        [Required]
        public KanbanBoardDTO kanbanBoard { get; set; }

        public int AssignedId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public KanbanTaskStatus Status { get; set; }
    }
}
