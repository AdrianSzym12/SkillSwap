namespace SkillSwap.Application.DTO
{
    public class ReviewDTO
    {
        public int Id { get; set; }
        public int FromProfileId { get; set; }  
        public int ToProfileId { get; set; }
        public int MatchId { get; set; }

        public int CooperationRating { get; set; }
        public int WorkQualityRating { get; set; }
        public int KnowledgeGainRating { get; set; }

        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
