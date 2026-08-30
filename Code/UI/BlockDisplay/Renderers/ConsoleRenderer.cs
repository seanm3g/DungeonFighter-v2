using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.BlockDisplay.Renderers
{
    /// <summary>
    /// Renderer for console output - uses ColoredConsoleWriter
    /// </summary>
    public class ConsoleRenderer : IBlockRenderer
    {
        public void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null)
        {
            // Sync path dumps lines without pacing. Combat must use RenderMessageGroupsAsync.
            if (UIManager.DisableAllUIOutput || groups == null || groups.Count == 0)
                return;

            foreach (var (segments, _) in groups)
            {
                if (segments == null)
                    continue;
                if (segments.Count == 0)
                    Console.WriteLine();
                else
                {
                    ColoredConsoleWriter.WriteSegments(segments);
                    Console.WriteLine();
                }
            }
        }
        
        public async Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null)
        {
            // Skip rendering if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            if (groups != null && groups.Count > 0)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    var (segments, messageType) = groups[i];
                    
                    if (segments != null)
                    {
                        if (segments.Count == 0)
                        {
                            // Empty segments are treated as blank lines
                            Console.WriteLine();
                        }
                        else
                        {
                            ColoredConsoleWriter.WriteSegments(segments);
                            Console.WriteLine();
                        }
                    }
                    
                    // Add delay between messages (but not after the last one - that's handled by delayMs)
                    if (i < groups.Count - 1)
                    {
                        // Use MessageDelayMs for delays between lines within an action block
                        if (!CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
                        {
                            await CombatDelayManager.DelayAfterMessageAsync();
                        }
                    }
                }
            }
            
            // Apply final delay after the entire batch (delay between action blocks)
            int scaledDelayMs = DeveloperModeState.ScaleDelayMs(delayMs);
            if (scaledDelayMs > 0 && !CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
            {
                await Task.Delay(scaledDelayMs);
            }
        }

        public async Task RenderSetupPunchlineAsync(
            List<ColoredText> setup,
            List<ColoredText> completeHeadline,
            List<(List<ColoredText> segments, UIMessageType messageType)> followUps,
            int halfDelayMs,
            Character? character,
            UIMessageType headlineType)
        {
            _ = setup;
            _ = character;
            _ = headlineType;

            if (UIManager.DisableAllUIOutput)
            {
                PunchlineRevealFeedback.CommitQueued();
                return;
            }

            if (halfDelayMs > 0)
                await Task.Delay(halfDelayMs);

            if (completeHeadline != null && completeHeadline.Count > 0)
            {
                ColoredConsoleWriter.WriteSegments(completeHeadline);
                Console.WriteLine();
            }

            PunchlineRevealFeedback.CommitQueued();

            if (followUps != null && followUps.Count > 0)
            {
                for (int i = 0; i < followUps.Count; i++)
                {
                    var (segments, _) = followUps[i];
                    if (segments == null)
                        continue;
                    if (segments.Count == 0)
                        Console.WriteLine();
                    else
                    {
                        ColoredConsoleWriter.WriteSegments(segments);
                        Console.WriteLine();
                    }

                    if (i < followUps.Count - 1 && !CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
                        await CombatDelayManager.DelayAfterMessageAsync();
                }
            }

            if (halfDelayMs > 0 && !CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
                await Task.Delay(halfDelayMs);
        }
    }
}

