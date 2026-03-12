using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class AboutSettingsPanel : UserControl
    {
        public AboutSettingsPanel()
        {
            InitializeComponent();
            if (SettingsFilePathTextBlock != null)
                SettingsFilePathTextBlock.Text = GameSettings.GetSettingsFilePathForDisplay();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

