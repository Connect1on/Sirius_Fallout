using Content.Shared.Administration.Logs;
using Content.Shared._Nuclear14.AutodocSirius;
using Content.Shared.Body.Components;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Nuclear14.AutodocSirius;

public abstract partial class SharedAutodocSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly Dictionary<EntityUid, TimeSpan> _lastToggleTime = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiriusAutodocComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SiriusAutodocComponent, CanDropTargetEvent>(OnCanDropOn);
        SubscribeLocalEvent<SiriusAutodocComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<SiriusAutodocComponent, SiriusAutodocInsertFinished>(OnInsertFinished);
    }

    private void OnComponentInit(EntityUid uid, SiriusAutodocComponent component, ComponentInit args)
    {
        component.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "autodoc-body");
    }

    private void OnCanDropOn(EntityUid uid, SiriusAutodocComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!component.IsOpen)
        {
            args.CanDrop = false;
            args.Handled = true;
            return;
        }

        args.CanDrop = HasComp<BodyComponent>(args.Dragged);
        args.Handled = true;
    }

    protected void AddAlternativeVerbs(EntityUid uid, SiriusAutodocComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = component.IsOpen ? Loc.GetString("autodoc-verb-close") : Loc.GetString("autodoc-verb-open"),
            Priority = 1,
            Act = () => TryToggleOpen(uid, args.User, component)
        });

        if (component.BodyContainer.ContainedEntity != null && !component.IsOpen)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("autodoc-verb-eject"),
                Category = VerbCategory.Eject,
                Priority = 1,
                Act = () => TryEjectBody(uid, args.User, component)
            });
        }
    }

    public void TryToggleOpen(EntityUid uid, EntityUid user, SiriusAutodocComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var now = _gameTiming.CurTime;
        if (_lastToggleTime.TryGetValue(uid, out var last) && (now - last).TotalMilliseconds < 100)
            return;
        _lastToggleTime[uid] = now;

        component.IsOpen = !component.IsOpen;
        UpdateAppearance(uid, component);
    }

    public bool InsertBody(EntityUid uid, EntityUid target, SiriusAutodocComponent component)
    {
        if (!component.IsOpen)
            return false;

        if (component.BodyContainer.ContainedEntity != null)
            return false;

        if (!HasComp<MobStateComponent>(target))
            return false;

        _containerSystem.Insert(target, component.BodyContainer, containerXform: Transform(component.BodyContainer.Owner));

        EnsureComp<InsideAutodocComponent>(target);
        _standingState.Stand(target, force: true);

        return true;
    }

    public void TryEjectBody(EntityUid uid, EntityUid user, SiriusAutodocComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsOpen)
            return;

        var ejected = EjectBody(uid, component);
        if (ejected != null)
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(ejected.Value)} ejected from {ToPrettyString(uid)} by {ToPrettyString(user)}");
        }
    }

    public virtual EntityUid? EjectBody(EntityUid uid, SiriusAutodocComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (component.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return null;

        _containerSystem.Remove(contained, component.BodyContainer);

        if (HasComp<KnockedDownComponent>(contained))
        {
            _standingState.Down(contained);
        }
        else
        {
            _standingState.Stand(contained);
        }

        UpdateAppearance(uid, component);
        return contained;
    }

    protected void UpdateAppearance(EntityUid uid, SiriusAutodocComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, SiriusAutodocComponent.AutodocVisuals.IsOn, component.Powered, appearance);
        _appearance.SetData(uid, SiriusAutodocComponent.AutodocVisuals.IsOpen, component.IsOpen, appearance);
    }

    private void OnInsertFinished(EntityUid uid, SiriusAutodocComponent component, SiriusAutodocInsertFinished args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (InsertBody(uid, args.Args.Target.Value, component))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(args.User)} inserted {ToPrettyString(args.Args.Target.Value)} into {ToPrettyString(uid)}");
        }
        args.Handled = true;
    }

    [Serializable, NetSerializable]
    public sealed partial class SiriusAutodocInsertFinished : SimpleDoAfterEvent
    {
    }
}
