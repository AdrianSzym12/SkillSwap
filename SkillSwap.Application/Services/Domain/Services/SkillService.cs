using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class SkillService : ISkillService
    {
        private readonly ISkillRepository _skillRepository;
        private readonly IMapper _mapper;

        public SkillService(ISkillRepository skillRepository, IMapper mapper)
        {
            _skillRepository = skillRepository;
            _mapper = mapper;
        }

        public async Task<Result<SkillDTO>> GetAsync(int id)
        {
            try
            {
                var skill = await _skillRepository.GetAsync(id);
                if (skill == null)
                    return new() { IsSuccess = false, Message = "Skill not found" };

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<SkillDTO>(skill),
                    Message = "Skill retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Result<List<SkillDTO>>> GetAsync()
        {
            try
            {
                var skills = await _skillRepository.GetAsync();
                var dtos = skills.Select(s => _mapper.Map<SkillDTO>(s)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "Skills retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Result<List<SkillDTO>>> SearchAsync(string? query, SkillCategory? category)
        {
            try
            {
                var skills = await _skillRepository.GetAsync();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var q = query.Trim().ToLower();
                    skills = skills
                        .Where(s =>
                            s.Name.ToLower().Contains(q) ||
                            !string.IsNullOrWhiteSpace(s.Tags) && s.Tags.ToLower().Contains(q))
                        .ToList();
                }

                if (category.HasValue)
                {
                    skills = skills
                        .Where(s => s.Category == category.Value)
                        .ToList();
                }

                var dtos = skills.Select(s => _mapper.Map<SkillDTO>(s)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "Skills search completed"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error searching skills: {ex.Message}"
                };
            }
        }
    }
}
