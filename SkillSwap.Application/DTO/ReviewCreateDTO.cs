using System.ComponentModel.DataAnnotations;

public class ReviewCreateDTO
{
    public int MatchId { get; set; }

    [Range(1, 5)] 
    public int CooperationRating { get; set; }

    [Range(1, 5)] 
    public int WorkQualityRating { get; set; }

    [Range(1, 5)] 
    public int KnowledgeGainRating { get; set; }

    [MaxLength(2000)] 
    public string? Comment { get; set; }
}
