using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class MatchService : IMatchService
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IMatchSwipeRepository _swipeRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IMapper _mapper;

        public MatchService(
            IMatchRepository matchRepository,
            IMatchSwipeRepository swipeRepository,
            IProfileRepository profileRepository,
            IMapper mapper)
        {
            _matchRepository = matchRepository;
            _swipeRepository = swipeRepository;
            _profileRepository = profileRepository;
            _mapper = mapper;
        }

        public async Task<Result<MatchDTO>> GetAsync(int id)
        {
            try
            {
                var match = await _matchRepository.GetAsync(id);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var dto = _mapper.Map<MatchDTO>(match);
                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "Match retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving match: {ex.Message}" };
            }
        }

        public async Task<Result<List<MatchDTO>>> GetAsync()
        {
            try
            {
                var matches = await _matchRepository.GetAsync(); 
                var dtos = matches.Select(m => _mapper.Map<MatchDTO>(m)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "Matches retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving matches: {ex.Message}" };
            }
        }

        public async Task<Result<List<MatchDTO>>> GetMyAsync(int currentUserId)
        {
            try
            {
                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                var matches = await _matchRepository.GetAsync(); 
                var my = matches
                    .Where(m => m.Profile1Id == profile.Id || m.Profile2Id == profile.Id)
                    .ToList();

                var dtos = my.Select(m => _mapper.Map<MatchDTO>(m)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "User matches retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving user matches: {ex.Message}" };
            }
        }

        public async Task<Result<MatchDTO>> AddAsync(MatchDTO dto)
        {
            try
            {
                if (dto.profile1 == null || dto.profile2 == null)
                    return new() { IsSuccess = false, Message = "Profiles are required" };

                var p1 = await _profileRepository.GetAsync(dto.profile1.id);
                var p2 = await _profileRepository.GetAsync(dto.profile2.id);

                if (p1 is null || p1.IsDeleted || p2 is null || p2.IsDeleted)
                    return new() { IsSuccess = false, Message = "One or both profiles not found" };

                var entity = _mapper.Map<Match>(dto);
                entity.Profile1Id = p1.Id;
                entity.Profile2Id = p2.Id;
                entity.Status = dto.Status == 0 ? MatchStatus.Pending : dto.Status;
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                var added = await _matchRepository.AddAsync(entity);
                var mapped = _mapper.Map<MatchDTO>(added);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Match created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating match: {ex.Message}" };
            }
        }

        public async Task<Result<MatchDTO>> UpdateAsync(MatchDTO dto, int currentUserId)
        {
            try
            {
                var match = await _matchRepository.GetAsync(dto.Id);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                match.Status = dto.Status;

                var updated = await _matchRepository.UpdateAsync(match);
                var mapped = _mapper.Map<MatchDTO>(updated);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Match updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating match: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var match = await _matchRepository.GetAsync(id);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                await _matchRepository.DeleteAsync(match); 

                return new()
                {
                    IsSuccess = true,
                    Data = "Match deleted",
                    Message = "Match soft-deleted"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error deleting match: {ex.Message}" };
            }
        }
        public async Task<Result<SwipeResultDTO>> LikeAsync(int targetProfileId, int currentUserId)
        {
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (myProfile is null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (myProfile.Id == targetProfileId)
                    return new() { IsSuccess = false, Message = "You cannot like your own profile" };

                var targetProfile = await _profileRepository.GetAsync(targetProfileId);
                if (targetProfile is null || targetProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Target profile not found" };

                // Zapisz / zaktualizuj mój swipe
                var existingSwipe = await _swipeRepository.GetByPairAsync(myProfile.Id, targetProfileId);
                if (existingSwipe is null)
                {
                    existingSwipe = new MatchSwipe
                    {
                        FromProfileId = myProfile.Id,
                        ToProfileId = targetProfileId,
                        Direction = SwipeDirection.Like,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _swipeRepository.AddAsync(existingSwipe);
                }
                else
                {
                    existingSwipe.Direction = SwipeDirection.Like;
                    existingSwipe.IsDeleted = false;
                    existingSwipe.CreatedAt = DateTime.UtcNow;
                    await _swipeRepository.UpdateAsync(existingSwipe);
                }

                // Sprawdź czy druga strona już dała like
                var oppositeLike = await _swipeRepository.GetLikeAsync(targetProfileId, myProfile.Id);

                Match? match = null;
                bool isMatch = false;

                if (oppositeLike != null)
                {
                    // Sprawdź czy match nie istnieje
                    var existingMatch = await _matchRepository.GetBetweenProfilesAsync(myProfile.Id, targetProfileId);
                    if (existingMatch is null)
                    {
                        match = new Match
                        {
                            Profile1Id = myProfile.Id,
                            Profile2Id = targetProfileId,
                            Status = MatchStatus.Accepted,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        match = await _matchRepository.AddAsync(match);
                    }
                    else
                    {
                        existingMatch.Status = MatchStatus.Accepted;
                        match = await _matchRepository.UpdateAsync(existingMatch);
                    }

                    isMatch = true;
                }

                var dto = new SwipeResultDTO
                {
                    IsMatch = isMatch,
                    Match = match != null ? _mapper.Map<MatchDTO>(match) : null
                };

                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = isMatch ? "It's a match! 🎉" : "Like saved"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error during like: {ex.Message}"
                };
            }
        }

        public async Task<Result<string>> DislikeAsync(int targetProfileId, int currentUserId)
        {
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (myProfile is null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (myProfile.Id == targetProfileId)
                    return new() { IsSuccess = false, Message = "You cannot dislike your own profile" };

                var targetProfile = await _profileRepository.GetAsync(targetProfileId);
                if (targetProfile is null || targetProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Target profile not found" };

                var existingSwipe = await _swipeRepository.GetByPairAsync(myProfile.Id, targetProfileId);
                if (existingSwipe is null)
                {
                    existingSwipe = new MatchSwipe
                    {
                        FromProfileId = myProfile.Id,
                        ToProfileId = targetProfileId,
                        Direction = SwipeDirection.Dislike,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _swipeRepository.AddAsync(existingSwipe);
                }
                else
                {
                    existingSwipe.Direction = SwipeDirection.Dislike;
                    existingSwipe.IsDeleted = false;
                    existingSwipe.CreatedAt = DateTime.UtcNow;
                    await _swipeRepository.UpdateAsync(existingSwipe);
                }


                // opcjonalnie: można tutaj znaleźć istniejący Match i ustawić Status = Rejected

                return new()
                {
                    IsSuccess = true,
                    Data = "Dislike saved",
                    Message = "Dislike saved"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error during dislike: {ex.Message}"
                };
            }
        }
    }
}
