using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class KanbanBoardDTO
    {
        public int Id { get; set; }

        [Required]
        public MatchDTO match { get; set; }

        [Required]
        public ProfileDTO user { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
