using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Components;

/// <summary>
///     When placed on a station entity, players whose roles belong to a banned department
///     will be prevented from spawning on that station.
/// </summary>
[RegisterComponent]
public sealed partial class StationDepartmentBlacklistComponent : Component
{
    /// <summary>
    ///     Departments that are banned from this station.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> BannedDepartments = new();
}
