using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;

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
        
        public UtilityCoordinator(GameCanvasControl canvas, CanvasRenderer renderer, ICanvasTextManager textManager, ICanvasContextManager contextManager)
        {
            this.canvas = canvas;
            this.renderer = renderer;
            this.textManager = textManager;
            this.contextManager = contextManager;
        }
        
        /// <summary>
        /// Clears the canvas
        /// </summary>
        public void Clear()
        {
            canvas.Clear();
        }
        
        /// <summary>
        /// Refreshes the canvas
        /// </summary>
        public void Refresh()
        {
            canvas.Refresh();
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
        /// Shows press key message
        /// </summary>
        public void ShowPressKeyMessage()
        {
            canvas.Clear();
            canvas.AddCenteredText(25, "Press any key to continue...", AsciiArtAssets.Colors.Gray);
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
    }
}

