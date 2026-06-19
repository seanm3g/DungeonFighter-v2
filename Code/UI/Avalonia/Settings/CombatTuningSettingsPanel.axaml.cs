using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class CombatTuningSettingsPanel : UserControl
    {
        public CombatTuningSettingsPanel()
        {
            InitializeComponent();
        }

        public CombatTuningPanelViewModel? ViewModel
        {
            get => DataContext as CombatTuningPanelViewModel;
            set => DataContext = value;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
