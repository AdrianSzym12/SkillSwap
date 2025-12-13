using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserDTO>> GetAsync(int id);
        Task<Result<UserDTO>> AddAsync(UserDTO userDTO);
        Task<Result<UserDTO>> UpdateAsync(UserDTO userDTO);
        Task<Result<string>> DeleteAsync(int id, int currentUserId);
    }
}
