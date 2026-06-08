using System.Numerics;
using Content.Shared._Nuclear14.AutodocSirius;
using Content.Shared.DrawDepth;
using Robust.Client.GameObjects;
using DrawDepthShared = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Nuclear14.AutodocSirius;

public sealed class AutodocSystem : SharedAutodocSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiriusAutodocComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<InsideAutodocComponent, ComponentStartup>(OnInsideStartup);
        SubscribeLocalEvent<InsideAutodocComponent, ComponentRemove>(OnInsideRemove);
    }

    private void OnInsideStartup(EntityUid uid, InsideAutodocComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        component.PreviousOffset = sprite.Offset;
        _spriteSystem.SetOffset(uid, new Vector2(0, 0.5f));
    }

    private void OnInsideRemove(EntityUid uid, InsideAutodocComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _spriteSystem.SetOffset(uid, component.PreviousOffset);
    }

    private void OnAppearanceChange(EntityUid uid, SiriusAutodocComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, SiriusAutodocComponent.AutodocVisuals.IsOn, out var isOn, args.Component))
            isOn = false;

        if (!_appearance.TryGetData<bool>(uid, SiriusAutodocComponent.AutodocVisuals.IsOpen, out var isOpen, args.Component))
            isOpen = true;

        var baseLayer = args.Sprite.LayerMapReserveBlank(AutodocVisualLayers.Base);
        _spriteSystem.LayerSetRsiState(uid, baseLayer, isOn ? "autodoc_on" : "autodoc_off");

        var doorLayer = args.Sprite.LayerMapReserveBlank(AutodocVisualLayers.Door);
        if (isOpen)
        {
            _spriteSystem.LayerSetVisible(uid, doorLayer, false);
            _spriteSystem.SetDrawDepth(uid, (int) DrawDepthShared.Objects);
        }
        else
        {
            _spriteSystem.LayerSetVisible(uid, doorLayer, true);
            _spriteSystem.LayerSetRsiState(uid, doorLayer, "autodoc_door");
            _spriteSystem.SetDrawDepth(uid, (int) DrawDepthShared.Mobs);
        }
    }
}

public enum AutodocVisualLayers : byte
{
    Base,
    Door
}
