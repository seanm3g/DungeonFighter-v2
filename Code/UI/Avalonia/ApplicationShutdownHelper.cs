using System;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RPGGame;
using RPGGame.Audio;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Tuning;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Ensures background services and auxiliary windows are torn down so the GUI process
    /// exits cleanly and releases the single-instance mutex in <see cref="Program"/>.
    /// </summary>
    public static class ApplicationShutdownHelper
    {
        private static int _shutdownStarted;
        private static System.Action? _bestEffortCharacterSave;

        /// <summary>
        /// Registers a best-effort character save invoked once at process shutdown
        /// (window close, Exit Game, etc.). Replaces any previous callback.
        /// </summary>
        public static void RegisterBestEffortCharacterSave(System.Action? saveCallback)
        {
            _bestEffortCharacterSave = saveCallback;
        }

        public static void PerformShutdown()
        {
            if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
                return;

            try
            {
                try
                {
                    _bestEffortCharacterSave?.Invoke();
                }
                catch
                {
                    // Best-effort save must never block or throw through shutdown.
                }

                ActionLabControlsWindow.CloseIfOpen();
                BalanceTuningWorkbenchWindow.CloseForShutdown();
                CloseVisibleWindows<SettingsWindow>();

                if (GameTicker.Instance.IsRunning)
                    GameTicker.Instance.Stop();

                if (UIManager.GetCustomUIManager() is CanvasUICoordinator canvasUI
                    && canvasUI.GetAnimationManager() is CanvasAnimationManager animationManager)
                {
                    animationManager.Dispose();
                }

                AudioBootstrap.Shutdown();
            }
            catch
            {
                // Shutdown must never throw back into Avalonia's close path.
            }
        }

        private static void CloseVisibleWindows<TWindow>() where TWindow : Window
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            foreach (var window in desktop.Windows.OfType<TWindow>().ToList())
            {
                try
                {
                    if (window.IsVisible)
                        window.Close();
                }
                catch
                {
                    // Ignore errors when closing windows during shutdown.
                }
            }
        }
    }
}
