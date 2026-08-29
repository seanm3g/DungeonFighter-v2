using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame;
using RPGGame.UI.Avalonia;
using RPGGame.UI.TitleScreen;
using RPGGame.Utils;
using System;

namespace RPGGame.UI.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Closing the main window must exit the process even if auxiliary windows
                // (settings, Action Lab, tuning workbench) are still open.
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // Load title colors/config/first frame before the window exists so Show()
                // does not hitch on the first idle paint.
                try
                {
                    TitleScreenHelper.Preload();
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("App", $"TitleScreenHelper.Preload failed: {ex.Message}");
                }

                desktop.MainWindow = new MainWindow();

                // Title-bar X (and any other main-window close) must fully exit the process.
                // Avalonia shutdown alone can leave SoundFlow/native threads alive, which
                // keeps DF.exe locked and breaks the next build (MSB3026).
                desktop.MainWindow.Closing += (_, _) =>
                {
                    BuildExecutionMetrics.StopExecutionTracking("GUI");
                    ApplicationShutdownHelper.PerformShutdown(forceProcessExit: true);
                };

                desktop.Exit += (_, _) =>
                    ApplicationShutdownHelper.PerformShutdown(forceProcessExit: true);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
