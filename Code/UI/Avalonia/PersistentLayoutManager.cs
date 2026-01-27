using Avalonia.Media;
using RPGGame;
using System;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Effects;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Manages the persistent layout structure where character info is always visible
    /// and only the center content area changes.
    /// Facade coordinator that delegates to specialized layout components.
    /// </summary>
    public class PersistentLayoutManager
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        
        // Specialized layout components using composition pattern
        private readonly LayoutCoordinator layoutCoordinator;
        private readonly CharacterPanelRenderer characterPanelRenderer;
        private readonly RightPanelRenderer rightPanelRenderer;
        
        public PersistentLayoutManager(
            GameCanvasControl canvas,
            ICanvasInteractionManager? interactionManager = null,
            StatsPanelStateManager? statsPanelStateManager = null,
            StatsHeaderGlowAnimator? glowAnimator = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.textWriter = new ColoredTextWriter(canvas);
            
            // Initialize specialized components
            this.layoutCoordinator = new LayoutCoordinator(canvas);
            this.characterPanelRenderer = new CharacterPanelRenderer(
                canvas, 
                textWriter, 
                statsPanelStateManager, 
                glowAnimator,
                interactionManager);
            this.rightPanelRenderer = new RightPanelRenderer(canvas);
        }
        
        /// <summary>
        /// Renders the complete persistent layout with character info and dynamic content
        /// </summary>
        /// <param name="clearCanvas">Whether to clear the canvas before rendering. Set to false to preserve existing content when transitioning to combat.</param>
        public void RenderLayout(Character? character, Action<int, int, int, int> renderCenterContent, string title = "DUNGEON FIGHTERS", Enemy? enemy = null, string? dungeonName = null, string? roomName = null, bool clearCanvas = true)
        {
            layoutCoordinator.CoordinateLayout(
                character,
                renderCenterContent,
                title,
                enemy,
                dungeonName,
                roomName,
                clearCanvas,
                character,
                characterPanelRenderer,
                rightPanelRenderer);
        }
        
        /// <summary>
        /// Gets the center content area dimensions
        /// Returns the same coordinates that RenderLayout uses for consistency
        /// </summary>
        public (int x, int y, int width, int height) GetCenterContentArea()
        {
            return (LayoutConstants.CENTER_PANEL_X + 1, LayoutConstants.CENTER_PANEL_Y + 1, LayoutConstants.CENTER_PANEL_WIDTH - 2, LayoutConstants.CENTER_PANEL_HEIGHT - 2);
        }
    }
}

