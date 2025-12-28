using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;
using System;
using System.Linq;

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
                desktop.MainWindow = new MainWindow();
                
                // Track execution time when window closes
                desktop.MainWindow.Closing += (sender, e) =>
                {
                    BuildExecutionMetrics.StopExecutionTracking("GUI");
                    
                    // Close all settings windows when main window closes
                    var settingsWindows = desktop.Windows.OfType<SettingsWindow>().ToList();
                    foreach (var window in settingsWindows)
                    {
                        try
                        {
                            if (window != null && window.IsVisible)
                            {
                                window.Close();
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore errors when closing windows
                        }
                    }
                };
                
                // Also handle application exit as a fallback
                desktop.Exit += (sender, e) =>
                {
                    // Close all settings windows on application exit
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                    {
                        var settingsWindows = desktopLifetime.Windows.OfType<SettingsWindow>().ToList();
                        foreach (var window in settingsWindows)
                        {
                            try
                            {
                                if (window != null && window.IsVisible)
                                {
                                    window.Close();
                                }
                            }
                            catch (Exception)
                            {
                                // Ignore errors when closing windows
                            }
                        }
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
