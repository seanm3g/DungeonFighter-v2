using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame;
using RPGGame.Config.BalancePatches;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class AboutSettingsPanel : UserControl
    {
        public AboutSettingsPanel()
        {
            InitializeComponent();
            if (VersionSubtitleTextBlock != null)
                VersionSubtitleTextBlock.Text =
                    $"Game data version {BalancePatchMetadata.GetGameVersion()} · .NET 8 · Avalonia 11.2";
            if (SettingsFilePathTextBlock != null)
                SettingsFilePathTextBlock.Text = GameSettings.GetSettingsFilePathForDisplay();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

