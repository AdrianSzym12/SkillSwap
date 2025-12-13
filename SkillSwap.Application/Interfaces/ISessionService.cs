using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface ISessionService
    {
        Task<Result<LoginResultDTO>> LoginAsync(LoginDTO dto);
        Task<Result<string>> LogoutAsync(string token);
        Task<Result<CurrentSessionDTO>> GetCurrentAsync(string token);
        Task<Result<RegisterResponseDTO>> RegisterAsync(RegisterDTO dto);
    }
}