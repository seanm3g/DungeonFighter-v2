using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Threading;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for displaying messages, errors, and loading animations
    /// </summary>
    public class MessageDisplayRenderer
    {
        private readonly GameCanvasControl canvas;
        
        public MessageDisplayRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Shows a general message on screen
        /// </summary>
        public void ShowMessage(string message, Color color = default)
        {
            if (color == default) color = AsciiArtAssets.Colors.White;
            
            canvas.Clear();
            canvas.AddCenteredText(20, message, color);
            canvas.Refresh();
        }
        
        /// <summary>
        /// Shows an error message
        /// </summary>
        public void ShowError(string error)
        {
            ShowMessage($"ERROR: {error}", AsciiArtAssets.Colors.Red);
        }
        
        /// <summary>
        /// Shows a success message
        /// </summary>
        public void ShowSuccess(string message)
        {
            ShowMessage(message, AsciiArtAssets.Colors.Green);
        }
        
        /// <summary>
        /// Shows a detailed error with suggestion
        /// </summary>
        public void ShowError(string error, string suggestion = "")
        {
            canvas.Clear();
            
            // Error title
            canvas.AddCenteredText(15, "ERROR", AsciiArtAssets.Colors.Red);
            
            // Error message
            canvas.AddCenteredText(18, error, AsciiArtAssets.Colors.White);
            
            // Suggestion if provided
            if (!string.IsNullOrEmpty(suggestion))
            {
                canvas.AddCenteredText(20, suggestion, AsciiArtAssets.Colors.Yellow);
            }
            
            // Instructions
            canvas.AddCenteredText(25, "Press any key to continue...", AsciiArtAssets.Colors.Gray);
            
            canvas.Refresh();
        }
        
        /// <summary>
        /// Shows a loading animation with dots (async version)
        /// </summary>
        public async Task ShowLoadingAnimationAsync(string message = "Loading...")
        {
            canvas.Clear();
            
            // Center the loading message
            canvas.AddCenteredText(18, message, AsciiArtAssets.Colors.White);
            
            // Simple loading animation with dots
            string dots = "....";
            for (int i = 0; i < 4; i++)
            {
                canvas.AddCenteredText(20, dots.Substring(0, i + 1), AsciiArtAssets.Colors.Yellow);
                canvas.Refresh();
                await System.Threading.Tasks.Task.Delay(200);
            }
        }
        
        /// <summary>
        /// Shows a loading animation with dots (synchronous version for backwards compatibility)
        /// </summary>
        public void ShowLoadingAnimation(string message = "Loading...")
        {
            ShowLoadingAnimationAsync(message).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Updates status message at the bottom of the display
        /// </summary>
        public void UpdateStatus(string message)
        {
            // Add status message to the bottom of the display
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 2, CanvasLayoutManager.CONTENT_HEIGHT - 2, message, AsciiArtAssets.Colors.Gray);
            canvas.Refresh();
        }
        
        /// <summary>
        /// Shows an invalid key message at the bottom of the display without clearing the screen
        /// This allows users to still see the available menu options
        /// </summary>
        public void ShowInvalidKeyMessage(string message)
        {
            // Clear the bottom status area first to remove any previous message
            // Clear a few lines at the bottom to ensure clean display
            // Position at the very bottom of the screen (SCREEN_HEIGHT - 2 to leave a small margin)
            int statusY = CanvasLayoutManager.SCREEN_HEIGHT - 2;
            canvas.ClearTextInRange(statusY - 1, statusY + 1);
            
            // Add the invalid key message at the bottom in yellow to make it prominent
            canvas.AddText(CanvasLayoutManager.LEFT_MARGIN + 2, statusY, message, AsciiArtAssets.Colors.Yellow);
            canvas.Refresh();
        }
    }
}
