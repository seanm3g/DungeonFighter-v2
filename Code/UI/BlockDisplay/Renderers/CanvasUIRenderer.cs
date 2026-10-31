using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.BlockDisplay.Renderers
{
    /// <summary>
    /// Renderer for CanvasUICoordinator - uses batch rendering
    /// </summary>
    public class CanvasUIRenderer : IBlockRenderer
    {
        private readonly CanvasUICoordinator coordinator;
        
        public CanvasUIRenderer(CanvasUICoordinator coordinator)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }
        
        public void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null)
        {
            try
            {
                if (groups != null && groups.Count > 0)
                {
                    coordinator.WriteColoredSegmentsBatch(groups, delayMs, character);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in CanvasUIRenderer.RenderMessageGroups: {ex.Message}");
            }
        }
        
        public Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null)
        {
            try
            {
                if (groups != null && groups.Count > 0)
                {
                    return coordinator.WriteColoredSegmentsBatchAsync(groups, delayMs, character);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in CanvasUIRenderer.RenderMessageGroupsAsync: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public async Task RenderSetupPunchlineAsync(
            List<ColoredText> setup,
            List<ColoredText> completeHeadline,
            List<(List<ColoredText> segments, UIMessageType messageType)> followUps,
            int halfDelayMs,
            Character? character,
            UIMessageType headlineType,
            Func<Task>? betweenBeats = null)
        {
            try
            {
                var reserved = SetupPunchlineReservation.NonNullFollowUps(followUps);
                // Sync dump: blank headline + blank placeholders in one pass so one scroll happens up front.
                var dump = SetupPunchlineReservation.BuildInitialDump(new List<ColoredText>(), headlineType, reserved);
                coordinator.WriteColoredSegmentsBatch(dump, 0, character);

                int n = reserved.Count;
                int headlineOffset = SetupPunchlineReservation.HeadlineOffsetFromEnd(n);
                await CombatLogLineTypewriter.TypeLineFromEndAsync(
                    coordinator, headlineOffset, setup, 0, character, headlineType);

                if (betweenBeats != null)
                    await betweenBeats();
                else if (halfDelayMs > 0)
                    await Task.Delay(halfDelayMs);

                // Setup is a prefix of the complete headline: the punchline types on after it.
                int setupLength = ColoredTextRenderer.RenderAsPlainText(setup).Length;
                await CombatLogLineTypewriter.TypeLineFromEndAsync(
                    coordinator, headlineOffset, completeHeadline, setupLength, character, headlineType);
                PunchlineRevealFeedback.CommitQueued();

                // Typing each line takes one MessageDelayMs, standing in for the old gap between lines.
                for (int i = 0; i < n; i++)
                {
                    var (segments, messageType) = reserved[i];
                    await CombatLogLineTypewriter.TypeLineFromEndAsync(
                        coordinator,
                        SetupPunchlineReservation.FollowUpOffsetFromEnd(n, i),
                        segments,
                        0,
                        character,
                        messageType);
                }

                if (halfDelayMs > 0)
                    await Task.Delay(halfDelayMs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CanvasUIRenderer.RenderSetupPunchlineAsync: {ex.Message}");
            }
        }
    }
}
