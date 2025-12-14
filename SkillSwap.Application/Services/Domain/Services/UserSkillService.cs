using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class UserSkillService : IUserSkillService
    {
        private readonly IUserSkillRepository _userSkillRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IMapper _mapper;
        private readonly ISkillRepository _skillRepository;

        public UserSkillService(
            IUserSkillRepository userSkillRepository,
            IProfileRepository profileRepository,
            ISkillRepository skillRepository,
            IMapper mapper)
        {
            _userSkillRepository = userSkillRepository;
            _profileRepository = profileRepository;
            _skillRepository = skillRepository;
            _mapper = mapper;
        }

        public async Task<Result<UserSkillDTO>> GetAsync(int id)
        {
            try
            {
                var entity = await _userSkillRepository.GetWithDetailsAsync(id);
                if (entity is null)
                    return new() { IsSuccess = false, Message = "UserSkill not found" };

                var dto = _mapper.Map<UserSkillDTO>(entity);
                return new() { IsSuccess = true, Data = dto, Message = "Retrieved" };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Result<List<UserSkillDTO>>> GetAsync()
        {
            try
            {
                var entities = await _userSkillRepository.GetAsync(); 
                var dtos = _mapper.Map<List<UserSkillDTO>>(entities);

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "Retrieved all"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Result<UserSkillDTO>> AddAsync(UserSkillDTO dto, int currentUserId)
        {
            try
            {
                if (dto.skill == null || dto.skill.Id <= 0)
                    return new() { IsSuccess = false, Message = "Skill is required" };

                var skill = await _skillRepository.GetAsync(dto.skill.Id);
                if (skill is null || skill.IsDeleted)
                    return new() { IsSuccess = false, Message = "Skill not found" };


                if (dto.profile == null || dto.profile.id <= 0)
                    return new() { IsSuccess = false, Message = "Profile is required" };

                var profile = await _profileRepository.GetAsync(dto.profile.id);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                if (profile.UserId != currentUserId)
                    return new() { IsSuccess = false, Message = "You cannot add skills to another user's profile" };

                var entity = _mapper.Map<UserSkill>(dto);
                entity.ProfileId = profile.Id;

                var result = await _userSkillRepository.AddAsync(entity);
                var mapped = _mapper.Map<UserSkillDTO>(result);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "UserSkill created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating: {ex.Message}" };
            }
        }

        public async Task<Result<UserSkillDTO>> UpdateAsync(UserSkillDTO dto, int currentUserId)
        {
            try
            {
                var entity = await _userSkillRepository.GetAsync(dto.Id);
                if (entity is null)
                    return new() { IsSuccess = false, Message = "UserSkill not found" };

                var profile = await _profileRepository.GetAsync(entity.ProfileId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                if (profile.UserId != currentUserId)
                    return new() { IsSuccess = false, Message = "You cannot edit another user's skill" };

                entity.Level = dto.Level;
                entity.Learned = dto.Learned;
                entity.SkillId = dto.skill.Id;

                var updated = await _userSkillRepository.UpdateAsync(entity);
                var mapped = _mapper.Map<UserSkillDTO>(updated);

                return new() { IsSuccess = true, Data = mapped, Message = "Updated successfully" };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var entity = await _userSkillRepository.GetAsync(id);
                if (entity is null || entity.IsDeleted)
                {
                    return new() { IsSuccess = false, Message = "UserSkill not found" };
                }

                var profile = await _profileRepository.GetAsync(entity.ProfileId);
                if (profile is null || profile.IsDeleted)
                {
                    return new() { IsSuccess = false, Message = "Profile not found" };
                }

                if (profile.UserId != currentUserId)
                {
                    return new() { IsSuccess = false, Message = "You cannot delete another user's skill" };
                }

                await _userSkillRepository.DeleteAsync(entity);

                return new()
                {
                    IsSuccess = true,
                    Data = "UserSkill deleted",
                    Message = "Deleted"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error deleting: {ex.Message}" };
            }
        }

        public async Task<Result<UserSkillDTO>> AddMeAsync(UserSkillCreateMeDTO dto, int currentUserId)
        {
            try
            {

                // 1) weź profil zalogowanego usera
                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                var skill = await _skillRepository.GetAsync(dto.SkillId);
                if (skill is null || skill.IsDeleted)
                    return new() { IsSuccess = false, Message = "Skill not found" };


                // 2) (opcjonalnie, ale polecam) sprawdź duplikat
                var existing = await _userSkillRepository.GetByProfileAndSkillAsync(profile.Id, dto.SkillId);
                if (existing is not null && !existing.IsDeleted)
                    return new() { IsSuccess = false, Message = "UserSkill already exists" };

                if (existing is not null && existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.Learned = dto.Learned;
                    existing.Level = (SkillLevel)dto.Level;
                    existing.SkillId = dto.SkillId;

                    var updated = await _userSkillRepository.UpdateAsync(existing);
                    var mappedRestored = _mapper.Map<UserSkillDTO>(updated);

                    return new()
                    {
                        IsSuccess = true,
                        Data = mappedRestored,
                        Message = "UserSkill restored"
                    };
                }

                // 3) create (bez ProfileDTO/SkillDTO)
                var entity = new UserSkill
                {
                    ProfileId = profile.Id,
                    SkillId = dto.SkillId,
                    Learned = dto.Learned,
                    Level = (SkillLevel)dto.Level,
                    IsDeleted = false
                };

                var created = await _userSkillRepository.AddAsync(entity);

                var full = await _userSkillRepository.GetWithDetailsAsync(created.Id);

                var mapped = _mapper.Map<UserSkillDTO>(full);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "UserSkill created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating: {ex.Message}" };
            }
        }
        public async Task<Result<UserSkillDTO>> UpdateMeAsync(int userSkillId, UserSkillUpdateMeDTO dto, int currentUserId)
        {
            try
            {
                // 1) pobierz userskill
                var entity = await _userSkillRepository.GetAsync(userSkillId);
                if (entity is null || entity.IsDeleted)
                    return new() { IsSuccess = false, Message = "UserSkill not found" };

                // 2) sprawdź, czy należy do profilu tego usera
                var profile = await _profileRepository.GetAsync(entity.ProfileId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                if (profile.UserId != currentUserId)
                    return new() { IsSuccess = false, Message = "You cannot edit another user's skill" };

                // 3) update pól
                entity.Learned = dto.Learned;
                entity.Level = (SkillLevel)dto.Level;

                var updated = await _userSkillRepository.UpdateAsync(entity);
                var mapped = _mapper.Map<UserSkillDTO>(updated);

                return new() { IsSuccess = true, Data = mapped, Message = "Updated successfully" };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating: {ex.Message}" };
            }
        }

    }
}
