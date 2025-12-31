using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            // Fire and forget - use async version but don't wait
            _ = RenderMessageGroupsAsync(groups, delayMs, character);
        }
        
        public async Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs, Character? character = null)
        {
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
            if (delayMs > 0 && !CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
            {
                await Task.Delay(delayMs);
            }
        }
    }
}

