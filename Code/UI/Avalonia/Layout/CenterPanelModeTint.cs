using Avalonia.Media;
using RPGGame;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Provides the center-panel frame colors that reflect runtime combat pacing mode.
    /// </summary>
    public static class CenterPanelModeTint
    {
        private static readonly Color NormalBackground = Colors.Black;
        private static readonly Color FastCombatBackground = Color.FromRgb(7, 4, 22);

        public static Color GetBackgroundColor() =>
            GetBackgroundColor(DeveloperModeState.IsCombatSpeedAccelerated);

        public static Color GetBackgroundColor(bool combatSpeedAccelerated) =>
            combatSpeedAccelerated ? FastCombatBackground : NormalBackground;

        public static void RenderFrame(GameCanvasControl canvas)
        {
            if (TryUpdateExistingFrame(canvas))
                return;

            canvas.AddBox(
                LayoutConstants.CENTER_PANEL_X,
                LayoutConstants.CENTER_PANEL_Y,
                LayoutConstants.CENTER_PANEL_WIDTH,
                LayoutConstants.CENTER_PANEL_HEIGHT,
                AsciiArtAssets.Colors.Cyan,
                GetBackgroundColor());
        }

        public static bool TryUpdateExistingFrame(GameCanvasControl canvas) =>
            canvas.TryUpdateBox(
                LayoutConstants.CENTER_PANEL_X,
                LayoutConstants.CENTER_PANEL_Y,
                LayoutConstants.CENTER_PANEL_WIDTH,
                LayoutConstants.CENTER_PANEL_HEIGHT,
                AsciiArtAssets.Colors.Cyan,
                GetBackgroundColor());
    }
}
