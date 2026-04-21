using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Scrollable read-only text dialog for Action Lab encounter simulation results.
    /// </summary>
    public static class ActionLabSimulationReportDialog
    {
        private const double DefaultDialogWidth = 600;
        private const double DefaultDialogHeight = 920;
        private const double MinDialogWidth = 520;
        private const double MinDialogHeight = 720;

        public static async Task ShowAsync(Window? owner, string title, string body)
        {
            var dialog = new Window
            {
                Title = title,
                Width = DefaultDialogWidth,
                Height = DefaultDialogHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true,
                MinWidth = MinDialogWidth,
                MinHeight = MinDialogHeight,
            };

            var text = new TextBox
            {
                Text = body ?? "",
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 12,
                Margin = new Thickness(12),
            };

            var close = new Button
            {
                Content = "Close",
                MinWidth = 88,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true,
                IsCancel = true,
            };
            close.Click += (_, _) => dialog.Close();

            var bottomBar = new Border
            {
                Child = close,
                Padding = new Thickness(12, 0, 12, 12),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            DockPanel.SetDock(bottomBar, Dock.Bottom);

            var root = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(0, 8, 0, 0),
            };
            root.Children.Add(bottomBar);
            root.Children.Add(text);

            dialog.Content = root;

            dialog.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    dialog.Close();
                    e.Handled = true;
                }
            };

            Window? target = ResolveUsableOwnerWindow(owner);
            if (target == null)
            {
                throw new InvalidOperationException(
                    "No visible window available to show the simulation report. Close stale windows or reopen the app.");
            }

            await dialog.ShowDialog(target).ConfigureAwait(true);
        }

        private static Window? ResolveUsableOwnerWindow(Window? preferred)
        {
            static bool IsDialogOwnerUsable(Window? w)
            {
                if (w == null) return false;
                try
                {
                    return w.IsVisible;
                }
                catch
                {
                    return false;
                }
            }

            if (IsDialogOwnerUsable(preferred))
                return preferred;

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime life)
                return null;

            if (IsDialogOwnerUsable(life.MainWindow))
                return life.MainWindow;

            foreach (var w in life.Windows)
            {
                if (w is Window win && IsDialogOwnerUsable(win))
                    return win;
            }

            return null;
        }
    }
}
