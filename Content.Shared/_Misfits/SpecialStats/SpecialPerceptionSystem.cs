using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._Misfits.SpecialStats;

/// <summary>
/// Reduces gun spread and camera recoil proportionally based on the holder's Perception.
/// </summary>
public sealed class SpecialPerceptionSystem : EntitySystem
{
    [Dependency] private readonly SharedSpecialSystem _special = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnGunRefreshModifiers(ref GunRefreshModifiersEvent args)
    {
        var holder = Transform(args.Gun.Owner).ParentUid;

        if (!TryComp<SpecialComponent>(holder, out var special))
            return;

        var tuning = _special.GetTuning();
        var modifier = _special.GetCurvedEffectScale(
            holder,
            SpecialStat.Perception,
            tuning.PerceptionSpreadPenaltyAtOne,
            -tuning.PerceptionSpreadReductionAtTen,
            special);
        var keepFraction = Math.Clamp(1.0 + modifier, 0.5, 2.0);

        args.MinAngle = new Angle((double) args.MinAngle * keepFraction);
        args.MaxAngle = new Angle((double) args.MaxAngle * keepFraction);
        args.AngleIncrease = new Angle((double) args.AngleIncrease * keepFraction);
        args.CameraRecoilScalar *= (float) keepFraction;
    }
}
