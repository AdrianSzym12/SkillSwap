using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class KanbanTaskAnswerCreateDTO
    {
        [Required]
        public int TaskId { get; set; }

        [Required, MaxLength(2000)]
        public string Content { get; set; } = null!;
    }
}
