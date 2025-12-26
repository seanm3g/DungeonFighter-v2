using RPGGame;
using RPGGame.UI.Avalonia.Renderers.Layout;
using RPGGame.UI.Avalonia.Managers;
using System;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Helper for rendering menu screens with consistent patterns.
    /// Extracts menu screen rendering logic from CanvasRenderer to reduce duplication.
    /// </summary>
    public class MenuScreenRenderingHelper
    {
        private readonly GameCanvasControl canvas;
        private readonly LayoutCoordinator layoutCoordinator;

        public MenuScreenRenderingHelper(GameCanvasControl canvas, LayoutCoordinator layoutCoordinator)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.layoutCoordinator = layoutCoordinator ?? throw new ArgumentNullException(nameof(layoutCoordinator));
        }

        /// <summary>
        /// Renders a menu screen with the given title and content renderer.
        /// </summary>
        /// <param name="title">The screen title</param>
        /// <param name="renderContent">Action to render the content</param>
        public void RenderMenuScreen(string title, Action<int, int, int, int> renderContent)
        {
            RenderWithLayout(null, title, renderContent, new CanvasContext());
        }

        /// <summary>
        /// Renders with layout using the layout coordinator.
        /// </summary>
        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context)
        {
            layoutCoordinator.RenderWithLayout(character, title, renderContent, context, null, null, null);
        }
    }
}

