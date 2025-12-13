using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces.ExternalInterfaces;
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

                // moje skille
                var mySkills = await _userSkillRepository.GetByProfileIdAsync(myProfile.Id);
                var myTeach = mySkills.Where(s => s.Learned).ToList();
                var myLearn = mySkills.Where(s => !s.Learned).ToList();

                var myTeachIds = myTeach.Select(s => s.SkillId).ToHashSet();
                var myLearnIds = myLearn.Select(s => s.SkillId).ToHashSet();

                // wszystkie profile (bez usuniętych)
                var allProfiles = await _profileRepository.GetAsync();

                // istniejące matche – nie proponujemy kogoś, z kim już masz match
                var allMatches = await _matchRepository.GetAsync();
                var myMatchPartners = allMatches
                    .Where(m => m.Profile1Id == myProfile.Id || m.Profile2Id == myProfile.Id)
                    .Select(m => m.Profile1Id == myProfile.Id ? m.Profile2Id : m.Profile1Id)
                    .ToHashSet();

                var suggestions = new List<MatchSuggestionDTO>();

                foreach (var profile in allProfiles)
                {
                    if (profile.Id == myProfile.Id)
                        continue;

                    if (profile.IsDeleted)
                        continue;

                    if (myMatchPartners.Contains(profile.Id))
                        continue;

                    // skille drugiego profilu
                    var otherSkills = await _userSkillRepository.GetByProfileIdAsync(profile.Id);
                    if (!otherSkills.Any())
                        continue;

                    var otherTeach = otherSkills.Where(s => s.Learned).ToList();
                    var otherLearn = otherSkills.Where(s => !s.Learned).ToList();

                    var otherTeachIds = otherTeach.Select(s => s.SkillId).ToHashSet();
                    var otherLearnIds = otherLearn.Select(s => s.SkillId).ToHashSet();

                    // Czy on może mnie uczyć?
                    var teachMeCount = myLearnIds.Intersect(otherTeachIds).Count();
                    // Czy ja mogę uczyć jego?
                    var teachThemCount = myTeachIds.Intersect(otherLearnIds).Count();

                    if (teachMeCount == 0 && teachThemCount == 0)
                        continue; // brak sensownej wymiany – skip

                    // dopasowanie skilli (czy w ogóle możemy się wymieniać)
                    double skillFitScore = CalculateSkillFitScore(teachMeCount, teachThemCount);

                    // nowość: dopasowanie poziomów (BEGINNER/ADVANCED itd.)
                    double levelFitScore = ComputeLevelFitScore(
                        myTeach, myLearn,
                        otherTeach, otherLearn);

                    // preferencje spotkań i stylu nauki
                    double meetingTypeScore = ComputeMeetingTypeScore(myProfile, profile);
                    double learningStyleScore = ComputeLearningStyleScore(myProfile, profile);

                    // prosty distanceScore na bazie Country + meeting type
                    double distanceScore = ComputeDistanceScore(myProfile, profile, meetingTypeScore);

                    // availability – na razie 1.0, można rozbudować później
                    double availabilityScore = 1.0;

                    // scalanie preferencji (typ spotkań + styl nauki) w jeden score 0–1
                    double preferenceScore = 0.6 * meetingTypeScore + 0.4 * learningStyleScore;
                    var otherUser = await _userRepository.GetAsync(profile.UserId);
                    // opinie i profil jak wcześniej
                    double opinionFactor = ComputeOpinionFactor(otherUser);
                    double profileFactor = ComputeProfileFactor(profile);
                    double newUserBoost = (otherUser?.ReviewsCount ?? 0) == 0 ? 1.05 : 1.0;

                    // wzór z Twojego opisu:
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
                        OpinionFactor = opinionFactor,
                        ProfileFactor = profileFactor
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

        private double CalculateSkillFitScore(int teachMeCount, int teachThemCount)
        {
            // można potem rozbudować, na razie prosto:
            // 0.6 za to, że on umie coś, czego ja chcę się uczyć
            // 0.4 za to, że ja umiem coś, czego on chce się uczyć
            double score = 0.0;

            if (teachMeCount > 0)
                score += 0.6;

            if (teachThemCount > 0)
                score += 0.4;

            return score; // 0, 0.6, 0.4, 1.0
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

        private double ComputeDistanceScore(ProfileEntity mine, ProfileEntity other, double meetingTypeScore)
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
    }
}
