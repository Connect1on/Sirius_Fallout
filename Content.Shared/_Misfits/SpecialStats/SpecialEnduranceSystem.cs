using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._Misfits.SpecialStats;

/// <summary>
/// Applies Endurance health bonuses when S.P.E.C.I.A.L. changes.
/// </summary>
public sealed class SpecialEnduranceSystem : EntitySystem
{
    [Dependency] private readonly SharedSpecialSystem _special = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    private static readonly MobState[] HealthThresholdStates =
    [
        MobState.SoftCritical,
        MobState.Critical,
        MobState.Dead,
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialComponent, SpecialChangedEvent>(OnSpecialChanged);
        SubscribeLocalEvent<SpecialComponent, SpecialStatsReadyEvent>(OnStatsReady);
    }

    private void OnSpecialChanged(Entity<SpecialComponent> ent, ref SpecialChangedEvent args)
    {
        ApplyEndurance(ent, false);
    }

    private void OnStatsReady(Entity<SpecialComponent> ent, ref SpecialStatsReadyEvent args)
    {
        ApplyEndurance(ent, false);
    }

    private void ApplyEndurance(Entity<SpecialComponent> ent, bool reset)
    {
        ClearLegacyStaminaModifier(ent);

        if (!TryComp<MobThresholdsComponent>(ent.Owner, out var thresholds))
            return;

        var tuning = _special.GetTuning();
        var desired = reset
            ? 0f
            : _special.GetCurvedEffectScale(
                ent.Owner,
                SpecialStat.Endurance,
                -tuning.EnduranceHealthPenaltyAtOne,
                tuning.EnduranceHealthBonusAtTen,
                ent.Comp);
        var adjustment = desired - ent.Comp.AppliedHealthThresholdModifier;

        if (MathHelper.CloseTo(adjustment, 0f))
            return;

        foreach (var state in HealthThresholdStates)
        {
            var threshold = _thresholds.GetThresholdForState(ent.Owner, state, thresholds);
            if (threshold == FixedPoint2.Zero)
                continue;

            _thresholds.SetMobStateThreshold(ent.Owner, FixedPoint2.Max(1, threshold + adjustment), state, thresholds);
        }

        ent.Comp.AppliedHealthThresholdModifier = desired;

        Dirty(ent.Owner, ent.Comp);
    }

    private void ClearLegacyStaminaModifier(Entity<SpecialComponent> ent)
    {
        if (MathHelper.CloseTo(ent.Comp.AppliedStaminaCritThresholdModifier, 0f))
            return;

        if (TryComp<StaminaComponent>(ent.Owner, out var stamina))
        {
            stamina.CritThreshold -= ent.Comp.AppliedStaminaCritThresholdModifier;
            Dirty(ent.Owner, stamina);
        }

        ent.Comp.AppliedStaminaCritThresholdModifier = 0f;
        Dirty(ent.Owner, ent.Comp);
    }
}
