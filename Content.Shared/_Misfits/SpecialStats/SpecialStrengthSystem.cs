using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Maths;

namespace Content.Shared._Misfits.SpecialStats;

/// <summary>
/// Applies Strength to heavy handling outside of direct melee damage.
/// </summary>
public sealed class SpecialStrengthSystem : EntitySystem
{
    [Dependency] private readonly SharedSpecialSystem _special = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialCarryPullSpeedModifierEvent>(OnCarryPullSpeedModifier);
        SubscribeLocalEvent<GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnCarryPullSpeedModifier(ref SpecialCarryPullSpeedModifierEvent args)
    {
        if (!TryComp<SpecialComponent>(args.User, out var special))
            return;

        var tuning = _special.GetTuning();
        var modifier = _special.GetCurvedEffectScale(
            args.User,
            SpecialStat.Strength,
            -tuning.StrengthCarryPullSpeedPenaltyAtOne,
            tuning.StrengthCarryPullSpeedBonusAtTen,
            special);
        var multiplier = MathF.Max(0.1f, 1f + modifier);

        args.ModifySpeed(multiplier);
    }

    private void OnGunRefreshModifiers(ref GunRefreshModifiersEvent args)
    {
        if (!HasComp<GunRequiresWieldComponent>(args.Gun.Owner) &&
            !HasComp<WieldableComponent>(args.Gun.Owner))
            return;

        var holder = Transform(args.Gun.Owner).ParentUid;
        if (!TryComp<SpecialComponent>(holder, out var special))
            return;

        var tuning = _special.GetTuning();
        var modifier = _special.GetCurvedEffectScale(
            holder,
            SpecialStat.Strength,
            tuning.StrengthHeavyGunPenaltyAtOne,
            -tuning.StrengthHeavyGunReductionAtTen,
            special);
        var keepFraction = Math.Clamp(1.0 + modifier, 0.70, 1.40);

        args.MinAngle = new Angle((double) args.MinAngle * keepFraction);
        args.MaxAngle = new Angle((double) args.MaxAngle * keepFraction);
        args.AngleIncrease = new Angle((double) args.AngleIncrease * keepFraction);
        args.CameraRecoilScalar *= (float) keepFraction;
    }
}
