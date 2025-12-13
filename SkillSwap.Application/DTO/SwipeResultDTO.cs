namespace SkillSwap.Application.DTO
{
    public class SwipeResultDTO
    {
        public bool IsMatch { get; set; }      // czy powstał realny Match
        public MatchDTO? Match { get; set; }   // dane matcha jeśli IsMatch = true
    }
}
