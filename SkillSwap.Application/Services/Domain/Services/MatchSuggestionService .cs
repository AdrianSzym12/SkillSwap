using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using ProfileEntity = SkillSwap.Domain.Entities.Database.Profile;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class MatchSuggestionService : IMatchSuggestion
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IUserSkillRepository _userSkillRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IMapper _mapper;

        public MatchSuggestionService(
            IProfileRepository profileRepository,
            IUserSkillRepository userSkillRepository,
            IUserRepository userRepository,
            IMatchRepository matchRepository,
            IMapper mapper)
        {
            _profileRepository = profileRepository;
            _userSkillRepository = userSkillRepository;
            _userRepository = userRepository;
            _matchRepository = matchRepository;
            _mapper = mapper;
        }

        public async Task<Result<List<MatchSuggestionDTO>>> GetSuggestionsAsync(int currentUserId, int limit = 20)
        {
            try
            {
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (myProfile is null || myProfile.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile for current user not found"
                    };
                }

                var allProfiles = await _profileRepository.GetAsync();
                var candidates = allProfiles
                    .Where(p => !p.IsDeleted && p.Id != myProfile.Id)
                    .ToList();

                var excludedPartners = await _matchRepository.GetPartnerProfileIdsAsync(myProfile.Id);
                candidates = candidates
                    .Where(p => !excludedPartners.Contains(p.Id))
                    .ToList();

                if (candidates.Count == 0)
                {
                    return new()
                    {
                        IsSuccess = true,
                        Data = new List<MatchSuggestionDTO>(),
                        Message = "No candidates"
                    };
                }

                var profileIds = candidates.Select(p => p.Id).ToList();
                profileIds.Add(myProfile.Id);

                var allSkills = await _userSkillRepository.GetByProfileIdsAsync(profileIds);

                var skillsByProfile = allSkills
                    .GroupBy(s => s.ProfileId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                skillsByProfile.TryGetValue(myProfile.Id, out var mySkills);
                mySkills ??= new List<UserSkill>();

                if (mySkills.Count == 0)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not ready for matching. Add at least 1 skill."
                    };
                }

                var myTeach = mySkills.Where(s => s.Learned).ToList();
                var myLearn = mySkills.Where(s => !s.Learned).ToList();

                if (myTeach.Count == 0 && myLearn.Count == 0)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not ready for matching. Add at least 1 skill to teach or learn."
                    };
                }

                if (myLearn.Count == 0)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile not ready for matching. Add at least 1 skill you want to learn."
                    };
                }

                var myTeachIds = myTeach.Select(s => s.SkillId).ToHashSet();
                var myLearnIds = myLearn.Select(s => s.SkillId).ToHashSet();

                var userIds = candidates.Select(p => p.UserId).Distinct().ToList();
                userIds.Add(myProfile.UserId);

                var users = await _userRepository.GetByIdsAsync(userIds);
                var usersById = users.ToDictionary(u => u.Id, u => u);

                var suggestions = new List<MatchSuggestionDTO>(candidates.Count);

                foreach (var profile in candidates)
                {
                    if (!skillsByProfile.TryGetValue(profile.Id, out var otherSkills) || otherSkills.Count == 0)
                        continue;

                    var otherTeach = otherSkills.Where(s => s.Learned).ToList();
                    var otherLearn = otherSkills.Where(s => !s.Learned).ToList();

                    var otherTeachIds = otherTeach.Select(s => s.SkillId).ToHashSet();
                    var otherLearnIds = otherLearn.Select(s => s.SkillId).ToHashSet();

                    var teachMeCount = myLearnIds.Intersect(otherTeachIds).Count();
                    var teachThemCount = myTeachIds.Intersect(otherLearnIds).Count();

                    if (teachMeCount == 0 && teachThemCount == 0)
                        continue;

                    double skillFitScore = CalculateSkillFitScore(teachMeCount, teachThemCount);
                    double levelFitScore = ComputeLevelFitScore(myTeach, myLearn, otherTeach, otherLearn);

                    double meetingTypeScore = ComputeMeetingTypeScore(myProfile, profile);
                    double learningStyleScore = ComputeLearningStyleScore(myProfile, profile);

                    double distanceScore = ComputeDistanceScore(myProfile, profile);
                    double availabilityScore = ComputeAvailabilityScore(myProfile, profile);

                    double preferenceScore = 0.6 * meetingTypeScore + 0.4 * learningStyleScore;

                    usersById.TryGetValue(profile.UserId, out var otherUser);

                    double opinionFactor = ComputeOpinionFactor(otherUser);
                    double profileFactor = ComputeProfileFactor(profile);
                    double newUserBoost = (otherUser?.ReviewsCount ?? 0) == 0 ? 1.05 : 1.0;

                    double baseScore =
                        skillFitScore * 0.4 +
                        levelFitScore * 0.2 +
                        availabilityScore * 0.2 +
                        preferenceScore * 0.1 +
                        distanceScore * 0.1;

                    double matchScore = baseScore * opinionFactor * profileFactor * newUserBoost;

                    suggestions.Add(new MatchSuggestionDTO
                    {
                        ProfileId = profile.Id,
                        Profile = _mapper.Map<ProfileDTO>(profile),

                        MatchScore = matchScore,

                        SkillFitScore = skillFitScore,
                        LevelFitScore = levelFitScore,
                        AvailabilityScore = availabilityScore,
                        PreferenceScore = preferenceScore,
                        DistanceScore = distanceScore,

                        OpinionFactor = opinionFactor,
                        ProfileFactor = profileFactor,
                        NewUserBoost = newUserBoost
                    });
                }

                var ordered = suggestions
                    .OrderByDescending(s => s.MatchScore)
                    .Take(limit)
                    .ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = ordered,
                    Message = "Match suggestions calculated"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error calculating match suggestions: {ex.Message}"
                };
            }
        }


        private static double CalculateSkillFitScore(int teachMeCount, int teachThemCount)
        {
            double score = 0.0;
            if (teachMeCount > 0) score += 0.6;
            if (teachThemCount > 0) score += 0.4;
            return score;
        }

        private static double ComputeOpinionFactor(User? user)
        {
            if (user == null || user.ReviewsCount == 0)
                return 1.0;

            double teachingScore =
                user.AvgKnowledgeGainRating * 0.5 +
                user.AvgCooperationRating * 0.3 +
                user.AvgWorkQualityRating * 0.2;

            double normalized = (teachingScore - 1.0) / 4.0;
            normalized = Math.Clamp(normalized, 0.0, 1.0);

            return 0.6 + normalized * 0.5; 
        }

        private static double ComputeProfileFactor(ProfileEntity profile)
        {
            int total = 4;
            int filled = 0;

            if (!string.IsNullOrWhiteSpace(profile.UserName)) filled++;
            if (!string.IsNullOrWhiteSpace(profile.Bio)) filled++;
            if (profile.Avatar != null && profile.Avatar.Length > 0) filled++;
            if (!string.IsNullOrWhiteSpace(profile.Country)) filled++;

            double completion = total > 0 ? (double)filled / total : 0.0;
            return 0.9 + 0.2 * completion; 
        }

        private static int LevelToInt(SkillLevel level) => level switch
        {
            SkillLevel.Beginner => 1,
            SkillLevel.Intermediate => 2,
            SkillLevel.Advanced => 3,
            SkillLevel.Expert => 4,
            _ => 1
        };

        private static double ComputeLevelFitScore(
            List<UserSkill> myTeach,
            List<UserSkill> myLearn,
            List<UserSkill> otherTeach,
            List<UserSkill> otherLearn)
        {
            var scoreParts = new List<double>();

            var teachMePairs = from mine in myLearn
                               join theirs in otherTeach on mine.SkillId equals theirs.SkillId
                               select new { my = mine, other = theirs };

            foreach (var p in teachMePairs)
            {
                int myLvl = LevelToInt(p.my.Level);
                int otherLvl = LevelToInt(p.other.Level);
                int delta = otherLvl - myLvl;

                scoreParts.Add(delta switch
                {
                    >= 2 => 1.0,
                    1 => 0.9,
                    0 => 0.6,
                    -1 => 0.3,
                    _ => 0.1
                });
            }

            var teachThemPairs = from mine in myTeach
                                 join theirs in otherLearn on mine.SkillId equals theirs.SkillId
                                 select new { my = mine, other = theirs };

            foreach (var p in teachThemPairs)
            {
                int myLvl = LevelToInt(p.my.Level);
                int otherLvl = LevelToInt(p.other.Level);
                int delta = myLvl - otherLvl;

                scoreParts.Add(delta switch
                {
                    >= 2 => 1.0,
                    1 => 0.9,
                    0 => 0.6,
                    -1 => 0.3,
                    _ => 0.1
                });
            }

            return scoreParts.Any() ? scoreParts.Average() : 0.0;
        }

        private static double ComputeMeetingTypeScore(ProfileEntity mine, ProfileEntity other)
        {
            var a = mine.PreferredMeetingType;
            var b = other.PreferredMeetingType;

            if (a == b) return 1.0;
            if (a == MeetingType.Hybrid || b == MeetingType.Hybrid) return 0.8;
            return 0.3;
        }

        private static double ComputeLearningStyleScore(ProfileEntity mine, ProfileEntity other)
            => mine.PreferredLearningStyle == other.PreferredLearningStyle ? 1.0 : 0.5;

        private static double ComputeDistanceScore(ProfileEntity mine, ProfileEntity other)
        {
            if (mine.PreferredMeetingType == MeetingType.Online || other.PreferredMeetingType == MeetingType.Online)
                return 1.0;

            if (mine.PreferredMeetingType == MeetingType.Hybrid || other.PreferredMeetingType == MeetingType.Hybrid)
            {
                if (!string.IsNullOrWhiteSpace(mine.Country) &&
                    !string.IsNullOrWhiteSpace(other.Country) &&
                    !string.Equals(mine.Country, other.Country, StringComparison.OrdinalIgnoreCase))
                    return 0.6;

                return 0.9;
            }

            if (!string.IsNullOrWhiteSpace(mine.Country) && !string.IsNullOrWhiteSpace(other.Country))
                return string.Equals(mine.Country, other.Country, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.4;

            return 0.8;
        }

        private static int CountBits(int x)
        {
            int count = 0;
            while (x != 0)
            {
                x &= (x - 1);
                count++;
            }
            return count;
        }

        private static double ComputeAvailabilityScore(ProfileEntity mine, ProfileEntity other)
        {
            int myMask = (int)mine.Availability;
            int otherMask = (int)other.Availability;

            if (myMask == 0 || otherMask == 0) return 0.7;

            int overlap = CountBits(myMask & otherMask);
            int mySlots = CountBits(myMask);
            if (mySlots == 0) return 0.7;

            double ratio = (double)overlap / mySlots;

            if (ratio >= 0.8) return 1.0;
            if (ratio >= 0.5) return 0.9;
            if (ratio >= 0.3) return 0.7;
            if (ratio > 0.0) return 0.4;
            return 0.1;
        }
    }
}
