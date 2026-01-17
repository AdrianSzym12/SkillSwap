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

        public async Task<Result<MatchDTO>> GetAsync(int id, CancellationToken ct = default)
        {
            // ===== Pobranie pojedynczego matchu =====
            try
            {
                var match = await _matchRepository.GetAsync(id, ct);
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

        public async Task<Result<List<MatchDTO>>> GetAsync(CancellationToken ct = default)
        {
            // ===== Lista wszystkich matchy (admin/dev) =====
            try
            {
                var matches = await _matchRepository.GetAsync(ct);
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

        public async Task<Result<List<MatchDTO>>> GetMyAsync(int currentUserId, CancellationToken ct = default)
        {
            // ===== Moje matche (po profilu zalogowanego użytkownika) =====
            try
            {
                var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found", StatusCode = 404 };

                var my = await _matchRepository.GetByProfileIdAsync(profile.Id, ct);

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

        public async Task<Result<MatchDTO>> AddAsync(MatchDTO dto, CancellationToken ct = default)
        {
            // ===== Utworzenie matchu (głównie wewnętrznie / debug) =====
            try
            {
                if (dto.profile1 == null || dto.profile2 == null)
                    return new() { IsSuccess = false, Message = "Profiles are required" };

                var p1 = await _profileRepository.GetAsync(dto.profile1.id, ct);
                var p2 = await _profileRepository.GetAsync(dto.profile2.id, ct);

                if (p1 is null || p1.IsDeleted || p2 is null || p2.IsDeleted)
                    return new() { IsSuccess = false, Message = "One or both profiles not found" };

                var entity = _mapper.Map<Match>(dto);
                var a = Math.Min(p1.Id, p2.Id);
                var b = Math.Max(p1.Id, p2.Id);
                entity.Profile1Id = a;
                entity.Profile2Id = b;
                entity.Status = dto.Status == 0 ? MatchStatus.Pending : dto.Status;
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                var added = await _matchRepository.AddAsync(entity, ct);
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

        public async Task<Result<MatchDTO>> UpdateAsync(MatchDTO dto, int currentUserId, CancellationToken ct = default)
        {
            // ===== Aktualizacja statusu matchu (tylko uczestnik) =====
            try
            {
                var match = await _matchRepository.GetAsync(dto.Id);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                match.Status = dto.Status;

                var updated = await _matchRepository.UpdateAsync(match, ct);
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

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct = default)
        {
            // ===== Soft delete matchu (tylko uczestnik) =====
            try
            {
                var match = await _matchRepository.GetAsync(id, ct);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                await _matchRepository.DeleteAsync(match, ct);

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
        public async Task<Result<SwipeResultDTO>> LikeAsync(int targetProfileId, int currentUserId, CancellationToken ct = default)
        {
            // ===== Swipe LIKE + ewentualne utworzenie matchu =====
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (myProfile is null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found", StatusCode = 404 };

                if (myProfile.Id == targetProfileId)
                    return new() { IsSuccess = false, Message = "You cannot like your own profile", StatusCode = 400 };

                var targetProfile = await _profileRepository.GetAsync(targetProfileId, ct);
                if (targetProfile is null || targetProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Target profile not found", StatusCode = 404 };

                // Zapisz / zaktualizuj mój swipe
                var existingSwipe = await _swipeRepository.GetByPairAsync(myProfile.Id, targetProfileId);
                if (existingSwipe != null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You already swiped this profile",
                        StatusCode = 409
                    };
                }

                var swipe = new MatchSwipe
                {
                    FromProfileId = myProfile.Id,
                    ToProfileId = targetProfileId,
                    Direction = SwipeDirection.Like,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _swipeRepository.AddAsync(swipe, ct);

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
                        var a = Math.Min(myProfile.Id, targetProfileId);
                        var b = Math.Max(myProfile.Id, targetProfileId);
                        match = new Match
                        {
                            Profile1Id = a,
                            Profile2Id = b,
                            Status = MatchStatus.Accepted,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        match = await _matchRepository.AddAsync(match, ct);
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

        public async Task<Result<string>> DislikeAsync(int targetProfileId, int currentUserId, CancellationToken ct = default)
        {
            // ===== Swipe DISLIKE =====
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (myProfile is null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found", StatusCode = 404 };

                if (myProfile.Id == targetProfileId)
                    return new() { IsSuccess = false, Message = "You cannot dislike your own profile", StatusCode = 400 };

                var targetProfile = await _profileRepository.GetAsync(targetProfileId, ct);
                if (targetProfile is null || targetProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Target profile not found", StatusCode = 404 };

                var existingSwipe = await _swipeRepository.GetByPairAsync(myProfile.Id, targetProfileId);
                if (existingSwipe != null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You already swiped this profile",
                        StatusCode = 409
                    };
                }

                var swipe = new MatchSwipe
                {
                    FromProfileId = myProfile.Id,
                    ToProfileId = targetProfileId,
                    Direction = SwipeDirection.Dislike,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _swipeRepository.AddAsync(swipe, ct);


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
