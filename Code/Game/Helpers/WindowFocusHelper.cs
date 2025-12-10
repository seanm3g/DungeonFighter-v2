namespace RPGGame
{
    using System;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using RPGGame.Utils;

    /// <summary>
    /// Helper class for focusing the main window to ensure keyboard input is captured.
    /// </summary>
    public static class WindowFocusHelper
    {
        /// <summary>
        /// Attempts to focus the main window to ensure keyboard input is captured.
        /// This is a workaround - ideally we'd have a direct reference to MainWindow.
        /// </summary>
        /// <param name="loggerName">Name to use for logging (e.g., class name)</param>
        public static void FocusMainWindow(string loggerName = "WindowFocusHelper")
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : null;
                    if (window != null)
                    {
                        window.Focus();
                    }
                }
                catch (Exception ex)
                {
                }
            }, Avalonia.Threading.DispatcherPriority.Normal);
        }
    }
}

