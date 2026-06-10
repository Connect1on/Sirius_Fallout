using Content.Shared.Administration.Logs;
using Content.Shared._Nuclear14.AutodocSirius;
using Content.Shared.Body.Components;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Nuclear14.AutodocSirius;

public abstract partial class SharedSiriusAutodocSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly StandingStateSystem _standingState = default!;
    [Dependency] protected readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] protected readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] protected readonly SharedPointLightSystem _light = default!;
    [Dependency] protected readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly IGameTiming _gameTiming = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfterSystem = default!;

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

    protected void OnDragDrop(Entity<SiriusAutodocComponent> entity, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!entity.Comp.IsOpen)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-not-open"), entity, args.User);
            return;
        }

        if (entity.Comp.BodyContainer.ContainedEntity != null)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-occupied"), entity, args.User);
            return;
        }

        if (entity.Comp.IsTreating)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-cant-insert-during-treatment"), entity, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, entity.Comp.EntryDelay, new SiriusAutodocInsertFinished(), entity, target: args.Dragged, used: entity)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    protected void AddAlternativeVerbs(EntityUid uid, SiriusAutodocComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!component.IsTreating)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = component.IsOpen ? Loc.GetString("autodoc-verb-close") : Loc.GetString("autodoc-verb-open"),
                Priority = 1,
                Act = () => TryToggleOpen(uid, args.User, component)
            });
        }

        if (component.BodyContainer.ContainedEntity != null && !component.IsTreating)
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

        if (component.IsTreating)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-cant-toggle-during-treatment"), uid, user);
            return;
        }

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

        if (component.IsTreating)
            return false;

        _containerSystem.Insert(target, component.BodyContainer, containerXform: Transform(component.BodyContainer.Owner));

        EnsureComp<InsideAutodocComponent>(target);
        _standingState.Stand(target, force: true);

        UpdateAppearance(uid, component);

        return true;
    }

    public void TryEjectBody(EntityUid uid, EntityUid user, SiriusAutodocComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsTreating)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-cant-eject-during-treatment"), uid, user);
            return;
        }

        if (!component.IsOpen)
        {
            component.IsOpen = true;
            UpdateAppearance(uid, component);
        }

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

        if (component.IsTreating)
            return null;

        if (component.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return null;

        _containerSystem.Remove(contained, component.BodyContainer);

        if (HasComp<InsideAutodocComponent>(contained))
            RemComp<InsideAutodocComponent>(contained);

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
