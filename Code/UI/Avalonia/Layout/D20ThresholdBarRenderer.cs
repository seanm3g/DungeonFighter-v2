using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Renders the d20 threshold outcome bar (colored segments with black dividers).
    /// </summary>
    public static class D20ThresholdBarRenderer
    {
        public const double CombatHealthHeightScale = 1.0;
        public const double CombatStripHeightScale = 0.5;

        /// <summary>Combat board's d20 strip: thicker, eating half the gap row next to its health bar.</summary>
        public const double CombatBoardStripHeightScale = 1.5;

        /// <summary>Grid rows cleared for the combat bar block (health 1.0 + threshold 0.5).</summary>
        public const int CombatBarAreaRowCount = 2;

        public const double CombatStripVerticalOffsetNoArmor = CombatHealthHeightScale;

        public static ThresholdDisplayFormatting.D20OutcomeSegment[] RenderBar(
            GameCanvasControl canvas,
            int x,
            int y,
            int barWidth,
            Actor actor,
            ThresholdBarPanel panel = ThresholdBarPanel.Hero,
            double heightScale = CombatStripHeightScale,
            double verticalOffsetScale = 0.0,
            RPGGame.Combat.Sequence.CombatSequenceThresholds? thresholds = null,
            System.Func<int?>? rollMarker = null)
        {
            var bands = thresholds ?? CurrentThresholds(actor);
            var segments = ThresholdDisplayFormatting.BuildD20OutcomeSegments(
                bands.Crit,
                bands.Combo,
                bands.MinHit,
                bands.CritMiss);

            canvas.AddSegmentedBar(
                x,
                y,
                barWidth,
                segments,
                Colors.White,
                Colors.Black,
                segmentHighlight: (segmentIndex, baseColor) =>
                    ThresholdBarFeedback.TryGetSegmentHighlight(panel, segmentIndex, out var highlight)
                        ? highlight
                        : baseColor,
                heightScale: heightScale,
                verticalOffsetScale: verticalOffsetScale,
                rollMarkerRoll: rollMarker ?? (() =>
                    ThresholdBarFeedback.TryGetRollMarker(panel, out int roll) ? roll : null));
            return segments;
        }

        /// <summary>The bands the bar shows for <paramref name="actor"/> right now (effective, with HUD shifts).</summary>
        public static RPGGame.Combat.Sequence.CombatSequenceThresholds CurrentThresholds(Actor actor)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(actor);
            return new RPGGame.Combat.Sequence.CombatSequenceThresholds(
                snapshot.EffectiveCrit, snapshot.EffectiveCombo, snapshot.EffectiveHit, snapshot.EffectiveCritMiss);
        }

        /// <summary>Maps segment face counts to approximate grid-column hover widths.</summary>
        public static int[] GetSegmentHoverWidths(int barWidth, ThresholdDisplayFormatting.D20OutcomeSegment[] segments)
        {
            var widths = new int[segments.Length];
            int assigned = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                if (i == segments.Length - 1)
                    widths[i] = Math.Max(1, barWidth - assigned);
                else
                {
                    widths[i] = Math.Max(1, (int)Math.Round(barWidth * segments[i].FaceCount / 20.0));
                    assigned += widths[i];
                }
            }
            return widths;
        }
    }
}
