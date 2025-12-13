using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public ReviewService(
            IReviewRepository reviewRepository,
            IMatchRepository matchRepository,
            IProfileRepository profileRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _matchRepository = matchRepository;
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<Result<ReviewDTO>> GetAsync(int id)
        {
            try
            {
                var entity = await _reviewRepository.GetAsync(id);
                if (entity is null)
                    return new() { IsSuccess = false, Message = "Review not found" };

                var dto = _mapper.Map<ReviewDTO>(entity);
                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "Review retrieved"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving review: {ex.Message}"
                };
            }
        }

        public async Task<Result<List<ReviewDTO>>> GetByProfileAsync(int profileId)
        {
            try
            {
                var list = await _reviewRepository.GetByToProfileIdAsync(profileId);
                var mapped = _mapper.Map<List<ReviewDTO>>(list);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Reviews retrieved"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving reviews: {ex.Message}"
                };
            }
        }

        public async Task<Result<List<ReviewDTO>>> GetByMatchAsync(int matchId)
        {
            try
            {
                var list = await _reviewRepository.GetByMatchIdAsync(matchId);
                var mapped = _mapper.Map<List<ReviewDTO>>(list);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Reviews retrieved"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving reviews: {ex.Message}"
                };
            }
        }

        public async Task<Result<ReviewDTO>> AddAsync(ReviewDTO dto, int currentUserId)
        {
            try
            {
                if (dto.ToProfileId <= 0 || dto.MatchId <= 0)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "ToProfileId and MatchId are required"
                    };

                // Profil wystawiającego (z userId)
                var fromProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (fromProfile is null || fromProfile.IsDeleted)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile for current user not found"
                    };

                var toProfile = await _profileRepository.GetAsync(dto.ToProfileId);
                if (toProfile is null || toProfile.IsDeleted)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Target profile not found"
                    };

                // Match musi istnieć
                var match = await _matchRepository.GetAsync(dto.MatchId);
                if (match is null || match.IsDeleted)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Match not found"
                    };

                // Czy oba profile biorą udział w tym matchu?
                bool validPair =
                    match.Profile1Id == fromProfile.Id && match.Profile2Id == toProfile.Id ||
                    match.Profile2Id == fromProfile.Id && match.Profile1Id == toProfile.Id;

                if (!validPair)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profiles are not participants of this match"
                    };
                }

                // Czy już wystawiono review z tego profilu dla tego matcha?
                var existing = await _reviewRepository.GetByFromProfileAndMatchAsync(fromProfile.Id, dto.MatchId);
                if (existing != null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You have already reviewed this match"
                    };
                }

                // Tworzymy encję
                var entity = new Review
                {
                    FromProfileId = fromProfile.Id,
                    ToProfileId = dto.ToProfileId,
                    MatchId = dto.MatchId,
                    CooperationRating = dto.CooperationRating,
                    WorkQualityRating = dto.WorkQualityRating,
                    KnowledgeGainRating = dto.KnowledgeGainRating,
                    Comment = dto.Comment,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                entity = await _reviewRepository.AddAsync(entity);

                // Aktualizacja agregatów na User (użytkownik właściciel profilu ToProfile)
                var toUser = await _userRepository.GetAsync(toProfile.UserId);
                if (toUser != null && !toUser.IsDeleted)
                {
                    int oldCount = toUser.ReviewsCount;
                    int newCount = oldCount + 1;

                    toUser.AvgCooperationRating =
                        (toUser.AvgCooperationRating * oldCount + dto.CooperationRating) / newCount;

                    toUser.AvgWorkQualityRating =
                        (toUser.AvgWorkQualityRating * oldCount + dto.WorkQualityRating) / newCount;

                    toUser.AvgKnowledgeGainRating =
                        (toUser.AvgKnowledgeGainRating * oldCount + dto.KnowledgeGainRating) / newCount;

                    toUser.ReviewsCount = newCount;

                    await _userRepository.UpdateAsync(toUser);
                }

                var mapped = _mapper.Map<ReviewDTO>(entity);
                mapped.FromProfileId = fromProfile.Id;

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "Review created successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error creating review: {ex.Message}"
                };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var entity = await _reviewRepository.GetAsync(id);
                if (entity is null)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Review not found"
                    };

                var fromProfile = await _profileRepository.GetAsync(entity.FromProfileId);
                if (fromProfile is null)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };

                if (fromProfile.UserId != currentUserId)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You are not allowed to delete this review"
                    };

                await _reviewRepository.DeleteAsync(entity); // soft delete (IsDeleted = true)

                return new()
                {
                    IsSuccess = true,
                    Data = "Review deleted",
                    Message = "Review deleted"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error deleting review: {ex.Message}"
                };
            }
        }
    }
}
