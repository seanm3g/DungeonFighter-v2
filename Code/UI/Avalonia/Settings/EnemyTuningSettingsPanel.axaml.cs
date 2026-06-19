using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class EnemyTuningSettingsPanel : UserControl
    {
        public EnemyTuningSettingsPanel()
        {
            InitializeComponent();
        }

        public EnemyTuningPanelViewModel? ViewModel
        {
            get => DataContext as EnemyTuningPanelViewModel;
            set => DataContext = value;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
