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
    /// 
    /// IMPORTANT: This is the SINGLE POINT for canvas clearing in layout operations.
    /// All canvas clearing for layout-based rendering goes through this coordinator.
    /// The canvas.Clear() call at line 52 is the ONLY direct canvas clearing allowed
    /// outside of GameCanvasControl itself and specialized full-screen renderers
    /// (MessageDisplayRenderer, HelpSystemRenderer) which use clearing actions.
    /// 
    /// Clearing Strategy:
    /// - When clearCanvas=true: Clears canvas immediately before rendering to prevent flicker
    /// - When clearCanvas=false: Preserves existing content, only updates what's needed
    /// - ScreenTransitionProtocol should pass clearCanvas=false since it handles clearing
    /// - Direct render calls should use clearCanvas=true for clean transitions
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
            // Check if title changed - used for border rendering, not for clearing decisions
            // Clearing is now handled explicitly by ScreenTransitionProtocol, so we respect
            // the clearCanvas parameter directly without title-change detection interfering
            bool titleChanged = title != lastRenderedTitle;
            
            // Respect the clearCanvas parameter directly - no automatic clearing based on title changes
            // This prevents unexpected clears with dynamic titles and ensures explicit control
            
            if (clearCanvas)
            {
                // SINGLE CLEARING POINT: This is the only place LayoutCoordinator clears the canvas
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
                
                // NOTE: No need to call ClearTextInArea here because canvas.Clear() at line 52
                // already removed ALL text elements. The additional ClearTextInArea was redundant
                // and could potentially interfere with content that's about to be rendered.
                // Content renderers are responsible for managing their own text elements.
            }
            else
            {
                // When not clearing, only update panels that need updating
                // Title rendering removed - panels now extend to top of frame
                
                // Update left panel (Character Info) - may have changed
                if (character != null)
                {
                    characterPanelRenderer.RenderCharacterPanel(character);
                }
                
                // Only render center panel border if title changed (transitioning from different screen)
                // This ensures the border is visible for menu screens that use clearCanvas: false
                // but prevents double-drawing when the same screen is re-rendered
                if (titleChanged)
                {
                    canvas.AddBorder(LayoutConstants.CENTER_PANEL_X, LayoutConstants.CENTER_PANEL_Y, LayoutConstants.CENTER_PANEL_WIDTH, LayoutConstants.CENTER_PANEL_HEIGHT, AsciiArtAssets.Colors.Cyan);
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

