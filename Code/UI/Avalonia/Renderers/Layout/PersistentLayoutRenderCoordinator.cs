using System;
using RPGGame;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Layout
{
    /// <summary>
    /// Bridges full-screen rendering to <see cref="RPGGame.UI.Avalonia.PersistentLayoutManager"/> (persistent chrome + center content).
    /// Extracted from <see cref="RPGGame.UI.Avalonia.Renderers.CanvasRenderer"/> <c>RenderWithLayout</c> logic.
    /// </summary>
    public class PersistentLayoutRenderCoordinator
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasInteractionManager interactionManager;
        private readonly ICanvasTextManager? textManager;

        public PersistentLayoutRenderCoordinator(GameCanvasControl canvas, ICanvasInteractionManager interactionManager, ICanvasTextManager? textManager = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.interactionManager = interactionManager ?? throw new ArgumentNullException(nameof(interactionManager));
            this.textManager = textManager;
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
            bool clearCanvas = true,
            bool usePersistentChrome = true)
        {
            interactionManager.ClearClickableElements();

            StatsPanelStateManager? stats = null;
            if (textManager is CanvasTextManager ctm)
            {
                stats = ctm.DisplayManager?.StatsPanelStateManager;
            }

            var layoutManager = new PersistentLayoutManager(canvas, interactionManager, stats);
            layoutManager.RenderLayout(character, renderContent, title, enemy, dungeonName, roomName, clearCanvas, usePersistentChrome);
        }
    }
}

