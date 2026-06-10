using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Nuclear14.AutodocSirius;

[Serializable, NetSerializable]
public sealed class AutodocBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool IsOpen;
    public bool Powered;
    public bool HasOccupant;
    public bool IsTreating;
    public OccupantStatus OccStatus;
    public Dictionary<string, FixedPoint2> OccupantDamage;
    public string? OccupantName;
    public bool HasBeaker;
    public FixedPoint2 BeakerCurrentVolume;
    public FixedPoint2 BeakerMaxVolume;
    public FixedPoint2 BeakerStimulantsAmount;
    public string? ErrorMessage;
    public bool TreatButtonEnabled;

    public AutodocBoundUserInterfaceState(
        bool isOpen,
        bool powered,
        bool hasOccupant,
        bool isTreating,
        OccupantStatus occStatus,
        Dictionary<string, FixedPoint2> occupantDamage,
        string? occupantName,
        bool hasBeaker,
        FixedPoint2 beakerCurrentVolume,
        FixedPoint2 beakerMaxVolume,
        FixedPoint2 beakerStimulantsAmount,
        string? errorMessage,
        bool treatButtonEnabled)
    {
        IsOpen = isOpen;
        Powered = powered;
        HasOccupant = hasOccupant;
        IsTreating = isTreating;
        OccStatus = occStatus;
        OccupantDamage = occupantDamage;
        OccupantName = occupantName;
        HasBeaker = hasBeaker;
        BeakerCurrentVolume = beakerCurrentVolume;
        BeakerMaxVolume = beakerMaxVolume;
        BeakerStimulantsAmount = beakerStimulantsAmount;
        ErrorMessage = errorMessage;
        TreatButtonEnabled = treatButtonEnabled;
    }
}

[Serializable, NetSerializable]
public enum OccupantStatus : byte
{
    None,
    Alive,
    Critical,
    Dead
}
