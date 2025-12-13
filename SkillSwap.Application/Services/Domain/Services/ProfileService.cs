using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Interfaces;
using ProfileEntity = SkillSwap.Domain.Entities.Database.Profile;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public ProfileService(
            IProfileRepository profileRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<Result<ProfileDTO>> AddAsync(ProfileDTO profileDTO, int currentUserId)
        {
            try
            {
                var user = await _userRepository.GetAsync(currentUserId);
                if (user is null || user.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "User not found"
                    };
                }

                var existing = await _profileRepository.GetAnyByUserIdAsync(currentUserId);

                // ma aktywny → blokada
                if (existing is not null && !existing.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "User already has an active profile"
                    };
                }

                // ma soft-deleted → reaktywacja
                if (existing is not null && existing.IsDeleted)
                {
                    existing.UserName = profileDTO.UserName;
                    existing.Bio = profileDTO.Bio;
                    existing.Country = profileDTO.Country;
                    existing.Avatar = profileDTO.Avatar;
                    existing.PreferredMeetingType = profileDTO.PreferredMeetingType;
                    existing.PreferredLearningStyle = profileDTO.PreferredLearningStyle;
                    existing.Language = profileDTO.Language;
                    existing.Availability = profileDTO.Availability;
                    existing.IsDeleted = false;

                    var updated = await _profileRepository.UpdateAsync(existing);
                    var mapped = _mapper.Map<ProfileDTO>(updated);

                    return new()
                    {
                        IsSuccess = true,
                        Data = mapped,
                        Message = "Profile restored and updated"
                    };
                }
            

                // brak profilu → nowy
                var profile = _mapper.Map<ProfileEntity>(profileDTO);
                profile.UserId = currentUserId;
                profile.IsDeleted = false;

                var result = await _profileRepository.AddAsync(profile);
                var mappedNew = _mapper.Map<ProfileDTO>(result);

                return new()
                {
                    IsSuccess = true,
                    Data = mappedNew,
                    Message = "Profile created successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error creating profile: {ex.Message}"
                };
            }
        }


        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var profile = await _profileRepository.GetAsync(id);
                if (profile is null || profile.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                if (profile.UserId != currentUserId)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You are not allowed to delete this profile"
                    };
                }

                await _profileRepository.DeleteAsync(profile);

                return new()
                {
                    IsSuccess = true,
                    Data = "Profile deleted successfully",
                    Message = "Profile soft-deleted"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error deleting profile: {ex.Message}"
                };
            }
        }
        public async Task<Result<ProfileDTO>> GetAsync(int id)
        {
            try
            {
                var profile = await _profileRepository.GetAsync(id);
                if (profile is null || profile.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                var mapped = _mapper.Map<ProfileDTO>(profile);
                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Profile retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving profile: {ex.Message}"
                };
            }
        }
        public async Task<Result<List<ProfileDTO>>> GetAsync()
        {
            try
            {
                var profiles = await _profileRepository.GetAsync();
                var mapped = profiles
                    .Select(p => _mapper.Map<ProfileDTO>(p))
                    .ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Profiles retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving profiles: {ex.Message}"
                };
            }
        }

        public async Task<Result<ProfileDTO>> UpdateAsync(ProfileDTO profileDTO, int currentUserId)
        {
            try
            {
                var profile = await _profileRepository.GetAsync(profileDTO.id);
                if (profile is null || profile.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                if (profile.UserId != currentUserId)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You are not allowed to edit this profile"
                    };
                }

                profile.UserName = profileDTO.UserName;
                profile.Bio = profileDTO.Bio;
                profile.Country = profileDTO.Country;
                profile.Avatar = profileDTO.Avatar;

                var updated = await _profileRepository.UpdateAsync(profile);
                var mapped = _mapper.Map<ProfileDTO>(updated);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Profile updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error updating profile: {ex.Message}"
                };
            }
        }

        public async Task<Result<ProfileDTO>> GetByUserIdAsync(int userId)
        {
            try
            {
                var profile = await _profileRepository.GetByUserIdAsync(userId);
                if (profile is null || profile.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                var mapped = _mapper.Map<ProfileDTO>(profile);
                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Profile retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving profile: {ex.Message}"
                };
            }
        }
    }
}
