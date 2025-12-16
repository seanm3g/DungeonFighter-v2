using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.BlockDisplay.Renderers
{
    /// <summary>
    /// Renderer for generic UI managers - writes segments individually
    /// </summary>
    public class GenericUIRenderer : IBlockRenderer
    {
        private readonly IUIManager uiManager;
        
        public GenericUIRenderer(IUIManager uiManager)
        {
            this.uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
        }
        
        public void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs)
        {
            try
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
                                uiManager.WriteBlankLine();
                            }
                            else
                            {
                                uiManager.WriteColoredSegments(segments, messageType);
                            }
                        }
                    }
                }
                
                // Apply delay after all lines are written (skip if combat UI is disabled)
                if (!CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
                {
                    // Fire and forget - don't block sync method
                    _ = CombatDelayManager.DelayAfterMessageAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in GenericUIRenderer.RenderMessageGroups: {ex.Message}");
            }
        }
        
        public async Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs)
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
                            uiManager.WriteBlankLine();
                        }
                        else
                        {
                            uiManager.WriteColoredSegments(segments, messageType);
                        }
                    }
                }
            }
            
            // Apply delay after all lines are written (skip if combat UI is disabled)
            if (!CombatManager.DisableCombatUIOutput && UIManager.EnableDelays)
            {
                await CombatDelayManager.DelayAfterMessageAsync();
            }
        }
    }
}

