using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Application.Interfaces
{
    public interface ISkillService
    {
        Task<Result<SkillDTO>> GetAsync(int id);
        Task<Result<List<SkillDTO>>> GetAsync();
        Task<Result<List<SkillDTO>>> SearchAsync(string? query, SkillCategory? category);
    }
}
