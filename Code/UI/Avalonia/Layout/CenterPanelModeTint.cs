using Avalonia.Media;
using RPGGame;
using RPGGame.Combat.Sequence;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Provides the center-panel frame colors that reflect runtime combat pacing mode.
    /// During combat also owns the two-row sequence HUD panel above the combat log.
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
            EnsureFrames(canvas);
        }

        public static bool TryUpdateExistingFrame(GameCanvasControl canvas)
        {
            bool log = canvas.TryUpdateBox(
                LayoutConstants.CENTER_PANEL_X,
                LayoutConstants.CENTER_PANEL_Y,
                LayoutConstants.CENTER_PANEL_WIDTH,
                LayoutConstants.CENTER_PANEL_HEIGHT,
                AsciiArtAssets.Colors.Cyan,
                GetBackgroundColor());
            bool sequence = !CombatSequenceHudState.IsBandReserved
                || canvas.TryUpdateBox(
                    LayoutConstants.CENTER_PANEL_X,
                    LayoutConstants.CombatSequencePanelY,
                    LayoutConstants.CENTER_PANEL_WIDTH,
                    LayoutConstants.CombatSequencePanelHeight,
                    AsciiArtAssets.Colors.Cyan,
                    GetBackgroundColor());
            return log && sequence;
        }

        /// <summary>
        /// Keeps the combat-log frame (and the sequence HUD panel in combat) aligned when
        /// <see cref="CombatSequenceHudState.IsBandReserved"/> changes without a full canvas clear.
        /// </summary>
        private static void EnsureFrames(GameCanvasControl canvas)
        {
            if (TryUpdateExistingFrame(canvas))
                return;

            int bandY = LayoutConstants.ACTION_INFO_STRIP_HEIGHT;
            int bandH = LayoutConstants.CombatSequenceBandHeight + LayoutConstants.CENTER_PANEL_HEIGHT;
            canvas.ClearBoxesInArea(
                LayoutConstants.CENTER_PANEL_X,
                bandY,
                LayoutConstants.CENTER_PANEL_WIDTH,
                bandH);

            if (CombatSequenceHudState.IsBandReserved)
            {
                canvas.AddBox(
                    LayoutConstants.CENTER_PANEL_X,
                    LayoutConstants.CombatSequencePanelY,
                    LayoutConstants.CENTER_PANEL_WIDTH,
                    LayoutConstants.CombatSequencePanelHeight,
                    AsciiArtAssets.Colors.Cyan,
                    GetBackgroundColor());
            }

            canvas.AddBox(
                LayoutConstants.CENTER_PANEL_X,
                LayoutConstants.CENTER_PANEL_Y,
                LayoutConstants.CENTER_PANEL_WIDTH,
                LayoutConstants.CENTER_PANEL_HEIGHT,
                AsciiArtAssets.Colors.Cyan,
                GetBackgroundColor());
        }
    }
}
