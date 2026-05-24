using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Special.Prototypes;

[Prototype("specialTuning")]
public sealed partial class SpecialTuningPrototype : IPrototype
{
    public static readonly SpecialTuningPrototype Fallback = new()
    {
        ID = "Fallback",
    };

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("strengthMeleeDamageMultiplierPerPoint")]
    public float StrengthMeleeDamageMultiplierPerPoint = 0.015f;

    [DataField("perceptionSpreadReductionPerPoint")]
    public float PerceptionSpreadReductionPerPoint = 0.004f;

    [DataField("perceptionSpreadPenaltyAtOne")]
    public float PerceptionSpreadPenaltyAtOne = 0.15f;

    [DataField("perceptionSpreadReductionAtTen")]
    public float PerceptionSpreadReductionAtTen = 0.25f;

    [DataField("enduranceStaminaCritThresholdPerPoint")]
    public float EnduranceStaminaCritThresholdPerPoint = 4f;

    [DataField("enduranceHealthPenaltyAtOne")]
    public float EnduranceHealthPenaltyAtOne = 25f;

    [DataField("enduranceHealthBonusAtTen")]
    public float EnduranceHealthBonusAtTen = 25f;

    [DataField("agilityMovementSpeedMultiplierPerPoint")]
    public float AgilityMovementSpeedMultiplierPerPoint = 0.004f;

    [DataField("agilityMovementSpeedPenaltyAtOne")]
    public float AgilityMovementSpeedPenaltyAtOne = 0.10f;

    [DataField("agilityMovementSpeedBonusAtTen")]
    public float AgilityMovementSpeedBonusAtTen = 0.10f;

    [DataField("luckCriticalChancePerPoint")]
    public float LuckCriticalChancePerPoint = 0.005f;

    [DataField("luckSingleShotCriticalChanceAtTen")]
    public float LuckSingleShotCriticalChanceAtTen = 0.3f;

    [DataField("luckCriticalDamageMultiplier")]
    public float LuckCriticalDamageMultiplier = 1.5f;

    [DataField("luckLootChancePerPoint")]
    public float LuckLootChancePerPoint = 0.03f;
}
