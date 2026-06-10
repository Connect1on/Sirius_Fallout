using Content.Client.UserInterface.Controls;
using Content.Shared._Nuclear14.AutodocSirius;
using Robust.Client.UserInterface;
using Robust.Shared.Log;

namespace Content.Client._Nuclear14.AutodocSirius;

public sealed class SiriusAutodocBoundUserInterface : BoundUserInterface
{
    private static readonly ISawmill _sawmill = Logger.GetSawmill("autodoc");
    private SiriusAutodocWindow? _window;

    public SiriusAutodocBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _window = this.CreateWindow<SiriusAutodocWindow>();
        if (_window != null)
        {
            _window.OnAutodocButton += OnButtonPressed;
            _window.OnClose += Close;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window != null && state is AutodocBoundUserInterfaceState castState)
        {
            _sawmill.Debug($"UpdateState: HasBeaker={castState.HasBeaker}, HasOccupant={castState.HasOccupant}, IsTreating={castState.IsTreating}");
            _window.UpdateState(castState);
        }
    }

    private void OnButtonPressed(AutodocUiButton button)
    {
        _sawmill.Debug($"Button pressed: {button}");
        SendMessage(new AutodocUiButtonPressedMessage(button));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _window != null)
        {
            _window.OnAutodocButton -= OnButtonPressed;
            _window.OnClose -= Close;
            _window.Close();
        }
        _window = null;
    }
}
