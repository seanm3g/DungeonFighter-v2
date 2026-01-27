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
        private readonly System.Action clearCanvasAction;
        
        public MessageDisplayRenderer(GameCanvasControl canvas, System.Action clearCanvasAction)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.clearCanvasAction = clearCanvasAction ?? throw new ArgumentNullException(nameof(clearCanvasAction));
        }
        
        /// <summary>
        /// Shows a general message on screen
        /// </summary>
        public void ShowMessage(string message, Color color = default)
        {
            if (color == default) color = AsciiArtAssets.Colors.White;
            
            clearCanvasAction();
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
            clearCanvasAction();
            
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
            clearCanvasAction();
            
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
        /// Non-blocking version that shows message immediately without freezing UI
        /// </summary>
        public void ShowLoadingAnimation(string message = "Loading...")
        {
            // Show immediate message without blocking animation
            // This prevents UI freeze while async operations happen in background
            clearCanvasAction();
            canvas.AddCenteredText(18, message, AsciiArtAssets.Colors.White);
            canvas.AddCenteredText(20, "....", AsciiArtAssets.Colors.Yellow);
            canvas.Refresh();
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
        
        /// <summary>
        /// Shows a loading message in the bottom left corner of the window
        /// This is used to indicate data is being loaded while the menu is displayed
        /// </summary>
        public void ShowLoadingStatus(string message = "Loading data...")
        {
            // Position at the very bottom left of the screen (SCREEN_HEIGHT - 1 for last line)
            int statusY = CanvasLayoutManager.SCREEN_HEIGHT - 1;
            int statusX = CanvasLayoutManager.LEFT_MARGIN + 2;
            
            // Clear the area first to remove any previous message
            canvas.ClearTextInRange(statusY - 1, statusY + 1);
            
            // Add the loading message in gray color
            canvas.AddText(statusX, statusY, message, AsciiArtAssets.Colors.Gray);
            canvas.Refresh();
        }
        
        /// <summary>
        /// Clears the loading status message from the bottom left corner
        /// Also clears the center area where ShowLoadingAnimation might have displayed a full-screen animation
        /// </summary>
        public void ClearLoadingStatus()
        {
            // Clear the bottom left area where loading status is displayed
            int statusY = CanvasLayoutManager.SCREEN_HEIGHT - 1;
            canvas.ClearTextInRange(statusY - 1, statusY + 1);
            
            // Also clear the center area where ShowLoadingAnimation displays full-screen loading messages
            // This ensures that if ShowLoadingAnimation was used, it gets cleared too
            canvas.ClearTextInRange(16, 22); // Clear lines 16-22 where loading animation is typically shown
            
            canvas.Refresh();
        }
    }
}
