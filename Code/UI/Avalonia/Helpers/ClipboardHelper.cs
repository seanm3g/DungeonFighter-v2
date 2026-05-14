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
        /// Copies the center display buffer plus a plain-text snapshot of the left character panel to the clipboard.
        /// </summary>
        /// <param name="canvasUI">The canvas UI coordinator</param>
        /// <param name="window">The main window (for TopLevel access)</param>
        /// <param name="statusText">Optional status text block for feedback</param>
        /// <param name="statusCallback">Optional callback (UI thread) when <paramref name="statusText"/> is not used — e.g. canvas status bar</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> CopyDisplayBufferToClipboard(
            CanvasUICoordinator canvasUI,
            Window window,
            TextBlock? statusText = null,
            Action<string>? statusCallback = null)
        {
            void Notify(string message)
            {
                StatusUpdateHelper.UpdateStatusText(statusText, message);
                if (statusCallback != null)
                    Dispatcher.UIThread.Post(() => statusCallback(message));
            }

            try
            {
                Notify("Copying to clipboard...");

                string bufferText = canvasUI.GetBattleLogClipboardText();

                if (string.IsNullOrWhiteSpace(bufferText))
                {
                    Notify("No text to copy");
                    return false;
                }

                // Get the clipboard from the top-level window
                var topLevel = TopLevel.GetTopLevel(window);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(bufferText);
                    int lineCount = bufferText.Split(new[] { System.Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.None).Length;
                    string message = $"Copied {lineCount} lines (character panel + combat log)";
                    Notify(message);
                    Dispatcher.UIThread.Post(() => canvasUI.FlashCenterPanelCopyFeedback());
                    return true;
                }
                else
                {
                    Notify("Clipboard not available");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error: {ex.Message}";
                Notify(errorMsg);
                return false;
            }
        }
    }
}
