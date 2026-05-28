using RPGGame;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Renders the THRESHOLDS section body (ladder numbers or CHANCES %) for hero and enemy panels.
    /// The colored d20 bar is rendered under the health bar separately.
    /// </summary>
    public static class ThresholdSectionRenderer
    {
        public readonly struct RenderResult
        {
            public RenderResult(int nextY, ThresholdDisplayFormatting.D20OutcomeSegment[]? barSegments, ThresholdDisplayFormatting.D20ChanceDisplayRow[]? chanceHoverOrder)
            {
                NextY = nextY;
                BarSegments = barSegments;
                ChanceHoverOrder = chanceHoverOrder;
            }

            public int NextY { get; }
            public ThresholdDisplayFormatting.D20OutcomeSegment[]? BarSegments { get; }
            public ThresholdDisplayFormatting.D20ChanceDisplayRow[]? ChanceHoverOrder { get; }
        }

        public static RenderResult Render(
            GameCanvasControl canvas,
            int x,
            int y,
            int barWidth,
            Actor actor,
            StatsPanelStateManager? stateManager,
            ThresholdBarPanel panel = ThresholdBarPanel.Hero)
        {
            var hudMode = stateManager?.ThresholdsHudMode ?? ThresholdsHudMode.Ladder;
            bool showChances = hudMode == ThresholdsHudMode.Chances;
            bool flashChances = stateManager != null && stateManager.IsThresholdsChancesFlashActive();
            int nextY = DiceRollThresholdRowsRenderer.RenderRows(
                canvas,
                x,
                y,
                actor,
                showChances,
                flashChances,
                out var chanceHoverOrder);
            return new RenderResult(nextY, null, chanceHoverOrder);
        }
    }
}
