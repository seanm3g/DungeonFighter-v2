using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.UI.Avalonia;
using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Helper class for clipboard operations.
    /// Extracts clipboard logic from MainWindow.
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// Copies the display buffer text to the clipboard.
        /// </summary>
        /// <param name="canvasUI">The canvas UI coordinator</param>
        /// <param name="window">The main window (for TopLevel access)</param>
        /// <param name="statusText">Optional status text block for feedback</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> CopyDisplayBufferToClipboard(
            CanvasUICoordinator canvasUI,
            Window window,
            TextBlock? statusText = null)
        {
            try
            {
                System.Console.WriteLine("[COPY] CopyDisplayBufferToClipboard called");

                // Update status for immediate feedback
                StatusUpdateHelper.UpdateStatusText(statusText, "Copying to clipboard...");

                string bufferText = canvasUI.GetDisplayBufferText();
                System.Console.WriteLine($"[COPY] Buffer text length: {bufferText?.Length ?? 0}");

                if (string.IsNullOrWhiteSpace(bufferText))
                {
                    StatusUpdateHelper.UpdateStatusText(statusText, "No text to copy");
                    return false;
                }

                // Get the clipboard from the top-level window
                var topLevel = TopLevel.GetTopLevel(window);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(bufferText);
                    int lineCount = bufferText.Split(new[] { System.Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.None).Length;
                    string message = $"Copied {lineCount} lines to clipboard";
                    StatusUpdateHelper.UpdateStatusText(statusText, message);
                    System.Console.WriteLine($"[COPY] Successfully copied {lineCount} lines to clipboard");
                    return true;
                }
                else
                {
                    System.Console.WriteLine("[COPY] Clipboard is null");
                    StatusUpdateHelper.UpdateStatusText(statusText, "Clipboard not available");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[COPY] Exception: {ex.Message}\n{ex.StackTrace}");
                string errorMsg = $"Error: {ex.Message}";
                StatusUpdateHelper.UpdateStatusText(statusText, errorMsg);
                return false;
            }
        }
    }
}
