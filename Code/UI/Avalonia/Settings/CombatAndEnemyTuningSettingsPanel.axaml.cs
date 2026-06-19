using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class CombatAndEnemyTuningSettingsPanel : UserControl
    {
        public CombatTuningSettingsPanel CombatPanel { get; }
        public EnemyTuningSettingsPanel EnemyPanel { get; }

        public CombatAndEnemyTuningSettingsPanel()
        {
            InitializeComponent();
            CombatPanel = new CombatTuningSettingsPanel();
            EnemyPanel = new EnemyTuningSettingsPanel();

            var combatHost = this.FindControl<ContentControl>("CombatHost");
            var enemyHost = this.FindControl<ContentControl>("EnemyHost");
            if (combatHost != null)
                combatHost.Content = CombatPanel;
            if (enemyHost != null)
                enemyHost.Content = EnemyPanel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
