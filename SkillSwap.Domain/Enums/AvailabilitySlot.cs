
namespace SkillSwap.Domain.Enums
{
    [Flags]
    public enum AvailabilitySlot
    {
        None = 0,

        MonMorning = 1 << 0,
        MonAfternoon = 1 << 1,
        MonEvening = 1 << 2,

        TueMorning = 1 << 3,
        TueAfternoon = 1 << 4,
        TueEvening = 1 << 5,

        WedMorning = 1 << 6,
        WedAfternoon = 1 << 7,
        WedEvening = 1 << 8,

        ThuMorning = 1 << 9,
        ThuAfternoon = 1 << 10,
        ThuEvening = 1 << 11,

        FriMorning = 1 << 12,
        FriAfternoon = 1 << 13,
        FriEvening = 1 << 14,

        SatMorning = 1 << 15,
        SatAfternoon = 1 << 16,
        SatEvening = 1 << 17,

        SunMorning = 1 << 18,
        SunAfternoon = 1 << 19,
        SunEvening = 1 << 20
    }
}
