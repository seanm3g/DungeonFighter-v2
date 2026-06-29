using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class CombatTuningSettingsPanel : UserControl
    {
        private TabControl? innerTabControl;
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
            innerTabControl ??= this.FindControl<TabControl>("CombatTuningTabControl");
            var parametersInner = this.FindControl<TabControl>("ParametersInnerTabControl");
            if (innerTabControl == null)
                return;

            innerTabControl.SelectedIndex = 0;

            if (subTab == CombatTuningNavigation.CombatTuningSubTab.ProgressionCurve && parametersInner != null)
            {
                var progressionTab = this.FindControl<TabItem>("ProgressionCurveTabItem");
                if (progressionTab != null)
                    parametersInner.SelectedItem = progressionTab;
            }

            if (ViewModel != null)
            {
                ViewModel.RefreshProgressionPreview();
                progressionPreviewControl?.RedrawChart();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            innerTabControl = this.FindControl<TabControl>("CombatTuningTabControl");
            progressionPreviewControl = this.FindControl<ProgressionCurvePreviewControl>("ProgressionPreviewControl");
        }
    }
}
