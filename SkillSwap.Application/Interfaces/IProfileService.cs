using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IProfileService
    {
        Task<Result<ProfileDTO>> GetAsync(int id);
        Task<Result<List<ProfileDTO>>> GetAsync();
        Task<Result<ProfileDTO>> AddAsync(ProfileDTO profileDTO, int currentUserId);
        Task<Result<ProfileDTO>> UpdateAsync(ProfileDTO profileDTO, int userId);
        Task<Result<string>> DeleteAsync(int id, int userId);

        Task<Result<ProfileDTO>> GetByUserIdAsync(int userId);
    }
}
