using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class KanbanTaskAnswerUpdateDTO
    {
        [Required, MaxLength(2000)]
        public string Content { get; set; } = null!;
    }
}
