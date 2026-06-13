using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets
{
    public interface IStylesheetManager
    {
        Stylesheet SheetNano { get; }
        Stylesheet SheetSpace { get; }

        void Initialize();

        /// <summary>
        /// Switches the active UI color palette by id (see <see cref="UiPalette"/>), rebuilds the
        /// Nano stylesheet and reassigns it so the live UI re-skins. Stylesheet-rule-driven controls
        /// update immediately; colors bound via <c>{x:Static}</c> refresh when their screen is reopened.
        /// </summary>
        void SetActivePalette(string id);
    }
}
