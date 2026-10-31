using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Config.TextDelay;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Types mechanical combat-log lines in place, character by character, with the same
    /// sentence → word → character budget the F7 narrative log uses. Each line's budget is one
    /// <c>MessageDelayMs</c>, so typing a line takes the gap the old line-by-line reveal waited.
    /// </summary>
    public static class CombatLogLineTypewriter
    {
        /// <summary>Delays shorter than this are batched into one paint (about one frame).</summary>
        private const int MinPaintStepMs = 16;

        /// <summary>
        /// Grows the reserved row at <paramref name="offsetFromEnd"/> from <paramref name="fromLength"/>
        /// visible characters up to the whole of <paramref name="line"/>.
        /// </summary>
        public static async Task TypeLineFromEndAsync(
            CanvasUICoordinator coordinator,
            int offsetFromEnd,
            List<ColoredText> line,
            int fromLength,
            Character? character,
            UIMessageType messageType)
        {
            string plain = ColoredTextRenderer.RenderAsPlainText(line);
            int from = Math.Clamp(fromLength, 0, plain.Length);
            if (from >= plain.Length || DeveloperModeState.IsCombatLogInstant || CombatManager.DisableCombatUIOutput)
            {
                coordinator.ReplaceColoredSegmentsFromEnd(offsetFromEnd, line, character, messageType);
                return;
            }

            int[] schedule = CharacterRevealRhythmCalculator.BuildParagraphSchedule(
                plain, CombatDelayManager.Config.MessageDelayMs);

            int pendingMs = 0;
            for (int len = from + 1; len <= plain.Length; len++)
            {
                pendingMs += schedule.Length >= len ? schedule[len - 1] : 0;
                if (len < plain.Length && pendingMs < MinPaintStepMs)
                    continue;

                coordinator.ReplaceColoredSegmentsFromEnd(
                    offsetFromEnd, ColoredTextRenderer.Truncate(line, len), character, messageType);
                if (len < plain.Length && pendingMs > 0)
                    await CombatDelayManager.DelayAfterNarrativeScheduledMsAsync(pendingMs);
                pendingMs = 0;
            }
        }

        /// <summary>Appends each group as a new row and types it out; waits <paramref name="delayAfterBatchMs"/> after the last.</summary>
        public static async Task TypeMessageGroupsAsync(
            CanvasUICoordinator coordinator,
            List<(List<ColoredText> segments, UIMessageType messageType)> groups,
            int delayAfterBatchMs,
            Character? character)
        {
            foreach (var (segments, messageType) in groups)
            {
                if (segments == null)
                    continue;

                coordinator.WriteColoredSegmentsBatch(
                    new List<(List<ColoredText> segments, UIMessageType messageType)> { (new List<ColoredText>(), messageType) },
                    0,
                    character);
                await TypeLineFromEndAsync(coordinator, 0, segments, 0, character, messageType);
            }

            int scaledDelayAfterBatchMs = DeveloperModeState.ScaleDelayMs(delayAfterBatchMs);
            if (scaledDelayAfterBatchMs > 0)
                await Task.Delay(scaledDelayAfterBatchMs);
        }
    }
}
