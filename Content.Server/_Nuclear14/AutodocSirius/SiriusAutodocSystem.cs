using Content.Server.Power.Components;
using Content.Shared._Nuclear14.AutodocSirius;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Log;

namespace Content.Server._Nuclear14.AutodocSirius;

public sealed partial class SiriusAutodocSystem : SharedSiriusAutodocSystem
{
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("autodoc");
        _sawmill.Debug("=== SiriusAutodocSystem INITIALIZING ===");

        SubscribeLocalEvent<SiriusAutodocComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<SiriusAutodocComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
        SubscribeLocalEvent<SiriusAutodocComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SiriusAutodocComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SiriusAutodocComponent, BoundUIClosedEvent>(OnBoundUIClosed);
        SubscribeLocalEvent<SiriusAutodocComponent, AutodocTreatmentDoAfterEvent>(OnTreatmentFinished);
        SubscribeLocalEvent<SiriusAutodocComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<SiriusAutodocComponent, AutodocUiButtonPressedMessage>(OnUiButtonPressed);
        SubscribeLocalEvent<SiriusAutodocComponent, AutodocUiToggleOpenMessage>(OnToggleOpenMessage);

        SubscribeLocalEvent<SiriusAutodocComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiriusAutodocComponent, AutodocUiEjectOccupantMessage>(OnEjectOccupantMessage);
    }

    private void OnComponentStartup(Entity<SiriusAutodocComponent> entity, ref ComponentStartup args)
    {
        _sawmill.Debug($"=== SiriusAutodocComponent STARTUP for {entity.Owner} ===");
        if (_uiSystem.HasUi(entity.Owner, SiriusAutodocUiKey.Key))
        {
            _sawmill.Debug($"Autodoc {entity.Owner} HAS UI registered for key {SiriusAutodocUiKey.Key}");
        }
        else
        {
            _sawmill.Debug($"WARNING: Autodoc {entity.Owner} has NO UI for key {SiriusAutodocUiKey.Key}!");
        }
    }
}
