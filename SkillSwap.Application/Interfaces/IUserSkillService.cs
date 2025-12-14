using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IUserSkillService
    {
        Task<Result<UserSkillDTO>> GetAsync(int id);
        Task<Result<List<UserSkillDTO>>> GetAsync();

        Task<Result<UserSkillDTO>> AddAsync(UserSkillDTO dto, int currentUserId);
        Task<Result<UserSkillDTO>> UpdateAsync(UserSkillDTO dto, int currentUserId);
        Task<Result<string>> DeleteAsync(int id, int currentUserId);
        Task<Result<UserSkillDTO>> AddMeAsync(UserSkillCreateMeDTO dto, int currentUserId);
        Task<Result<UserSkillDTO>> UpdateMeAsync(int userSkillId, UserSkillUpdateMeDTO dto, int currentUserId);
        Task<Result<List<UserSkillDTO>>> GetMeAsync(int currentUserId);

    }
}
