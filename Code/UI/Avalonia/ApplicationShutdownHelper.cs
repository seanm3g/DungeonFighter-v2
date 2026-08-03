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
    /// Closing the main window (title-bar X) must also terminate the process so native
    /// audio / ticker work cannot leave <c>DF.exe</c> locked for the next build.
    /// </summary>
    public static class ApplicationShutdownHelper
    {
        private static int _shutdownStarted;

        /// <summary>
        /// Max time cleanup may take before a forced exit watchdog ends the process.
        /// Prevents audio dispose / ticker wait from leaving a zombie <c>DF.exe</c>.
        /// </summary>
        internal const int ForceExitWatchdogMs = 1500;

        /// <summary>
        /// Tears down GUI services. When <paramref name="forceProcessExit"/> is true,
        /// ends the process after cleanup (same contract as menu Exit Game).
        /// </summary>
        public static void PerformShutdown(bool forceProcessExit = false)
        {
            if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
            {
                if (forceProcessExit)
                    System.Environment.Exit(0);
                return;
            }

            if (forceProcessExit)
                StartForceExitWatchdog();

            try
            {
                ActionLabControlsWindow.CloseIfOpen();
                BalanceTuningWorkbenchWindow.CloseForShutdown();
                CloseVisibleWindows<SettingsWindow>();

                // Do not Wait on the ticker — blocking Closing can prevent Environment.Exit.
                if (GameTicker.Instance.IsRunning)
                    GameTicker.Instance.Stop(waitForExit: false);

                if (UIManager.GetCustomUIManager() is CanvasUICoordinator canvasUI
                    && canvasUI.GetAnimationManager() is CanvasAnimationManager animationManager)
                {
                    animationManager.Dispose();
                }

                AudioBootstrap.Shutdown();

                TryRequestDesktopShutdown();
            }
            catch
            {
                // Shutdown must never throw back into Avalonia's close path.
            }

            if (forceProcessExit)
                System.Environment.Exit(0);
        }

        /// <summary>Test hook so suite runs can exercise shutdown cleanup more than once.</summary>
        internal static void ResetForTests()
        {
            Interlocked.Exchange(ref _shutdownStarted, 0);
        }

        /// <summary>True after the first successful <see cref="PerformShutdown"/> call in this process.</summary>
        internal static bool HasShutdownStarted => Volatile.Read(ref _shutdownStarted) != 0;

        /// <summary>
        /// Schedules <see cref="System.Environment.Exit"/> so a hung audio/ticker dispose
        /// cannot leave the process alive after the window closes.
        /// </summary>
        private static void StartForceExitWatchdog()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    Thread.Sleep(ForceExitWatchdogMs);
                    System.Environment.Exit(0);
                }
                catch
                {
                    // Ignore — process may already be exiting.
                }
            });
        }

        private static void TryRequestDesktopShutdown()
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            try
            {
                desktop.Shutdown();
            }
            catch
            {
                // Process may already be tearing down.
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
