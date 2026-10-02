using System;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Help;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Canvas fallback help screen. Primary play-time help is the Avalonia overlay
    /// toggled from <c>MainWindow</c> (H); this renderer keeps the same catalog if
    /// the canvas path is invoked.
    /// </summary>
    public class HelpSystemRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly System.Action clearCanvasAction;
        private bool showHelp = false;

        public HelpSystemRenderer(GameCanvasControl canvas, System.Action clearCanvasAction)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.clearCanvasAction = clearCanvasAction ?? throw new ArgumentNullException(nameof(clearCanvasAction));
        }

        public bool ToggleHelp()
        {
            showHelp = !showHelp;
            return showHelp;
        }

        public bool IsHelpVisible => showHelp;

        public void HideHelp() => showHelp = false;

        public void RenderHelp()
        {
            clearCanvasAction();

            canvas.AddTitle(2, "HELP - HOTKEYS", AsciiArtAssets.Colors.White);

            string[] lines = HotkeyHelpCatalog.FormatHelpBody()
                .Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

            int y = 5;
            foreach (string line in lines)
            {
                Color color = line.Length > 0 && line == line.ToUpperInvariant() && line.Trim().Length > 0
                    && !line.StartsWith("  ", System.StringComparison.Ordinal)
                    ? AsciiArtAssets.Colors.Yellow
                    : AsciiArtAssets.Colors.White;
                if (line.StartsWith("Press H", System.StringComparison.OrdinalIgnoreCase))
                    color = AsciiArtAssets.Colors.Gray;

                canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 2, y++, line, color);
                if (y >= CanvasLayoutManager.SCREEN_HEIGHT - 3)
                    break;
            }

            canvas.AddText(
                CanvasLayoutManager.LEFT_MARGIN + 2,
                Math.Min(y + 1, CanvasLayoutManager.SCREEN_HEIGHT - 2),
                "Press H or Esc to close",
                AsciiArtAssets.Colors.Gray);

            HelpFooterHintRenderer.Render(canvas);
            canvas.Refresh();
        }
    }
}
