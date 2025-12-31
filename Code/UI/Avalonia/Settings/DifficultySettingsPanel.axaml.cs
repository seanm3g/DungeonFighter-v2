using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class DifficultySettingsPanel : UserControl
    {
        public DifficultySettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

