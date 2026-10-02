using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Help
{
    /// <summary>
    /// Draws <see cref="HotkeyHelpCatalog.FooterHint"/> in the outer bottom pad row,
    /// right-aligned to the layout content edge (flush with the right panel).
    /// </summary>
    public static class HelpFooterHintRenderer
    {
        public static void Render(GameCanvasControl canvas)
        {
            if (canvas == null)
                return;

            string hint = HotkeyHelpCatalog.FooterHint;
            canvas.AddText(
                HotkeyHelpCatalog.ResolveFooterHintStartColumn(),
                HotkeyHelpCatalog.FooterHintRow,
                hint,
                AsciiArtAssets.Colors.Gray);
        }
    }
}
