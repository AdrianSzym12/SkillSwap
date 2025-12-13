using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Domain.Entities.Database
{
    public class Review : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        public int FromProfileId { get; set; }   
        public int ToProfileId { get; set; }     
        public int MatchId { get; set; }         

        [Range(1, 5)]
        public int CooperationRating { get; set; }

        [Range(1, 5)]
        public int WorkQualityRating { get; set; }

        [Range(1, 5)]
        public int KnowledgeGainRating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(FromProfileId))]
        public virtual Profile FromProfile { get; set; }

        [ForeignKey(nameof(ToProfileId))]
        public virtual Profile ToProfile { get; set; }

        [ForeignKey(nameof(MatchId))]
        public virtual Match Match { get; set; }
    }
}
