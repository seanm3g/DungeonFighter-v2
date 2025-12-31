using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class BattleStatisticsSettingsPanel : UserControl
    {
        public BattleStatisticsSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

