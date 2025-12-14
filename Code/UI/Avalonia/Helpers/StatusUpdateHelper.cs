using Avalonia.Controls;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Helper class for updating status text in the UI.
    /// Consolidates Dispatcher.UIThread.Post patterns for status updates.
    /// </summary>
    public static class StatusUpdateHelper
    {
        /// <summary>
        /// Updates the status text block on the UI thread.
        /// </summary>
        /// <param name="statusText">The status text block control</param>
        /// <param name="message">The message to display</param>
        public static void UpdateStatusText(TextBlock? statusText, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (statusText != null)
                {
                    statusText.Text = message;
                }
            });
        }

        /// <summary>
        /// Updates the status text block asynchronously.
        /// </summary>
        /// <param name="statusText">The status text block control</param>
        /// <param name="message">The message to display</param>
        public static async Task UpdateStatusTextAsync(TextBlock? statusText, string message)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (statusText != null)
                {
                    statusText.Text = message;
                }
            });
        }
    }
}
