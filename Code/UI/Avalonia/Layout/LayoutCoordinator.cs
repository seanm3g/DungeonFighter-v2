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
                
                // Always clear the entire title line before rendering new title to prevent overlay
                // This is critical when transitioning between screens with different title lengths
                // Clear a wider range to ensure we remove any partial text from longer titles
                canvas.ClearTextInRange(LayoutConstants.TITLE_Y, LayoutConstants.TITLE_Y);
                
                // Render title bar
                canvas.AddTitle(LayoutConstants.TITLE_Y, title, AsciiArtAssets.Colors.Gold);
                
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
                // When not clearing, only update title if it changed, and update panels that need updating
                // Don't re-render center panel border - preserve existing content
                // Clear the title line before updating to prevent overlay from different title lengths
                if (titleChanged)
                {
                    canvas.ClearTextInRange(LayoutConstants.TITLE_Y, LayoutConstants.TITLE_Y);
                }
                canvas.AddTitle(LayoutConstants.TITLE_Y, title, AsciiArtAssets.Colors.Gold);
                
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
            ScrollDebugLogger.Log($"PersistentLayoutManager: About to invoke renderCenterContent with x={centerX}, y={centerY}, width={centerW}, height={centerH}");
            renderCenterContent?.Invoke(centerX, centerY, centerW, centerH);
            ScrollDebugLogger.Log($"PersistentLayoutManager: renderCenterContent invoked");
            
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

