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
        
        public void RenderMessageGroups(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs)
        {
            try
            {
                if (groups != null && groups.Count > 0)
                {
                    coordinator.WriteColoredSegmentsBatch(groups, delayMs);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in CanvasUIRenderer.RenderMessageGroups: {ex.Message}");
            }
        }
        
        public Task RenderMessageGroupsAsync(List<(List<ColoredText> segments, UIMessageType messageType)> groups, int delayMs)
        {
            try
            {
                if (groups != null && groups.Count > 0)
                {
                    return coordinator.WriteColoredSegmentsBatchAsync(groups, delayMs);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow combat to continue
                System.Diagnostics.Debug.WriteLine($"Error in CanvasUIRenderer.RenderMessageGroupsAsync: {ex.Message}");
            }
            return Task.CompletedTask;
        }
    }
}

