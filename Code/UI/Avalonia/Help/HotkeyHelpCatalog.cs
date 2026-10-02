using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Help
{
    /// <summary>
    /// Single source of truth for play-time hotkeys shown in the H help overlay
    /// and related tests. Keeps footer hint copy and the overlay list in sync.
    /// </summary>
    public static class HotkeyHelpCatalog
    {
        public const string FooterHint = "Press H for Help";

        /// <summary>
        /// Character row of the outer bottom pad (one row below painted panel borders).
        /// </summary>
        public static int FooterHintRow => LayoutConstants.LEFT_PANEL_HEIGHT;

        /// <summary>
        /// Start column for the footer hint, right-aligned to the layout content edge
        /// (flush with the right panel; leaves the outer right pad empty).
        /// </summary>
        public static int ResolveFooterHintStartColumn() =>
            ResolveFooterHintStartColumn(
                LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH,
                FooterHint.Length);

        /// <summary>
        /// Pure layout helper for tests: right-align <paramref name="hintLength"/> chars
        /// so the last character sits just before <paramref name="layoutRightEdge"/>.
        /// </summary>
        public static int ResolveFooterHintStartColumn(int layoutRightEdge, int hintLength) =>
            Math.Max(0, layoutRightEdge - Math.Max(0, hintLength));

        public readonly record struct HotkeyEntry(string Keys, string Description, string Section);

        public static IReadOnlyList<HotkeyEntry> Entries { get; } = new HotkeyEntry[]
        {
            new("H", "Toggle this help overlay", "General"),
            new("Esc", "Back / cancel / close menus", "General"),
            new("1-9 / 0", "Choose numbered menu options", "General"),
            new("Enter / Space", "Confirm selection", "General"),
            new("Arrow keys", "Navigate menus and scroll lists", "General"),
            new("Shift/Ctrl + Up/Down", "Page-scroll lists and the combat log", "General"),
            new("F3", "Cycle game font: VT323 → Noplato Mono → Pixelzone → Bytesized → Courier New", "General"),
            new("Ctrl+/- / Cmd+/-", "Size the UI up/down (scales content; saved per font)", "General"),
            new("Ctrl+0 / Cmd+0", "Reset UI size to this font's default", "General"),
            new("Ctrl+Shift+0 / Cmd+Shift+0", "Save current UI size as this font's default", "General"),
            new("F6", "Toggle glyph distortion: mouse wind + click burst (default on)", "General"),

            new("Page Up / Page Down", "Combat speed ladder (1x → 2x → 5x → 20x)", "Combat"),
            new("F5", "Toggle narrative combat-log video overlay (default off)", "Combat"),
            new("F7", "Toggle narrative combat log (prose paragraphs)", "Combat"),
            new("Ctrl+C / Cmd+C", "Copy the full combat/dungeon text log as plain text", "Combat"),
            new("Right-click combat log", "Copy the full combat/dungeon text log as plain text", "Combat"),
            new("Mouse wheel (over combat log)", "Scroll the combat log", "Combat"),
            new("Mouse wheel (over left panel)", "Scroll the hero panel when it overflows", "General"),

            new("F8", "Main menu: open Action Lab (+ Settings layout)", "Tools"),

            new("Numpad +", "Inventory: cycle bag sort (order / rarity / slot)", "Inventory"),
            new("Numpad -", "Inventory: toggle hide unmet requirements", "Inventory"),
            new("Numpad *", "Inventory: auto-equip empty gear slots from bag", "Inventory"),
            new("Numpad /", "Inventory: cycle equipment slot filter", "Inventory"),
        };

        /// <summary>Plain multiline help body for the overlay (and canvas fallback).</summary>
        public static string FormatHelpBody()
        {
            var sb = new StringBuilder();
            string? lastSection = null;
            foreach (var entry in Entries)
            {
                if (!string.Equals(lastSection, entry.Section, StringComparison.Ordinal))
                {
                    if (lastSection != null)
                        sb.AppendLine();
                    sb.AppendLine(entry.Section.ToUpperInvariant());
                    lastSection = entry.Section;
                }

                sb.AppendLine($"  {entry.Keys,-28} {entry.Description}");
            }

            return sb.ToString().TrimEnd();
        }

        public static bool MentionsKey(string keyFragment) =>
            Entries.Any(e => e.Keys.Contains(keyFragment, StringComparison.OrdinalIgnoreCase));
    }
}
