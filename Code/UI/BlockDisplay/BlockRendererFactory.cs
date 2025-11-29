using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.BlockDisplay.Renderers;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Factory for creating appropriate block renderers based on the current UI backend
    /// </summary>
    public static class BlockRendererFactory
    {
        /// <summary>
        /// Gets the appropriate renderer for the current UI backend
        /// </summary>
        public static IBlockRenderer GetRenderer()
        {
            var customUIManager = UIManager.GetCustomUIManager();
            
            // Check for CanvasUICoordinator first (most common case for GUI)
            // Try multiple approaches to ensure type check works
            if (customUIManager != null)
            {
                // First try: pattern matching with fully qualified name
                if (customUIManager is CanvasUICoordinator canvasCoordinator1)
                {
                    return new CanvasUIRenderer(canvasCoordinator1);
                }
                
                // Second try: direct type check and cast
                var managerType = customUIManager.GetType();
                if (managerType.Name == "CanvasUICoordinator" && managerType.Namespace == "RPGGame.UI.Avalonia")
                {
                    var canvasCoordinator2 = customUIManager as CanvasUICoordinator;
                    if (canvasCoordinator2 != null)
                    {
                        return new CanvasUIRenderer(canvasCoordinator2);
                    }
                }
                
                // Fallback to generic UI renderer for other IUIManager implementations
                return new GenericUIRenderer(customUIManager);
            }
            else
            {
                return new ConsoleRenderer();
            }
        }
    }
}

