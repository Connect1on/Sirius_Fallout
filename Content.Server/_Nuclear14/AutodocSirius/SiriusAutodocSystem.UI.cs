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

    private const string StimulantsReagentId = "Stimulants";
    private const int StimulantsRequired = 30;

    private const float UiUpdateInterval = 1.0f;
    private readonly Dictionary<EntityUid, TimeSpan> _lastUiUpdate = new();

    private void OnContainerInserted(Entity<SiriusAutodocComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == SiriusAutodocComponent.SiriusBeakerSlotId)
        {
            _sawmill.Debug($"Beaker inserted into slot {args.Container.ID}");
            UpdateUiState(entity);
        }
        else if (args.Container.ID == "autodoc-body")
        {
            entity.Comp.CurrentPatient = args.Entity;
            _sawmill.Debug($"Patient inserted into autodoc: {args.Entity}");
            UpdateUiState(entity);
        }
    }

    private void OnContainerRemoved(Entity<SiriusAutodocComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == SiriusAutodocComponent.SiriusBeakerSlotId)
        {
            _sawmill.Debug($"Beaker removed from slot {args.Container.ID}");
            UpdateUiState(entity);
        }
        else if (args.Container.ID == "autodoc-body")
        {
            entity.Comp.CurrentPatient = null;
            _sawmill.Debug($"Patient removed from autodoc");
            UpdateUiState(entity);
        }
    }

    private void OnPowerChanged(Entity<SiriusAutodocComponent> entity, ref PowerChangedEvent args)
    {
        entity.Comp.Powered = args.Powered;
        _sawmill.Debug($"Power changed: {args.Powered}");
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
        _sawmill.Debug($"=== BOUND UI OPENED for autodoc {entity.Owner}, user: {args.Actor} ===");
        _lastUiUpdate[entity.Owner] = _gameTiming.CurTime;
        UpdateUiState(entity);
    }

    private void OnBoundUIClosed(Entity<SiriusAutodocComponent> entity, ref BoundUIClosedEvent args)
    {
        _sawmill.Debug($"=== BOUND UI CLOSED for autodoc {entity.Owner} ===");
        _lastUiUpdate.Remove(entity.Owner);
    }

    private void OnEjectBeakerMessage(Entity<SiriusAutodocComponent> entity, EntityUid user)
    {
        _sawmill.Debug($"EjectBeakerMessage received, IsTreating={entity.Comp.IsTreating}");

        if (entity.Comp.IsTreating)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-cant-eject-beaker-treating"), entity, user);
            UpdateUiState(entity);
            return;
        }

        var result = _itemSlots.TryEject(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId, user, out var ejected);
        _sawmill.Debug($"TryEject result: {result}, ejected: {ejected}");

        if (result)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-beaker-ejected"), entity, user);
        }

        UpdateUiState(entity);
    }

    private void OnEjectOccupantMessage(Entity<SiriusAutodocComponent> entity, ref AutodocUiEjectOccupantMessage message)
    {
        _sawmill.Debug($"=== OnEjectOccupantMessage CALLED for autodoc {entity.Owner} from user {message.Actor} ===");

        if (entity.Comp.IsTreating)
        {
            _sawmill.Debug("Cannot eject - autodoc is treating");
            _popupSystem.PopupEntity(Loc.GetString("autodoc-cant-eject-patient-treating"), entity, message.Actor);
            UpdateUiState(entity);
            return;
        }

        if (!entity.Comp.IsOpen)
        {
            _sawmill.Debug("Autodoc is closed, opening first");
            entity.Comp.IsOpen = true;
            UpdateAppearance(entity.Owner, entity.Comp);
            UpdateUiState(entity);
        }

        var ejected = EjectBody(entity.Owner, entity.Comp);
        if (ejected != null)
        {
            _sawmill.Debug($"Successfully ejected patient {ejected}");
            _popupSystem.PopupEntity(Loc.GetString("autodoc-patient-ejected"), entity, message.Actor);
        }
        else
        {
            _sawmill.Debug("Eject failed - no patient inside");
        }
        UpdateUiState(entity);
    }

    private void OnStartTreatmentMessage(Entity<SiriusAutodocComponent> entity, EntityUid user)
    {
        _sawmill.Debug($"StartTreatmentMessage received");

        if (!CanStartTreatment(entity, out var errorMessage))
        {
            _sawmill.Debug($"Cannot start treatment: {errorMessage}");
            _popupSystem.PopupEntity(errorMessage, entity, user);
            UpdateUiState(entity);
            return;
        }

        if (entity.Comp.IsOpen)
        {
            _popupSystem.PopupEntity(Loc.GetString("autodoc-error-not-closed"), entity, user);
            UpdateUiState(entity);
            return;
        }

        entity.Comp.IsTreating = true;

        // ЗАПОМИНАЕМ ВРЕМЯ НАЧАЛА ЛЕЧЕНИЯ
        _treatmentStartTime[entity.Owner] = _gameTiming.CurTime;

        UpdateUiState(entity);
        UpdateAppearance(entity.Owner, entity.Comp);

        // Альтернатива: не используем DoAfter вообще, полагаемся на таймаут в Update
        // Просто помечаем, что лечение началось, и таймаут завершит его
        _sawmill.Debug($"Treatment started on autodoc {entity.Owner}, will complete in {entity.Comp.TreatmentDuration} seconds");
    }

    //private void OnTreatmentFinished(Entity<SiriusAutodocComponent> entity, ref AutodocTreatmentDoAfterEvent args)
    //// {
    //// _sawmill.Debug($"Treatment finished! Cancelled={args.Cancelled}, Handled={args.Handled}");

    // УДАЛЯЕМ ВРЕМЯ НАЧАЛА ЛЕЧЕНИЯ
    // _treatmentStartTime.Remove(entity.Owner);

    //     if (args.Cancelled || args.Handled)
    //    {
    // entity.Comp.IsTreating = false;
    // UpdateUiState(entity);
    // UpdateAppearance(entity.Owner, entity.Comp);

    // Если лечение было прервано, уведомляем пользователя
    //   if (args.Args.User.IsValid())
    //    {
    // _popupSystem.PopupEntity(Loc.GetString("autodoc-treatment-cancelled"), entity, args.Args.User);
    //}
    //      return;
    //}

    //    if (!entity.Comp.Powered)
    //    {
    //  entity.Comp.IsTreating = false;
    //       if (args.Args.User.IsValid())
    //_popupSystem.PopupEntity(Loc.GetString("autodoc-error-no-power"), entity, args.Args.User);
    // UpdateUiState(entity);
    // UpdateAppearance(entity.Owner, entity.Comp);
    //         return;
    //}

    //       if (entity.Comp.CurrentPatient is { } patient)
    //       {
    // Лечим пациента
    //  HealPatient(patient);

    // Списываем стимулянты
    // var beaker = _itemSlots.GetItemOrNull(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId);
    //      if (beaker != null && _solutionContainer.TryGetSolution(beaker.Value, "beaker", out var soln, out var solution))
    //     {
    //var stimulantsAmount = solution.GetReagentQuantity(new(StimulantsReagentId, null));
    //var toRemove = FixedPoint2.Min(StimulantsRequired, stimulantsAmount);
    //_solutionContainer.RemoveReagent(soln.Value, new(StimulantsReagentId, null), toRemove);
    //}

    // Проверяем статус после лечения
    //     if (TryComp<MobStateComponent>(patient, out var mobState))
    //    {
    // if (mobState.CurrentState == MobState.Dead)
    //   {
    // _popupSystem.PopupEntity(Loc.GetString("autodoc-treatment-complete-still-dead"), entity, args.Args.User);
    //}
    //          else
    //          {
    // _popupSystem.PopupEntity(Loc.GetString("autodoc-treatment-complete"), entity, args.Args.User);
    //}
    //}
    //}

    //entity.Comp.IsTreating = false;
    //     UpdateUiState(entity);
    //  //      UpdateAppearance(entity.Owner, entity.Comp);
    // }

    private void OnUiButtonPressed(Entity<SiriusAutodocComponent> entity, ref AutodocUiButtonPressedMessage message)
    {
        _sawmill.Debug($"OnUiButtonPressed: Button={message.Button}, Actor={message.Actor}");

        switch (message.Button)
        {
            case AutodocUiButton.EjectBeaker:
                OnEjectBeakerMessage(entity, message.Actor);
                break;
            case AutodocUiButton.EjectPatient:
                var ejectMsg = new AutodocUiEjectOccupantMessage();
                OnEjectOccupantMessage(entity, ref ejectMsg);
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

    private bool CanStartTreatment(Entity<SiriusAutodocComponent> entity, out string errorMessage)
    {
        if (entity.Comp.CurrentPatient == null || entity.Comp.BodyContainer.ContainedEntity == null)
        {
            errorMessage = Loc.GetString("autodoc-error-no-patient");
            return false;
        }

        if (entity.Comp.IsOpen)
        {
            errorMessage = Loc.GetString("autodoc-error-not-closed");
            return false;
        }

        if (!entity.Comp.Powered)
        {
            errorMessage = Loc.GetString("autodoc-error-no-power");
            return false;
        }

        var beaker = _itemSlots.GetItemOrNull(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId);
        if (beaker == null)
        {
            errorMessage = Loc.GetString("autodoc-error-no-beaker");
            return false;
        }

        if (!_solutionContainer.TryGetSolution(beaker.Value, "beaker", out _, out var solution))
        {
            errorMessage = Loc.GetString("autodoc-error-beaker-empty");
            return false;
        }

        var stimulantsAmount = solution.GetReagentQuantity(new(StimulantsReagentId, null));
        if (stimulantsAmount < StimulantsRequired)
        {
            errorMessage = Loc.GetString("autodoc-error-insufficient-stimulants", ("required", StimulantsRequired), ("current", stimulantsAmount));
            return false;
        }

        errorMessage = string.Empty;
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
        if (!_uiSystem.HasUi(entity.Owner, SiriusAutodocUiKey.Key))
            return;

        var state = GetUiState(entity);
        _uiSystem.SetUiState(entity.Owner, SiriusAutodocUiKey.Key, state);
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

            if (_solutionContainer.TryGetSolution(beaker.Value, "beaker", out _, out var solution))
            {
                beakerStimulants = solution.GetReagentQuantity(new(StimulantsReagentId, null));
            }
            else if (_solutionContainer.TryGetSolution(beaker.Value, "solution", out _, out solution))
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

        var canTreat = CanStartTreatment(entity, out var errorMessage);
        var treatButtonEnabled = canTreat && !component.IsTreating;

        _sawmill.Debug($"GetUiState FINAL: HasBeaker={hasBeaker}, HasOccupant={hasOccupant}, TreatButtonEnabled={treatButtonEnabled}, IsTreating={component.IsTreating}, Progress={treatmentProgress:F2}");

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
            errorMessage,
            treatButtonEnabled,
            treatmentProgress
        );
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;

        // Обрабатываем лечение через таймаут
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
                // Лечение завершено
                treatmentsToComplete.Add(uid);
                CompleteTreatment((uid, comp));
            }
        }

        foreach (var uid in treatmentsToComplete)
        {
            _treatmentStartTime.Remove(uid);
        }

        // Обычное обновление UI
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
        _sawmill.Debug($"Completing treatment for {entity.Owner}");

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
            // Лечим пациента
            HealPatient(patient);

            // Списываем стимулянты
            var beaker = _itemSlots.GetItemOrNull(entity.Owner, SiriusAutodocComponent.SiriusBeakerSlotId);
            if (beaker != null && _solutionContainer.TryGetSolution(beaker.Value, "beaker", out var soln, out var solution))
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
