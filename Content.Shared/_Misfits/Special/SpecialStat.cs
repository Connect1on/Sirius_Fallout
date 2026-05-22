using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Special;

[Serializable, NetSerializable]
public enum SpecialStat : byte
{
    Strength,
    Perception,
    Endurance,
    Charisma,
    Intelligence,
    Agility,
    Luck,
}

public static class SpecialStats
{
    public static readonly SpecialStat[] All =
    {
        SpecialStat.Strength,
        SpecialStat.Perception,
        SpecialStat.Endurance,
        SpecialStat.Charisma,
        SpecialStat.Intelligence,
        SpecialStat.Agility,
        SpecialStat.Luck,
    };

    public static bool IsEnabled(SpecialStat stat)
    {
        return stat is SpecialStat.Strength
            or SpecialStat.Perception
            or SpecialStat.Endurance
            or SpecialStat.Charisma
            or SpecialStat.Intelligence
            or SpecialStat.Agility
            or SpecialStat.Luck;
    }
}
