namespace SkillSwap.Application.DTO
{
    public class KanbanTaskAnswerDTO
    {
        public int Id { get; set; }
        public int TaskId { get; set; }

        public int ProfileId { get; set; }   
        public string Content { get; set; } = null!;

        public int? CheckerId { get; set; }  
        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
