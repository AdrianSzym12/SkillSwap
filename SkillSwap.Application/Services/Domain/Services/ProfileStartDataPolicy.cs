using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;

namespace SkillSwap.Domain.Services
{
    public static class ProfileStartDataPolicy
    {
        public static (double completion, List<string> missingFields, bool isComplete) Evaluate(Profile profile)
        {
            var required = new List<(string Field, bool Present)>
            {
                ("UserName", !string.IsNullOrWhiteSpace(profile.UserName)),
                ("Country",  !string.IsNullOrWhiteSpace(profile.Country)),
                ("Language", !string.IsNullOrWhiteSpace(profile.Language)),
                ("PreferredMeetingType", profile.PreferredMeetingType != MeetingType.None),
                ("PreferredLearningStyle", profile.PreferredLearningStyle != LearningStyle.None),
                ("Availability", profile.Availability != AvailabilitySlot.None)
            };

            var missing = required.Where(x => !x.Present).Select(x => x.Field).ToList();
            var completion = required.Count == 0 ? 1.0 : (required.Count - missing.Count) / (double)required.Count;

            return (completion, missing, missing.Count == 0);
        }
    }
}
