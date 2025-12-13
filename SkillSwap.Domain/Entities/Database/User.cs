using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;


namespace SkillSwap.Domain.Entities.Database 
{
    public class User : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Średnia ocena współpracy (1–5)</summary>
        public double AvgCooperationRating { get; set; }

        /// <summary>Średnia ocena jakości pracy (1–5)</summary>
        public double AvgWorkQualityRating { get; set; }

        /// <summary>Średnia ocena „ile się nauczyłem” (1–5)</summary>
        public double AvgKnowledgeGainRating { get; set; }

        /// <summary>Liczba opinii wystawionych temu użytkownikowi</summary>
        public int ReviewsCount { get; set; }
    }
}
