using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class CombatTuningSettingsPanel : UserControl
    {
        private TabControl? tabControl;
        private ProgressionCurvePreviewControl? progressionPreviewControl;

        public CombatTuningSettingsPanel()
        {
            InitializeComponent();
        }

        public CombatTuningPanelViewModel? ViewModel
        {
            get => DataContext as CombatTuningPanelViewModel;
            set
            {
                DataContext = value;
                progressionPreviewControl?.RedrawChart();
            }
        }

        public void SelectSubTab(CombatTuningNavigation.CombatTuningSubTab subTab)
        {
            tabControl ??= this.FindControl<TabControl>("CombatTuningTabControl");
            if (tabControl == null)
                return;

            tabControl.SelectedIndex = subTab switch
            {
                CombatTuningNavigation.CombatTuningSubTab.Starting => 0,
                CombatTuningNavigation.CombatTuningSubTab.Scaling => 1,
                CombatTuningNavigation.CombatTuningSubTab.Outfit => 2,
                _ => 0
            };

            if (ViewModel != null)
            {
                ViewModel.RefreshProgressionPreview();
                progressionPreviewControl?.RedrawChart();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            tabControl = this.FindControl<TabControl>("CombatTuningTabControl");
            progressionPreviewControl = this.FindControl<ProgressionCurvePreviewControl>("ProgressionPreviewControl");
        }
    }
}
