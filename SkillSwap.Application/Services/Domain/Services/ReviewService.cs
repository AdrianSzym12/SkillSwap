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
        private const string ProfileNotFound = "Profile for current user not found";
        private const string MatchNotFound = "Match not found";
        private const string NotParticipant = "You are not a participant of this match";
        private const string AlreadyReviewed = "You have already reviewed this match";
        private const string ReviewNotFound = "Review not found";

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
                if (entity is null || entity.IsDeleted)
                    return new() { IsSuccess = false, Message = ReviewNotFound };

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ReviewDTO>(entity),
                    Message = "Review retrieved"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving review: {ex.Message}" };
            }
        }

        public async Task<Result<List<ReviewDTO>>> GetByProfileAsync(int profileId)
        {
            try
            {
                var list = await _reviewRepository.GetByToProfileIdAsync(profileId);
                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ReviewDTO>>(list),
                    Message = "Reviews retrieved"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving reviews: {ex.Message}" };
            }
        }

        public async Task<Result<List<ReviewDTO>>> GetByMatchAsync(int matchId)
        {
            try
            {
                var list = await _reviewRepository.GetByMatchIdAsync(matchId);
                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ReviewDTO>>(list),
                    Message = "Reviews retrieved"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving reviews: {ex.Message}" };
            }
        }

        public async Task<Result<List<ReviewDTO>>> GetMyGivenAsync(int currentUserId)
        {
            try
            {
                var fromProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (fromProfile is null || fromProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = ProfileNotFound };

                var list = await _reviewRepository.GetByFromProfileIdAsync(fromProfile.Id);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ReviewDTO>>(list),
                    Message = "Reviews retrieved"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving reviews: {ex.Message}" };
            }
        }

        public async Task<Result<List<ReviewDTO>>> GetMyReceivedAsync(int currentUserId)
        {
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (myProfile is null || myProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = ProfileNotFound };

                var list = await _reviewRepository.GetByToProfileIdAsync(myProfile.Id);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ReviewDTO>>(list),
                    Message = "Reviews retrieved"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving reviews: {ex.Message}" };
            }
        }

        public async Task<Result<ReviewDTO>> AddAsync(ReviewCreateDTO dto, int currentUserId)
        {
            try
            {
                var fromProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (fromProfile is null || fromProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = ProfileNotFound };

                var match = await _matchRepository.GetAsync(dto.MatchId);
                if (match is null || match.IsDeleted)
                    return new() { IsSuccess = false, Message = MatchNotFound };

                var isParticipant = match.Profile1Id == fromProfile.Id || match.Profile2Id == fromProfile.Id;
                if (!isParticipant)
                    return new() { IsSuccess = false, Message = NotParticipant };

                var toProfileId = match.Profile1Id == fromProfile.Id ? match.Profile2Id : match.Profile1Id;

                var existing = await _reviewRepository.GetByFromProfileAndMatchAsync(fromProfile.Id, dto.MatchId);
                if (existing is not null)
                    return new() { IsSuccess = false, Message = AlreadyReviewed };

                var entity = new Review
                {
                    FromProfileId = fromProfile.Id,
                    ToProfileId = toProfileId,
                    MatchId = dto.MatchId,

                    CooperationRating = dto.CooperationRating,
                    WorkQualityRating = dto.WorkQualityRating,
                    KnowledgeGainRating = dto.KnowledgeGainRating,

                    Comment = dto.Comment,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                entity = await _reviewRepository.AddAsync(entity);

                await RecalculateUserAggregatesAsync(toProfileId);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ReviewDTO>(entity),
                    Message = "Review created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating review: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var entity = await _reviewRepository.GetAsync(id);
                if (entity is null || entity.IsDeleted)
                    return new() { IsSuccess = false, Message = ReviewNotFound };

                var fromProfile = await _profileRepository.GetAsync(entity.FromProfileId);
                if (fromProfile is null || fromProfile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                if (fromProfile.UserId != currentUserId)
                    return new() { IsSuccess = false, Message = "You are not allowed to delete this review" };

                var toProfileId = entity.ToProfileId;

                await _reviewRepository.DeleteAsync(entity); 

                await RecalculateUserAggregatesAsync(toProfileId);

                return new()
                {
                    IsSuccess = true,
                    Data = "Review deleted",
                    Message = "Review deleted"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error deleting review: {ex.Message}" };
            }
        }

        private async Task RecalculateUserAggregatesAsync(int toProfileId)
        {
            var toProfile = await _profileRepository.GetAsync(toProfileId);
            if (toProfile is null || toProfile.IsDeleted)
                return;

            var toUser = await _userRepository.GetAsync(toProfile.UserId);
            if (toUser is null || toUser.IsDeleted)
                return;

            var reviews = await _reviewRepository.GetByToProfileIdAsync(toProfileId); 

            toUser.ReviewsCount = reviews.Count;

            if (reviews.Count == 0)
            {
                toUser.AvgCooperationRating = 0;
                toUser.AvgWorkQualityRating = 0;
                toUser.AvgKnowledgeGainRating = 0;
            }
            else
            {
                toUser.AvgCooperationRating = reviews.Average(r => r.CooperationRating);
                toUser.AvgWorkQualityRating = reviews.Average(r => r.WorkQualityRating);
                toUser.AvgKnowledgeGainRating = reviews.Average(r => r.KnowledgeGainRating);
            }

            await _userRepository.UpdateAsync(toUser);
        }
    }
}
