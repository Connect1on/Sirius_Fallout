using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared._Nuclear14.AutodocSirius;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._Nuclear14.AutodocSirius;

public sealed partial class SiriusAutodocSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly Dictionary<EntityUid, TimeSpan> _treatmentStartTime = new();

    private const string StimulantsReagentId = "HealingMixture";
    private const int StimulantsRequired = 30;
    private bool _isUpdating = false;
    private const float UiUpdateInterval = 0.5f;
    private readonly Dictionary<EntityUid, TimeSpan> _lastUiUpdate = new();

    private void OnContainerInserted(Entity<SiriusAutodocComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == SiriusAutodocComponent.SiriusBeakerSlotId)
        {
            UpdateUiState(entity);
        }
        else if (args.Container.ID == "autodoc-body")
        {
            entity.Comp.CurrentPatient = args.Entity;
            UpdateUiState(entity);
        }
    }

    private void OnContainerRemoved(Entity<SiriusAutodocComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == SiriusAutodocComponent.SiriusBeakerSlotId)
        {
            UpdateUiState(entity);
        }
        else if (args.Container.ID == "autodoc-body")
        {
            entity.Comp.CurrentPatient = null;
            UpdateUiState(entity);
        }
    }

    private void OnPowerChanged(Entity<SiriusAutodocComponent> entity, ref PowerChangedEvent args)
    {
        entity.Comp.Powered = args.Powered;
        UpdateAppearance(entity.Owner, entity.Comp);
        UpdateUiState(entity);

        if (!args.Powered && entity.Comp.IsTreating)
        {
            entity.Comp.IsTreating = false;
            UpdateUiState(entity);
        }
    }

    private void OnBoundUIOpened(Entity<SiriusAutodocComponent> entity, ref BoundUIOpenedEvent args)
    {
        _lastUiUpdate[entity.Owner] = _gameTiming.CurTime;
        UpdateUiState(entity);
    }

    private void OnBoundUIClosed(Entity<SiriusAutodocComponent> entity, ref BoundUIClosedEvent args)
    {
        _lastUiUpdate.Remove(entity.Owner);
    }

    private void OnEjectBeakerMessage(Entity<SiriusAutodocComponent> entity, EntityUid user)
    {
        if (entity.Comp.IsTreating)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-cant-eject-beaker-treating"), entity, user);
            UpdateUiState(entity);
            return;
        }

        var result = _itemSlots.TryEject(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId, user, out var ejected);
        if (result)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-beaker-ejected"), entity, user);
        }

        UpdateUiState(entity);
    }

    private void OnStartTreatmentMessage(Entity<SiriusAutodocComponent> entity, EntityUid user)
    {
        if (!CanStartTreatment(entity))
        {
            UpdateUiState(entity);
            return;
        }

        if (entity.Comp.IsOpen)
        {
            UpdateUiState(entity);
            return;
        }

        entity.Comp.IsTreating = true;
        _treatmentStartTime[entity.Owner] = _gameTiming.CurTime;

        UpdateUiState(entity);
        UpdateAppearance(entity.Owner, entity.Comp);
    }

    private void OnUiButtonPressed(Entity<SiriusAutodocComponent> entity, ref AutodocUiButtonPressedMessage message)
    {
        switch (message.Button)
        {
            case AutodocUiButton.OpenDoor:
                if (!entity.Comp.IsTreating && !entity.Comp.IsOpen)
                {
                    entity.Comp.IsOpen = true;
                    UpdateAppearance(entity.Owner, entity.Comp);
                    UpdateUiState(entity);
                }
                break;

            case AutodocUiButton.CloseDoor:
                if (!entity.Comp.IsTreating && entity.Comp.IsOpen)
                {
                    if (entity.Comp.BodyContainer.ContainedEntity == null)
                    {
                        entity.Comp.IsOpen = false;
                        UpdateAppearance(entity.Owner, entity.Comp);
                        UpdateUiState(entity);
                    }
                    else
                    {
                        entity.Comp.IsOpen = false;
                        UpdateAppearance(entity.Owner, entity.Comp);
                        UpdateUiState(entity);
                    }
                }
                break;

            case AutodocUiButton.EjectBeaker:
                OnEjectBeakerMessage(entity, message.Actor);
                break;

            case AutodocUiButton.EjectPatient:
                if (entity.Comp.IsOpen)
                {
                    if (entity.Comp.IsEjecting)
                    {
                        return;
                    }

                    entity.Comp.IsEjecting = true;
                    try
                    {
                        TryEjectBody(entity.Owner, message.Actor, entity.Comp);
                        UpdateUiState(entity);
                    }
                    finally
                    {
                        entity.Comp.IsEjecting = false;
                    }
                }
                else
                {
                }
                break;

            case AutodocUiButton.StartTreatment:
                OnStartTreatmentMessage(entity, message.Actor);
                break;
        }
    }

    private void OnToggleOpenMessage(Entity<SiriusAutodocComponent> entity, ref AutodocUiToggleOpenMessage message)
    {
        if (entity.Comp.IsTreating)
            return;

        TryToggleOpen(entity.Owner, message.Actor, entity.Comp);
    }

    private bool CanStartTreatment(Entity<SiriusAutodocComponent> entity)
    {
        if (entity.Comp.CurrentPatient == null || entity.Comp.BodyContainer.ContainedEntity == null)
        {
            return false;
        }

        if (entity.Comp.IsOpen)
        {
            return false;
        }

        if (!entity.Comp.Powered)
        {
            return false;
        }

        var beaker = _itemSlots.GetItemOrNull(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId);
        if (beaker == null)
        {
            return false;
        }

        if (!_solutionContainer.TryGetFitsInDispenser(beaker.Value, out var soln, out var solution))
        {
            return false;
        }

        var stimulantsAmount = solution.GetReagentQuantity(new(StimulantsReagentId, null));
        if (stimulantsAmount < StimulantsRequired)
        {
            return false;
        }

        return true;
    }

    private void HealPatient(EntityUid patient)
    {
        if (!TryComp<DamageableComponent>(patient, out var damageable))
            return;

        var healSpec = new DamageSpecifier();
        foreach (var damage in damageable.Damage.DamageDict)
        {
            if (damage.Value > 0)
                healSpec.DamageDict[damage.Key] = -damage.Value;
        }

        if (!healSpec.Empty)
            _damageable.TryChangeDamage(patient, healSpec, true);
    }

    private void UpdateUiState(Entity<SiriusAutodocComponent> entity)
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            if (!_uiSystem.HasUi(entity.Owner, SiriusAutodocUiKey.Key))
            {
                return;
            }

            var state = GetUiState(entity);
            _uiSystem.SetUiState(entity.Owner, SiriusAutodocUiKey.Key, state);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private AutodocBoundUserInterfaceState GetUiState(Entity<SiriusAutodocComponent> entity)
    {
        var component = entity.Comp;
        var hasOccupant = component.BodyContainer.ContainedEntity != null;
        var occupantDamage = new Dictionary<string, FixedPoint2>();
        var occupantStatus = OccupantStatus.None;
        var occupantName = string.Empty;

        if (hasOccupant && component.CurrentPatient is { } patient)
        {
            if (TryComp<DamageableComponent>(patient, out var damageable))
            {
                foreach (var damage in damageable.Damage.DamageDict)
                {
                    occupantDamage[damage.Key] = damage.Value;
                }
            }

            if (TryComp<MobStateComponent>(patient, out var mobState))
            {
                occupantStatus = mobState.CurrentState switch
                {
                    MobState.Alive => OccupantStatus.Alive,
                    MobState.Critical => OccupantStatus.Critical,
                    MobState.Dead => OccupantStatus.Dead,
                    _ => OccupantStatus.None
                };
            }

            if (TryComp<MetaDataComponent>(patient, out var meta))
                occupantName = meta.EntityName;
        }

        var beakerStimulants = FixedPoint2.Zero;
        var hasBeaker = false;

        var beaker = _itemSlots.GetItemOrNull(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId);

        if (beaker != null)
        {
            hasBeaker = true;
            if (_solutionContainer.TryGetFitsInDispenser(beaker.Value, out _, out var solution))
            {
                beakerStimulants = solution.GetReagentQuantity(new(StimulantsReagentId, null));
            }
        }

        var treatmentProgress = 0f;
        if (component.IsTreating && _treatmentStartTime.TryGetValue(entity.Owner, out var startTime))
        {
            var elapsed = (float) (_gameTiming.CurTime - startTime).TotalSeconds;
            treatmentProgress = Math.Clamp(elapsed / component.TreatmentDuration, 0, 1);
        }

        var canTreat = CanStartTreatment(entity);
        var treatButtonEnabled = canTreat && !component.IsTreating;
        return new AutodocBoundUserInterfaceState(
            component.IsOpen,
            component.Powered,
            hasOccupant,
            component.IsTreating,
            occupantStatus,
            occupantDamage,
            occupantName,
            hasBeaker,
            FixedPoint2.Zero,
            FixedPoint2.Zero,
            beakerStimulants,
            treatButtonEnabled,
            treatmentProgress
        );
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;

        var treatmentsToComplete = new List<EntityUid>();

        foreach (var (uid, startTime) in _treatmentStartTime)
        {
            if (!TryComp<SiriusAutodocComponent>(uid, out var comp))
            {
                treatmentsToComplete.Add(uid);
                continue;
            }

            if (!comp.IsTreating)
            {
                treatmentsToComplete.Add(uid);
                continue;
            }

            var elapsed = (currentTime - startTime).TotalSeconds;
            if (elapsed >= comp.TreatmentDuration)
            {
                treatmentsToComplete.Add(uid);
                CompleteTreatment((uid, comp));
            }
        }

        foreach (var uid in treatmentsToComplete)
        {
            _treatmentStartTime.Remove(uid);
        }

        var query = EntityQueryEnumerator<SiriusAutodocComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_uiSystem.IsUiOpen(uid, SiriusAutodocUiKey.Key))
                continue;

            if (!_lastUiUpdate.TryGetValue(uid, out var lastUpdate))
            {
                _lastUiUpdate[uid] = currentTime;
                UpdateUiState((uid, comp));
                continue;
            }

            if ((currentTime - lastUpdate).TotalSeconds >= UiUpdateInterval)
            {
                _lastUiUpdate[uid] = currentTime;
                UpdateUiState((uid, comp));
            }
        }
    }

    private void CompleteTreatment(Entity<SiriusAutodocComponent> entity)
    {
        if (!entity.Comp.IsTreating)
            return;

        if (!entity.Comp.Powered)
        {
            entity.Comp.IsTreating = false;
            UpdateUiState(entity);
            UpdateAppearance(entity.Owner, entity.Comp);
            return;
        }

        if (entity.Comp.CurrentPatient is { } patient)
        {
            HealPatient(patient);

            var beaker = _itemSlots.GetItemOrNull(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId);
            if (beaker != null && _solutionContainer.TryGetFitsInDispenser(beaker.Value, out var soln, out var solution))
            {
                var stimulantsAmount = solution.GetReagentQuantity(new(StimulantsReagentId, null));
                var toRemove = FixedPoint2.Min(StimulantsRequired, stimulantsAmount);
                _solutionContainer.RemoveReagent(soln.Value, new(StimulantsReagentId, null), toRemove);
            }
        }

        entity.Comp.IsTreating = false;
        UpdateUiState(entity);
        UpdateAppearance(entity.Owner, entity.Comp);
    }
}
