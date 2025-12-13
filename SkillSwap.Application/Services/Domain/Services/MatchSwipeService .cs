using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class MatchSwipeService : IMatchSwipeService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IMatchSwipeRepository _swipeRepository;
        private readonly IMatchRepository _matchRepository;

        public MatchSwipeService(
            IProfileRepository profileRepository,
            IMatchSwipeRepository swipeRepository,
            IMatchRepository matchRepository)
        {
            _profileRepository = profileRepository;
            _swipeRepository = swipeRepository;
            _matchRepository = matchRepository;
        }

        public async Task<Result<string>> LikeAsync(int currentUserId, int targetProfileId)
        {
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (myProfile == null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                if (myProfile.Id == targetProfileId)
                    return new() { IsSuccess = false, Message = "You cannot like yourself" };

                var targetProfile = await _profileRepository.GetAsync(targetProfileId);
                if (targetProfile == null || targetProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Target profile not found" };

                var existing = await _swipeRepository.GetByPairAsync(myProfile.Id, targetProfileId);
                if (existing != null && existing.Direction == SwipeDirection.Like && !existing.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = true,
                        Data = "Already liked",
                        Message = "Already liked"
                    };
                }

                if (existing == null)
                {
                    existing = new MatchSwipe
                    {
                        FromProfileId = myProfile.Id,
                        ToProfileId = targetProfileId,
                        Direction = SwipeDirection.Like,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _swipeRepository.AddAsync(existing);
                }
                else
                {
                    existing.Direction = SwipeDirection.Like;
                    existing.CreatedAt = DateTime.UtcNow;
                    existing.IsDeleted = false;
                    await _swipeRepository.UpdateAsync(existing);
                }

                var likeBack = await _swipeRepository.GetLikeAsync(targetProfileId, myProfile.Id);

                if (likeBack != null)
                {
                    var existingMatch = await _matchRepository.GetBetweenProfilesAsync(myProfile.Id, targetProfileId);
                    if (existingMatch == null)
                    {
                        var match = new Match
                        {
                            Profile1Id = myProfile.Id,
                            Profile2Id = targetProfileId,
                            Status = MatchStatus.Accepted,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _matchRepository.AddAsync(match);
                    }

                    return new()
                    {
                        IsSuccess = true,
                        Data = "It's a match!",
                        Message = "It's a match!"
                    };
                }

                return new()
                {
                    IsSuccess = true,
                    Data = "Liked, waiting for other user",
                    Message = "Liked, waiting for other user"
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

        public async Task<Result<string>> DislikeAsync(int currentUserId, int targetProfileId)
        {
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (myProfile == null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                if (myProfile.Id == targetProfileId)
                    return new() { IsSuccess = false, Message = "You cannot dislike yourself" };

                var targetProfile = await _profileRepository.GetAsync(targetProfileId);
                if (targetProfile == null || targetProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Target profile not found" };

                var existing = await _swipeRepository.GetByPairAsync(myProfile.Id, targetProfileId);
                if (existing != null && existing.Direction == SwipeDirection.Dislike && !existing.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = true,
                        Data = "Already disliked",
                        Message = "Already disliked"
                    };
                }

                if (existing == null)
                {
                    existing = new MatchSwipe
                    {
                        FromProfileId = myProfile.Id,
                        ToProfileId = targetProfileId,
                        Direction = SwipeDirection.Dislike,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _swipeRepository.AddAsync(existing);
                }
                else
                {
                    existing.Direction = SwipeDirection.Dislike;
                    existing.CreatedAt = DateTime.UtcNow;
                    existing.IsDeleted = false;
                    await _swipeRepository.UpdateAsync(existing);
                }

                return new()
                {
                    IsSuccess = true,
                    Data = "Disliked",
                    Message = "Disliked"
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
