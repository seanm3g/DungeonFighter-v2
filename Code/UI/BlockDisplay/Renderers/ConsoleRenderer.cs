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
        public void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs)
        {
            if (groups != null)
            {
                foreach (var (segments, messageType) in groups)
                {
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
                }
            }
            
            // Apply delay after all lines are written
            if (UIManager.EnableDelays)
            {
                CombatDelayManager.DelayAfterMessage();
            }
        }
        
        public Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs)
        {
            RenderMessageGroups(groups, delayMs);
            return Task.CompletedTask;
        }
    }
}

