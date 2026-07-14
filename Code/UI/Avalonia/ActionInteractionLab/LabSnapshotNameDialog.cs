using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>Simple name prompt for Action Lab character snapshots.</summary>
    public static class LabSnapshotNameDialog
    {
        public static async Task<string?> PromptAsync(Window owner, string title, string defaultName, string promptLabel = "Snapshot name:")
        {
            var input = new TextBox
            {
                Text = defaultName ?? "",
                Width = 360
            };
            string? result = null;
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.Black,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = promptLabel,
                            Foreground = Brushes.White,
                            TextWrapping = TextWrapping.Wrap
                        },
                        input,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 8,
                            Children =
                            {
                                new Button { Content = "OK", Width = 90 },
                                new Button { Content = "Cancel", Width = 90 }
                            }
                        }
                    }
                }
            };

            var buttons = ((StackPanel)((StackPanel)dialog.Content!).Children[2]).Children;
            var okBtn = (Button)buttons[0]!;
            var cancelBtn = (Button)buttons[1]!;
            okBtn.Click += (_, _) =>
            {
                result = input.Text?.Trim();
                dialog.Close(true);
            };
            cancelBtn.Click += (_, _) => dialog.Close(false);
            input.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    result = input.Text?.Trim();
                    dialog.Close(true);
                    e.Handled = true;
                }
            };

            bool ok = await dialog.ShowDialog<bool>(owner).ConfigureAwait(true);
            return ok && !string.IsNullOrWhiteSpace(result) ? result : null;
        }
    }
}
