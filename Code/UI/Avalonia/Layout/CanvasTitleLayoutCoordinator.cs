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
    public class CanvasTitleLayoutCoordinator
    {
        private readonly GameCanvasControl canvas;
        private string lastRenderedTitle = "";
        
        public CanvasTitleLayoutCoordinator(GameCanvasControl canvas)
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
            bool usePersistentChrome,
            Character? characterForRightPanel,
            CharacterPanelRenderer characterPanelRenderer,
            RightPanelRenderer rightPanelRenderer)
        {
            // Check if title changed - used for border rendering when persistent chrome is on
            bool titleChanged = title != lastRenderedTitle;
            
            if (usePersistentChrome)
            {
                if (clearCanvas)
                {
                    // SINGLE CLEARING POINT: This is the only place CanvasTitleLayoutCoordinator clears the canvas
                    canvas.Clear();
                    
                    // Render left panel (Character Info) - Always visible
                    if (character != null)
                    {
                        characterPanelRenderer.RenderCharacterPanel(character);
                    }
                    else
                    {
                        characterPanelRenderer.RenderEmptyCharacterPanel();
                    }
                    
                    canvas.AddBorder(LayoutConstants.CENTER_PANEL_X, LayoutConstants.CENTER_PANEL_Y, LayoutConstants.CENTER_PANEL_WIDTH, LayoutConstants.CENTER_PANEL_HEIGHT, AsciiArtAssets.Colors.Cyan);
                }
                else
                {
                    // When not clearing, only update panels that need updating
                    if (character != null)
                    {
                        characterPanelRenderer.RenderCharacterPanel(character);
                    }
                    else
                    {
                        // ScreenTransitionProtocol clears the canvas then renders with clearCanvas:false;
                        characterPanelRenderer.RenderEmptyCharacterPanel();
                    }
                    
                    if (titleChanged)
                    {
                        canvas.AddBorder(LayoutConstants.CENTER_PANEL_X, LayoutConstants.CENTER_PANEL_Y, LayoutConstants.CENTER_PANEL_WIDTH, LayoutConstants.CENTER_PANEL_HEIGHT, AsciiArtAssets.Colors.Cyan);
                    }
                }
            }
            else
            {
                // Chromeless (e.g. main menu, weapon selection): no side panels or frame borders
                if (clearCanvas)
                {
                    canvas.Clear();
                }
            }
            
            lastRenderedTitle = title;
            
            int contentX;
            int contentY;
            int contentW;
            int contentH;
            if (usePersistentChrome)
            {
                contentX = LayoutConstants.CENTER_PANEL_X + 1;
                contentY = LayoutConstants.CENTER_PANEL_Y + 1;
                contentW = LayoutConstants.CENTER_PANEL_WIDTH - 2;
                contentH = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            }
            else
            {
                (contentX, contentY, contentW, contentH) = LayoutConstants.GetChromelessContentRect();
            }
            
            renderCenterContent?.Invoke(contentX, contentY, contentW, contentH);
            
            if (usePersistentChrome)
            {
                rightPanelRenderer.RenderRightPanel(enemy, dungeonName, roomName, title, characterForRightPanel);
            }
            
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

