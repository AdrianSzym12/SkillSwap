using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces.ExternalInterfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Domain.Interfaces;
using ProfileEntity = SkillSwap.Domain.Entities.Database.Profile;


namespace SkillSwap.Infrastructure.MatchLogic
{
    public class MatchSuggestion : IMatchSuggestion
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IUserSkillRepository _userSkillRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IMatchSwipeRepository _matchSwipeRepository;
        private readonly ISkillRepository _skillRepository;
        private readonly IMapper _mapper;

        public MatchSuggestion(
            IProfileRepository profileRepository,
            IUserSkillRepository userSkillRepository,
            IUserRepository userRepository,
            IMatchRepository matchRepository,
            IMatchSwipeRepository matchSwipeRepository,
            ISkillRepository skillRepository,
            IMapper mapper)
        {
            _profileRepository = profileRepository;
            _userSkillRepository = userSkillRepository;
            _userRepository = userRepository;
            _matchRepository = matchRepository;
            _matchSwipeRepository = matchSwipeRepository;
            _skillRepository = skillRepository;
            _mapper = mapper;
        }

        public async Task<Result<List<MatchSuggestionDTO>>> GetSuggestionsAsync(int currentUserId, int limit = 20, CancellationToken ct = default)
        {
            try
            {
                // ===== Walidacja parametrów =====
                if (limit < 1 || limit > 50)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Limit must be between 1 and 50",
                        StatusCode = 400
                    };
                }

                // ===== Profil użytkownika =====
                var myProfile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (myProfile is null || myProfile.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Profile for current user not found",
                        StatusCode = 404
                    };
                }

                // ===== Moje skille (uczę / chcę się nauczyć) =====
                var mySkills = await _userSkillRepository.GetByProfileIdAsync(myProfile.Id, ct);
                var myTeach = mySkills.Where(s => s.Learned).ToList();      // czego JA uczę
                var myLearn = mySkills.Where(s => !s.Learned).ToList();     // czego JA chcę się uczyć

                var myTeachIds = myTeach.Select(s => s.SkillId).ToHashSet();
                var myLearnIds = myLearn.Select(s => s.SkillId).ToHashSet();

                // ===== Kandydaci (filtry: nie ja, nie usunięty, onboarding, bez matchy, bez swipe) =====
                var allProfiles = await _profileRepository.GetAsync(ct);   // wszystkie profile

                var myMatches = await _matchRepository.GetByProfileIdAsync(myProfile.Id, ct);
                var myMatchPartners = myMatches
                    .Select(m => m.Profile1Id == myProfile.Id ? m.Profile2Id : m.Profile1Id)
                    .ToHashSet();

                var swipedProfileIds = (await _matchSwipeRepository.GetSwipedToProfileIdsAsync(myProfile.Id, ct)).ToHashSet();

                var candidateProfiles = allProfiles
                    .Where(p => p.Id != myProfile.Id)
                    .Where(p => !p.IsDeleted)
                    .Where(p => p.IsOnboardingComplete)
                    .Where(p => !myMatchPartners.Contains(p.Id))
                    .Where(p => !swipedProfileIds.Contains(p.Id))
                    .ToList();

                var candidateProfileIds = candidateProfiles.Select(p => p.Id).ToList();
                var candidatesSkills = await _userSkillRepository.GetByProfileIdsAsync(candidateProfileIds, ct);
                var skillsByProfile = candidatesSkills
                    .GroupBy(s => s.ProfileId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var candidateUserIds = candidateProfiles.Select(p => p.UserId).Distinct().ToList();
                var users = await _userRepository.GetByIdsAsync(candidateUserIds, ct);

                var suggestions = new List<MatchSuggestionDTO>();

                // ===== Liczenie score dla każdego kandydata =====
                foreach (var profile in candidateProfiles)
                {
                    if (!skillsByProfile.TryGetValue(profile.Id, out var otherSkills) || otherSkills is null || !otherSkills.Any())
                        continue;

                    var otherTeach = otherSkills.Where(s => s.Learned).ToList();      // on uczy
                    var otherLearn = otherSkills.Where(s => !s.Learned).ToList();     // on chce się uczyć
                    var otherTeachIds = otherTeach.Select(s => s.SkillId).ToHashSet();
                    var otherLearnIds = otherLearn.Select(s => s.SkillId).ToHashSet();

                    var teachMeSkills = myLearnIds.Intersect(otherTeachIds).ToList();   // on może mnie uczyć
                    var teachThemSkills = myTeachIds.Intersect(otherLearnIds).ToList();   // ja mogę uczyć jego

                    var teachMeCount = teachMeSkills.Count;
                    var teachThemCount = teachThemSkills.Count;

                    if (teachMeCount == 0 && teachThemCount == 0)
                        continue; // brak realnej wymiany → skip

                    double skillFitScore = CalculateSkillFitScore(teachMeCount, teachThemCount);
                    double levelFitScore = ComputeLevelFitScore(myTeach, myLearn, otherTeach, otherLearn);
                    double meetingTypeScore = ComputeMeetingTypeScore(myProfile, profile);
                    double learningStyleScore = ComputeLearningStyleScore(myProfile, profile);
                    double distanceScore = ComputeDistanceScore(myProfile, profile);
                    double availabilityScore = ComputeAvailabilityScore(myProfile, profile);

                    double preferenceScore = 0.6 * meetingTypeScore + 0.4 * learningStyleScore;

                    var otherUser = users.FirstOrDefault(u => u.Id == profile.UserId);

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
                        NewUserBoost = newUserBoost,
                        TeachMeCount = teachMeCount,
                        TeachThemCount = teachThemCount,
                        SkillsTheyCanTeachMe = teachMeSkills,
                        SkillsICanTeachThem = teachThemSkills
                    });
                }

                // ===== Sortowanie i limit =====
                var ordered = suggestions
                    .OrderByDescending(s => s.MatchScore)
                    .Take(limit)
                    .ToList();

                // ===== Optional enrichment: attach skill names for quick UI rendering =====
                var allSkillIds = ordered
                    .SelectMany(s => s.SkillsTheyCanTeachMe.Concat(s.SkillsICanTeachThem))
                    .Distinct()
                    .ToList();

                if (allSkillIds.Count > 0)
                {
                    var skillEntities = await _skillRepository.GetByIdsAsync(allSkillIds, ct);
                    var skillMap = skillEntities.ToDictionary(
                        s => s.Id,
                        s => new SkillPreviewDTO { Id = s.Id, Name = s.Name, Category = s.Category });

                    foreach (var s in ordered)
                    {
                        s.SkillsTheyCanTeachMeDetails = s.SkillsTheyCanTeachMe
                            .Where(id => skillMap.ContainsKey(id))
                            .Select(id => skillMap[id])
                            .ToList();

                        s.SkillsICanTeachThemDetails = s.SkillsICanTeachThem
                            .Where(id => skillMap.ContainsKey(id))
                            .Select(id => skillMap[id])
                            .ToList();
                    }
                }

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


        // ===== Składowe algorytmu (score) =====

        private double CalculateSkillFitScore(int teachMeCount, int teachThemCount)
        {
            // można potem rozbudować, na razie prosto:
            // 0..1 na podstawie liczby skilli, z lekkim bonusem za wzajemność
            double a = CountToScore(teachMeCount);
            double b = CountToScore(teachThemCount);
            double mutual = (teachMeCount > 0 && teachThemCount > 0) ? 0.15 : 0.0;

            double score = 0.55 * a + 0.45 * b + mutual;
            return Math.Clamp(score, 0.0, 1.0);
        }

        private double CountToScore(int count)
        {
            if (count <= 0)
                return 0.0;

            return 1.0 - Math.Exp(-count / 2.5);
        }

        private double ComputeOpinionFactor(User? user)
        {
            // brak usera / brak opinii -> neutralnie
            if (user == null || user.ReviewsCount == 0)
                return 1.0;

            // teachingScore = knowledge * 0.5 + cooperation * 0.3 + work * 0.2
            double teachingScore =
                user.AvgKnowledgeGainRating * 0.5 +
                user.AvgCooperationRating * 0.3 +
                user.AvgWorkQualityRating * 0.2;

            // rating 1..5 przemapujemy na factor 0.6..1.1
            // 1 -> 0.6, 5 -> 1.1
            double normalized = (teachingScore - 1.0) / 4.0; // 0..1
            if (normalized < 0) normalized = 0;
            if (normalized > 1) normalized = 1;

            double factor = 0.6 + normalized * 0.5; // 0.6..1.1
            return factor;
        }

        private double ComputeProfileFactor(ProfileEntity profile)
        {
            int total = 4;
            int filled = 0;

            if (!string.IsNullOrWhiteSpace(profile.UserName))
                filled++;
            if (!string.IsNullOrWhiteSpace(profile.Bio))
                filled++;
            if (profile.Avatar != null && profile.Avatar.Length > 0)
                filled++;
            if (!string.IsNullOrWhiteSpace(profile.Country))
                filled++;

            double completion = total > 0 ? (double)filled / total : 0.0;

            // 0.9 .. 1.1
            return 0.9 + 0.2 * completion;
        }
        private int LevelToInt(SkillLevel level) => level switch
        {
            SkillLevel.Beginner => 1,
            SkillLevel.Intermediate => 2,
            SkillLevel.Advanced => 3,
            SkillLevel.Expert => 4,
            _ => 1
        };

        private double ComputeLevelFitScore(
            List<UserSkill> myTeach,
            List<UserSkill> myLearn,
            List<UserSkill> otherTeach,
            List<UserSkill> otherLearn)
        {
            // nauczyciel powinien mieć >= poziom ucznia

            var scoreParts = new List<double>();

            // 1) on uczy mnie (ja Learn, on Teach)
            var teachMePairs = from mine in myLearn
                               join theirs in otherTeach on mine.SkillId equals theirs.SkillId
                               select new { my = mine, other = theirs };

            foreach (var p in teachMePairs)
            {
                int myLvl = LevelToInt(p.my.Level);
                int otherLvl = LevelToInt(p.other.Level);
                int delta = otherLvl - myLvl;

                double s = delta switch
                {
                    >= 2 => 1.0,   // dużo lepszy
                    1 => 0.9,   // trochę lepszy
                    0 => 0.6,   // na podobnym poziomie
                    -1 => 0.3,   // trochę słabszy
                    _ => 0.1    // dużo słabszy
                };

                scoreParts.Add(s);
            }

            // 2) ja uczę jego (ja Teach, on Learn)
            var teachThemPairs = from mine in myTeach
                                 join theirs in otherLearn on mine.SkillId equals theirs.SkillId
                                 select new { my = mine, other = theirs };

            foreach (var p in teachThemPairs)
            {
                int myLvl = LevelToInt(p.my.Level);
                int otherLvl = LevelToInt(p.other.Level);
                int delta = myLvl - otherLvl; // ja nauczyciel

                double s = delta switch
                {
                    >= 2 => 1.0,
                    1 => 0.9,
                    0 => 0.6,
                    -1 => 0.3,
                    _ => 0.1
                };

                scoreParts.Add(s);
            }

            if (!scoreParts.Any())
                return 0.0;

            return scoreParts.Average(); // 0..1
        }

        private double ComputeMeetingTypeScore(ProfileEntity mine, ProfileEntity other)
        {
            var a = mine.PreferredMeetingType;
            var b = other.PreferredMeetingType;

            if (a == b)
                return 1.0;

            // jeden Hybrydowy, drugi Online/Offline → da się dogadać
            if (a == MeetingType.Hybrid || b == MeetingType.Hybrid)
                return 0.8;

            // jeden chce tylko Online, drugi tylko Offline
            return 0.3;
        }

        private double ComputeLearningStyleScore(ProfileEntity mine, ProfileEntity other)
        {
            if (mine.PreferredLearningStyle == other.PreferredLearningStyle)
                return 1.0;

            // różne style, ale nie tragedia
            return 0.5;
        }

        private double ComputeDistanceScore(ProfileEntity mine, ProfileEntity other)
        {
            // jeżeli ktoś dopuszcza Online/Hybrid → miejsce mniej ważne
            if (mine.PreferredMeetingType == MeetingType.Online ||
                other.PreferredMeetingType == MeetingType.Online)
                return 1.0;

            if (mine.PreferredMeetingType == MeetingType.Hybrid ||
                other.PreferredMeetingType == MeetingType.Hybrid)
            {
                // Hybrid + inny kraj → gorzej, ale ok
                if (!string.IsNullOrWhiteSpace(mine.Country) &&
                    !string.IsNullOrWhiteSpace(other.Country) &&
                    !string.Equals(mine.Country, other.Country, StringComparison.OrdinalIgnoreCase))
                {
                    return 0.6;
                }
                return 0.9;
            }

            // obaj chcą Offline:
            if (!string.IsNullOrWhiteSpace(mine.Country) &&
                !string.IsNullOrWhiteSpace(other.Country))
            {
                if (string.Equals(mine.Country, other.Country, StringComparison.OrdinalIgnoreCase))
                    return 1.0;
                else
                    return 0.4;
            }

            // brak danych → neutralnie
            return 0.8;
        }
        private int CountBits(int x)
        {
            int count = 0;
            while (x != 0)
            {
                x &= (x - 1);
                count++;
            }
            return count;
        }

        private double ComputeAvailabilityScore(ProfileEntity mine, ProfileEntity other)
        {
            int myMask = (int)mine.Availability;
            int otherMask = (int)other.Availability;

            if (myMask == 0 || otherMask == 0)
            {
                // brak danych → neutralnie, żeby nie karać
                return 0.7;
            }

            int overlap = CountBits(myMask & otherMask);
            int mySlots = CountBits(myMask);

            if (mySlots == 0)
                return 0.7;

            double ratio = (double)overlap / mySlots; // 0..1

            if (ratio >= 0.8) return 1.0;
            if (ratio >= 0.5) return 0.9;
            if (ratio >= 0.3) return 0.7;
            if (ratio > 0.0) return 0.4;
            return 0.1; 
        }

    }
}
