using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame.Config;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class PatchSaveDialog : Window
    {
        public PatchSaveDialog()
        {
            InitializeComponent();
            var updateButton = this.FindControl<Button>("UpdateButton");
            var saveAsButton = this.FindControl<Button>("SaveAsButton");
            var cancelButton = this.FindControl<Button>("CancelButton");
            if (updateButton == null || saveAsButton == null || cancelButton == null)
                throw new InvalidOperationException("PatchSaveDialog: button controls missing from template.");

            updateButton.Click += (_, _) => Close(PatchSaveChoice.UpdateExisting);
            saveAsButton.Click += (_, _) => Close(PatchSaveChoice.SaveAsNew);
            cancelButton.Click += (_, _) => Close(PatchSaveChoice.Cancelled);
        }

        public PatchSaveDialog(string title, string message) : this()
        {
            var titleBlock = this.FindControl<TextBlock>("TitleTextBlock");
            var messageBlock = this.FindControl<TextBlock>("MessageTextBlock");
            if (titleBlock != null)
                titleBlock.Text = title;
            if (messageBlock != null)
                messageBlock.Text = message;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public static async Task<PatchSaveChoice> ShowAsync(Window? parent, PatchCategory category, string activePatchName)
        {
            string categoryLabel = PatchProfileService.GetCategoryDisplayName(category);
            var dialog = new PatchSaveDialog(
                $"Save {categoryLabel} patch",
                $"Active patch: \"{activePatchName}\"\n\nUpdate overwrites that patch file (commit to GitHub to share).\nSave as new creates another patch you can switch to locally.");

            Window? targetWindow = parent;
            if (targetWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                targetWindow = desktop.MainWindow;

            if (targetWindow == null)
                throw new InvalidOperationException("No window available to show patch save dialog.");

            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return await dialog.ShowDialog<PatchSaveChoice>(targetWindow);
        }
    }
}
