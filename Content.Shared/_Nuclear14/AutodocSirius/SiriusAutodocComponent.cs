using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Nuclear14.AutodocSirius;

[RegisterComponent, NetworkedComponent]
public sealed partial class SiriusAutodocComponent : Component
{
    public const string SiriusBeakerSlotId = "siriusBeakerSlot";

    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("entryDelay")]
    public float EntryDelay = 2f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("treatmentDuration")]
    public float TreatmentDuration = 10f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isOpen")]
    public bool IsOpen = true;

    [ViewVariables]
    public bool Powered = false;

    [ViewVariables]
    public bool IsTreating = false;

    [ViewVariables]
    public EntityUid? CurrentPatient = null;

    [Serializable, NetSerializable]
    public enum AutodocVisuals : byte
    {
        IsOn,
        IsOpen
    }
}
