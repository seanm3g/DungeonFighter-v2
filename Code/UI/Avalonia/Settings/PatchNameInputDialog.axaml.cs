using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame.Config;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class PatchNameInputDialog : Window
    {
        public PatchNameInputDialog()
        {
            InitializeComponent();

            var okButton = this.FindControl<Button>("OkButton");
            var cancelButton = this.FindControl<Button>("CancelButton");
            var nameBox = this.FindControl<TextBox>("PatchNameTextBox");
            if (okButton == null || cancelButton == null || nameBox == null)
                throw new InvalidOperationException("PatchNameInputDialog: required controls missing from template.");

            okButton.Click += async (_, _) =>
            {
                try
                {
                    ResultName = PatchProfileService.SanitizePatchName(nameBox.Text ?? string.Empty);
                    Close(true);
                }
                catch (Exception ex)
                {
                    ResultName = null;
                    await ConfirmationDialog.ShowAsync(this, "Invalid name", ex.Message);
                }
            };
            cancelButton.Click += (_, _) => Close(false);
        }

        public PatchNameInputDialog(string title, string prompt, string defaultName) : this()
        {
            Title = title;
            var promptBlock = this.FindControl<TextBlock>("PromptTextBlock");
            var nameBox = this.FindControl<TextBox>("PatchNameTextBox");
            if (promptBlock != null)
                promptBlock.Text = prompt;
            if (nameBox != null)
                nameBox.Text = defaultName;
        }

        public string? ResultName { get; private set; }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public static async Task<string?> ShowAsync(Window? parent, string title, string defaultName, string? prompt = null)
        {
            var dialog = new PatchNameInputDialog(
                title,
                prompt ?? "Patch name (letters, numbers, hyphens):",
                defaultName);

            Window? targetWindow = parent;
            if (targetWindow == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                targetWindow = desktop.MainWindow;

            if (targetWindow == null)
                throw new InvalidOperationException("No window available to show patch name dialog.");

            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            bool ok = await dialog.ShowDialog<bool>(targetWindow);
            return ok ? dialog.ResultName : null;
        }
    }
}
