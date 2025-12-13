namespace SkillSwap.Application.DTO
{
    public class MatchSuggestionDTO
    {
        public int ProfileId { get; set; }
        public ProfileDTO Profile { get; set; }

        public double MatchScore { get; set; }

        public double SkillFitScore { get; set; }
        public double LevelFitScore { get; set; }
        public double AvailabilityScore { get; set; }
        public double PreferenceScore { get; set; }
        public double DistanceScore { get; set; }

        public double OpinionFactor { get; set; }
        public double ProfileFactor { get; set; }
        public double NewUserBoost { get; set; }
    }
}
