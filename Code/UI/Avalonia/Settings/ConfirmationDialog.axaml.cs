using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog()
        {
            InitializeComponent();
            
            YesButton.Click += (s, e) =>
            {
                Close(true);
            };
            
            NoButton.Click += (s, e) =>
            {
                Close(false);
            };
        }

        public ConfirmationDialog(string title, string message) : this()
        {
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task<bool> ShowAsync(Window? parent, string title, string message)
        {
            var dialog = new ConfirmationDialog(title, message);
            Window? targetWindow = parent;
            
            if (targetWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                targetWindow = desktop.MainWindow;
            }
            
            if (targetWindow != null)
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                var result = await dialog.ShowDialog<bool>(targetWindow!);
                return result;
            }
            else
            {
                // Fallback: show as standalone window
                // If we still don't have a window, throw an exception as ShowDialog requires a non-null owner
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime fallbackDesktop)
                {
                    var fallbackWindow = fallbackDesktop.MainWindow;
                    if (fallbackWindow != null)
                    {
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        var result = await dialog.ShowDialog<bool>(fallbackWindow!);
                        return result;
                    }
                }
                
                throw new InvalidOperationException("No window available to show dialog. Application must have a main window.");
            }
        }
    }
}
