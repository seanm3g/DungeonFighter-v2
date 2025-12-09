using System;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Layout
{
    /// <summary>
    /// Coordinates layout rendering using PersistentLayoutManager
    /// Extracted from CanvasRenderer.cs RenderWithLayout() logic
    /// </summary>
    public class LayoutCoordinator
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasInteractionManager interactionManager;

        public LayoutCoordinator(GameCanvasControl canvas, ICanvasInteractionManager interactionManager)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.interactionManager = interactionManager ?? throw new ArgumentNullException(nameof(interactionManager));
        }

        /// <summary>
        /// Renders content with persistent layout
        /// </summary>
        public void RenderWithLayout(
            Character? character,
            string title,
            Action<int, int, int, int> renderContent,
            CanvasContext context,
            Enemy? enemy = null,
            string? dungeonName = null,
            string? roomName = null,
            bool clearCanvas = true)
        {
            interactionManager.ClearClickableElements();
            
            // Use the persistent layout manager for proper three-panel layout
            var layoutManager = new PersistentLayoutManager(canvas);
            layoutManager.RenderLayout(character, renderContent, title, enemy, dungeonName, roomName, clearCanvas);
        }
    }
}

