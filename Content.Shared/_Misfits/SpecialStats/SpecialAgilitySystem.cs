using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Misfits.SpecialStats;

/// <summary>
/// Applies Agility to close-quarters action cadence.
/// </summary>
public sealed class SpecialAgilitySystem : EntitySystem
{
    [Dependency] private readonly SharedSpecialSystem _special = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetMeleeAttackRateEvent>(OnMeleeAttackRate);
    }

    private void OnMeleeAttackRate(ref GetMeleeAttackRateEvent args)
    {
        if (!TryComp<SpecialComponent>(args.User, out var special))
            return;

        args.Multipliers *= GetActionDelayMultiplier(args.User, special);
    }

    private float GetActionDelayMultiplier(EntityUid uid, SpecialComponent special)
    {
        var tuning = _special.GetTuning();
        var modifier = _special.GetCurvedEffectScale(
            uid,
            SpecialStat.Agility,
            tuning.AgilityActionDelayPenaltyAtOne,
            -tuning.AgilityActionDelayReductionAtTen,
            special);

        return MathF.Max(0.1f, 1f + modifier);
    }
}
