using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame.Utils;

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
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
