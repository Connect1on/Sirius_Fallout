using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.SpecialStats;

/// <summary>
/// Marks a storage entity as eligible for the Luck S.P.E.C.I.A.L. bonus.
/// </summary>
[RegisterComponent]
public sealed partial class LuckJunkBonusComponent : Component
{
    [DataField]
    public List<EntProtoId> LuckyItems = new()
    {
        "N14Stimpak",
        "N14CurrencyCap",
        "N14MagazinePistol10mm",
        "N14MagazinePistol9mm",
    };

    /// <summary>
    /// Additional roll-success probability per Luck point above the default of 5.
    /// </summary>
    [DataField]
    public float ChancePerLuckPoint = 0.03f;
}
