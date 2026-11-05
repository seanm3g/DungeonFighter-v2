using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class AboutSettingsPanel : UserControl
    {
        /// <summary>Game data version (was BalancePatchMetadata.GameVersion in the removed legacy patch system).</summary>
        private const string GameDataVersion = "6.2";

        public AboutSettingsPanel()
        {
            InitializeComponent();
            if (VersionSubtitleTextBlock != null)
                VersionSubtitleTextBlock.Text =
                    $"Game data version {GameDataVersion} · .NET 8 · Avalonia 11.2";
            if (SettingsFilePathTextBlock != null)
                SettingsFilePathTextBlock.Text = GameSettings.GetSettingsFilePathForDisplay();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

