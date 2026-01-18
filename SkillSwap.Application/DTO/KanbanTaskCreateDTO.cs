using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class KanbanTaskCreateDTO
    {
        [Required]
        public int BoardId { get; set; }

        public int? AssignedId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
