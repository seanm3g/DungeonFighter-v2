using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Specialized coordinator for handling utility operations
    /// </summary>
    public class UtilityCoordinator
    {
        private readonly GameCanvasControl canvas;
        private readonly CanvasRenderer renderer;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasContextManager contextManager;
        private readonly DisplayUpdateCoordinator displayUpdateCoordinator;
        
        public UtilityCoordinator(GameCanvasControl canvas, CanvasRenderer renderer, ICanvasTextManager textManager, ICanvasContextManager contextManager)
        {
            this.canvas = canvas;
            this.renderer = renderer;
            this.textManager = textManager;
            this.contextManager = contextManager;
            
            // Create DisplayUpdateCoordinator with display manager if available
            CenterPanelDisplayManager? displayManager = null;
            if (textManager is CanvasTextManager canvasTextManager)
            {
                displayManager = canvasTextManager.DisplayManager;
            }
            this.displayUpdateCoordinator = new DisplayUpdateCoordinator(canvas, textManager, displayManager);
        }
        
        /// <summary>
        /// Clears the canvas
        /// </summary>
        public void Clear()
        {
            displayUpdateCoordinator.Clear(DisplayUpdateCoordinator.ClearOperation.Canvas);
        }
        
        /// <summary>
        /// Refreshes the canvas
        /// </summary>
        public void Refresh()
        {
            displayUpdateCoordinator.Refresh();
        }
        
        /// <summary>
        /// Clears the display
        /// </summary>
        public void ClearDisplay()
        {
            textManager.ClearDisplay();
        }
        
        /// <summary>
        /// Shows a message
        /// </summary>
        public void ShowMessage(string message, Color color = default)
        {
            renderer.ShowMessage(message, color);
        }
        
        /// <summary>
        /// Shows an error message
        /// </summary>
        public void ShowError(string error)
        {
            renderer.ShowError(error);
        }
        
        /// <summary>
        /// Shows a success message
        /// </summary>
        public void ShowSuccess(string message)
        {
            renderer.ShowSuccess(message);
        }
        
        /// <summary>
        /// Shows loading animation
        /// </summary>
        public void ShowLoadingAnimation(string message = "Loading...")
        {
            renderer.ShowLoadingAnimation(message);
        }
        
        /// <summary>
        /// Shows detailed error with suggestion
        /// </summary>
        public void ShowError(string error, string suggestion = "")
        {
            renderer.ShowError(error, suggestion);
        }
        
        /// <summary>
        /// Updates status message
        /// </summary>
        public void UpdateStatus(string message)
        {
            renderer.UpdateStatus(message);
        }
        
        /// <summary>
        /// Shows an invalid key message at the bottom without clearing the screen
        /// </summary>
        public void ShowInvalidKeyMessage(string message)
        {
            renderer.ShowInvalidKeyMessage(message);
        }
        
        /// <summary>
        /// Toggles help display
        /// </summary>
        public void ToggleHelp()
        {
            renderer.ToggleHelp();
        }
        
        /// <summary>
        /// Renders help screen
        /// </summary>
        public void RenderHelp()
        {
            renderer.RenderHelp();
        }
        
        /// <summary>
        /// Shows press key message without clearing the existing display
        /// (preserves the title screen or other content)
        /// Applies the same -6 left shift as the title screen for consistency
        /// </summary>
        public void ShowPressKeyMessage()
        {
            // Don't clear - just add the message to the bottom of the existing display
            // This preserves the title screen that was just rendered
            const int globalLeftShift = -6; // Match the title screen left shift
            string message = "Press any key to continue...";
            
            // Calculate centered position and apply left shift
            var segments = ColoredTextParser.Parse(message);
            int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
            int centerX = Math.Max(0, canvas.CenterX - (displayLength / 2));
            centerX += globalLeftShift;
            
            canvas.AddText(centerX, 50, message, AsciiArtAssets.Colors.Gray);
            canvas.Refresh();
        }
        
        /// <summary>
        /// Resets delete confirmation state
        /// </summary>
        public void ResetDeleteConfirmation()
        {
            contextManager.ResetDeleteConfirmation();
        }
        
        /// <summary>
        /// Sets delete confirmation pending state
        /// </summary>
        public void SetDeleteConfirmationPending(bool pending)
        {
            contextManager.SetDeleteConfirmationPending(pending);
        }
        
        /// <summary>
        /// Clears text elements within a specific Y range (inclusive)
        /// Clears ALL text in the Y range across the entire canvas width
        /// This is the standard method for clearing panels/areas - always clears full width
        /// </summary>
        public void ClearTextInRange(int startY, int endY)
        {
            canvas.ClearTextInRange(startY, endY);
        }
        
        /// <summary>
        /// Clears text elements within a specific rectangular area (inclusive)
        /// Use this only when you need to clear a specific rectangular region (e.g., center panel only)
        /// For clearing full-width panels, use ClearTextInRange() instead
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            canvas.ClearTextInArea(startX, startY, width, height);
        }
    }
}

