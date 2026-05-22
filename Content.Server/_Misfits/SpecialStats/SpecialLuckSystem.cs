using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Random;

namespace Content.Server._Misfits.SpecialStats;

/// <summary>
/// Grants a small bonus item chance when a lucky player opens marked junk storage.
/// </summary>
public sealed class SpecialLuckSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSpecialSystem _special = default!;

    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _alreadyRolled = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LuckJunkBonusComponent, ComponentShutdown>(OnLuckCompShutdown);
        SubscribeLocalEvent<LuckJunkBonusComponent, BoundUIOpenedEvent>(OnStorageOpened);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _alreadyRolled.Clear();
    }

    private void OnLuckCompShutdown(Entity<LuckJunkBonusComponent> ent, ref ComponentShutdown args)
    {
        _alreadyRolled.Remove(ent.Owner);
    }

    private void OnStorageOpened(Entity<LuckJunkBonusComponent> ent, ref BoundUIOpenedEvent args)
    {
        var actor = args.Actor;
        if (!TryComp<SpecialComponent>(actor, out var special))
            return;

        if (!_alreadyRolled.TryGetValue(ent.Owner, out var rolledSet))
        {
            rolledSet = new HashSet<EntityUid>();
            _alreadyRolled[ent.Owner] = rolledSet;
        }

        if (!rolledSet.Add(actor))
            return;

        var rollChance = _special.GetLuckRollChance(actor, 0f, ent.Comp.ChancePerLuckPoint, special);
        if (!_random.Prob(rollChance))
            return;

        if (ent.Comp.LuckyItems.Count == 0)
            return;

        var chosenProto = _random.Pick(ent.Comp.LuckyItems);
        Spawn(chosenProto, Transform(ent.Owner).Coordinates);
    }
}
