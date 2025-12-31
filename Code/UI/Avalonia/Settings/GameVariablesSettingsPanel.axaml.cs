using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class GameVariablesSettingsPanel : UserControl
    {
        public GameVariablesSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

