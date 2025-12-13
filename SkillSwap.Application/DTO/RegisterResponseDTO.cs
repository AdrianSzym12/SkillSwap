using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class RegisterResponseDTO
    {
        public int UserId { get; set; }
        public int ProfileId { get; set; }

        // opcjonalnie: jeśli od razu tworzysz sesję/token
        public string AccessToken { get; set; }

        public bool RequiresOnboarding { get; set; }
        public double ProfileCompletion { get; set; }

        [Required]
        public List<string> MissingFields { get; set; } = new();

    }
}
