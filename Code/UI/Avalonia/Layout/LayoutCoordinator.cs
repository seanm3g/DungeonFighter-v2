using System;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Layout
{

    /// <summary>
    /// Coordinates layout rendering operations.
    /// Handles layout state management and title rendering.
    /// </summary>
    public class LayoutCoordinator
    {
        private readonly GameCanvasControl canvas;
        private string lastRenderedTitle = "";
        
        public LayoutCoordinator(GameCanvasControl canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }
        
        /// <summary>
        /// Coordinates the complete layout rendering
        /// </summary>
        public void CoordinateLayout(
            Character? character,
            Action<int, int, int, int> renderCenterContent,
            string title,
            Enemy? enemy,
            string? dungeonName,
            string? roomName,
            bool clearCanvas,
            Character? characterForRightPanel,
            CharacterPanelRenderer characterPanelRenderer,
            RightPanelRenderer rightPanelRenderer)
        {
            // Check if title changed - this determines if we need to clear canvas
            bool titleChanged = title != lastRenderedTitle;
            if (titleChanged)
            {
                clearCanvas = true; // Force full render when title changes
            }
            
            if (clearCanvas)
            {
                // Clear canvas right before rendering to prevent blank frame flicker
                // This ensures we clear and immediately render, minimizing visible blank time
                canvas.Clear();
                
                // Title rendering removed - panels now extend to top of frame
                
                // Render left panel (Character Info) - Always visible
                if (character != null)
                {
                    characterPanelRenderer.RenderCharacterPanel(character);
                }
                else
                {
                    characterPanelRenderer.RenderEmptyCharacterPanel();
                }
                
                // Render center panel border (only when clearing)
                canvas.AddBorder(LayoutConstants.CENTER_PANEL_X, LayoutConstants.CENTER_PANEL_Y, LayoutConstants.CENTER_PANEL_WIDTH, LayoutConstants.CENTER_PANEL_HEIGHT, AsciiArtAssets.Colors.Cyan);
                
                
                // Explicitly clear the center panel content area to ensure clean rendering
                // This prevents old content from showing when transitioning between screens
                int centerContentX = LayoutConstants.CENTER_PANEL_X + 1;
                int centerContentY = LayoutConstants.CENTER_PANEL_Y + 1;
                int centerContentWidth = LayoutConstants.CENTER_PANEL_WIDTH - 2;
                int centerContentHeight = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
                // Clear with height + 1 to ensure we clear the full area (endY is exclusive)
                canvas.ClearTextInArea(centerContentX, centerContentY, centerContentWidth, centerContentHeight + 1);
                canvas.ClearProgressBarsInArea(centerContentX, centerContentY, centerContentWidth, centerContentHeight);
            }
            else
            {
                // When not clearing, only update panels that need updating
                // Don't re-render center panel border - preserve existing content
                // Title rendering removed - panels now extend to top of frame
                
                // Update left panel (Character Info) - may have changed
                if (character != null)
                {
                    characterPanelRenderer.RenderCharacterPanel(character);
                }
                
                // Note: We do NOT clear the center content area here when clearCanvas=false
                // The DisplayRenderer (or other content renderer) is responsible for clearing
                // its own content area before rendering. This avoids redundant clearing and
                // ensures proper separation of concerns.
            }
            
            // Track the last rendered title
            lastRenderedTitle = title;
            
            // Always call the content renderer for the center area
            // When clearCanvas is false, this will render from display buffer which contains all content
            int centerX = LayoutConstants.CENTER_PANEL_X + 1;
            int centerY = LayoutConstants.CENTER_PANEL_Y + 1;
            int centerW = LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int centerH = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            renderCenterContent?.Invoke(centerX, centerY, centerW, centerH);
            // Render right panel (Dungeon/Enemy Info or Inventory Actions) - always update
            rightPanelRenderer.RenderRightPanel(enemy, dungeonName, roomName, title, characterForRightPanel);
            
            // Ensure refresh happens on UI thread and after all rendering is complete
            if (Dispatcher.UIThread.CheckAccess())
            {
                canvas.Refresh();
            }
            else
            {
                Dispatcher.UIThread.Post(() => canvas.Refresh());
            }
        }
    }
}

