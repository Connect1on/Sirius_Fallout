using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Content.Client.Stylesheets
{
    /// <summary>
    /// A swappable UI color palette. Drives the themeable color fields on <see cref="StyleNano"/>
    /// so the whole UI can be re-skinned at runtime from the options menu.
    /// </summary>
    public sealed class UiPalette
    {
        /// <summary>Stable id stored in the <c>ui.theme_palette</c> CVar.</summary>
        public string Id { get; }

        /// <summary>Fluent localization id for the options dropdown label.</summary>
        public string NameLoc { get; }

        /// <summary>Primary accent: headings, dividers, borders (maps to NanoGold / PipBoyGreen).</summary>
        public Color Accent { get; }

        /// <summary>Secondary/dim text (maps to PipBoyGreenDim).</summary>
        public Color AccentDim { get; }

        /// <summary>Themed panel fill (maps to PipBoyPanelBg).</summary>
        public Color PanelBg { get; }

        public Color BtnDefault { get; }
        public Color BtnHovered { get; }
        public Color BtnPressed { get; }
        public Color BtnDisabled { get; }

        public UiPalette(
            string id,
            string nameLoc,
            Color accent,
            Color accentDim,
            Color panelBg,
            Color btnDefault,
            Color btnHovered,
            Color btnPressed,
            Color btnDisabled)
        {
            Id = id;
            NameLoc = nameLoc;
            Accent = accent;
            AccentDim = accentDim;
            PanelBg = panelBg;
            BtnDefault = btnDefault;
            BtnHovered = btnHovered;
            BtnPressed = btnPressed;
            BtnDisabled = btnDisabled;
        }

        public const string DefaultId = "pipboy_green";

        /// <summary>Selectable palettes, in the order shown by the options dropdown.</summary>
        public static readonly IReadOnlyList<UiPalette> All = new[]
        {
            // Default: phosphor-green Pip-Boy terminal.
            new UiPalette("pipboy_green", "ui-theme-pipboy-green",
                Color.FromHex("#33FF66"), Color.FromHex("#1E9C3D"), Color.FromHex("#0C1F0E"),
                Color.FromHex("#163E1E"), Color.FromHex("#205A2C"), Color.FromHex("#2E7D3F"), Color.FromHex("#10240F")),

            // Modern dark mode: neutral grays, soft low-saturation blue accent, easy on the eyes.
            new UiPalette("dark", "ui-theme-dark",
                Color.FromHex("#8FB3D0"), Color.FromHex("#6A8298"), Color.FromHex("#1A1B1E"),
                Color.FromHex("#2A2C30"), Color.FromHex("#3A3D42"), Color.FromHex("#4A6075"), Color.FromHex("#202225")),

            // Amber CRT terminal.
            new UiPalette("amber", "ui-theme-amber",
                Color.FromHex("#FFB642"), Color.FromHex("#B36A1E"), Color.FromHex("#1F1405"),
                Color.FromHex("#3E2A0E"), Color.FromHex("#5A3E14"), Color.FromHex("#7D5A1E"), Color.FromHex("#241805")),

            // Vault-Tec blue.
            new UiPalette("vault_blue", "ui-theme-vault-blue",
                Color.FromHex("#4FC3F7"), Color.FromHex("#2A7CA8"), Color.FromHex("#0A1622"),
                Color.FromHex("#15304A"), Color.FromHex("#1E4A6B"), Color.FromHex("#2E6B9E"), Color.FromHex("#0E2030")),

            // Classic stock SS14 Nano look (gold accent, gray buttons) — turns the Fallout theme off.
            new UiPalette("classic", "ui-theme-classic",
                Color.FromHex("#A88B5E"), Color.FromHex("#8A7049"), Color.FromHex("#1E1E22"),
                Color.FromHex("#464950"), Color.FromHex("#575B61"), Color.FromHex("#3E6C45"), Color.FromHex("#292929")),
        };

        /// <summary>Resolves a palette by id, falling back to the default if unknown.</summary>
        public static UiPalette GetById(string id)
        {
            foreach (var palette in All)
            {
                if (palette.Id == id)
                    return palette;
            }

            return All[0];
        }
    }
}
