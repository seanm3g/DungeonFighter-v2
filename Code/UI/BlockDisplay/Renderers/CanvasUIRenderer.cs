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
                var dump = SetupPunchlineReservation.BuildInitialDump(setup, headlineType, reserved);
                // Sync dump: setup + blank placeholders in one pass so one scroll happens up front.
                coordinator.WriteColoredSegmentsBatch(dump, 0, character);

                if (betweenBeats != null)
                    await betweenBeats();
                else if (halfDelayMs > 0)
                    await Task.Delay(halfDelayMs);

                int n = reserved.Count;
                coordinator.ReplaceColoredSegmentsFromEnd(
                    SetupPunchlineReservation.HeadlineOffsetFromEnd(n),
                    completeHeadline,
                    character,
                    headlineType);
                PunchlineRevealFeedback.CommitQueued();

                for (int i = 0; i < n; i++)
                {
                    var (segments, messageType) = reserved[i];
                    coordinator.ReplaceColoredSegmentsFromEnd(
                        SetupPunchlineReservation.FollowUpOffsetFromEnd(n, i),
                        segments,
                        character,
                        messageType);

                    if (i < n - 1)
                        await CombatDelayManager.DelayAfterMessageAsync();
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
