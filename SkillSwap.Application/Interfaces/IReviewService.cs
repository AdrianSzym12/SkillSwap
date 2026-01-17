using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IReviewService
    {
        Task<Result<ReviewDTO>> GetAsync(int id);
        Task<Result<List<ReviewDTO>>> GetByProfileAsync(int profileId);
        Task<Result<List<ReviewDTO>>> GetByMatchAsync(int matchId);

        Task<Result<ReviewDTO>> AddAsync(ReviewDTO dto, int currentUserId);
        Task<Result<string>> DeleteAsync(int id, int currentUserId);
    }
}
