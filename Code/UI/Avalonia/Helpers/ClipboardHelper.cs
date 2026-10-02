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
        /// Success feedback is the center-panel tint flash only (no status text).
        /// </summary>
        /// <param name="canvasUI">The canvas UI coordinator</param>
        /// <param name="window">The main window (for TopLevel access)</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> CopyDisplayBufferToClipboard(
            CanvasUICoordinator canvasUI,
            Window window)
        {
            try
            {
                string bufferText = canvasUI.GetBattleLogClipboardText();

                if (string.IsNullOrWhiteSpace(bufferText))
                    return false;

                var topLevel = TopLevel.GetTopLevel(window);
                if (topLevel?.Clipboard == null)
                    return false;

                await topLevel.Clipboard.SetTextAsync(bufferText);
                Dispatcher.UIThread.Post(() => canvasUI.FlashCenterPanelCopyFeedback());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
