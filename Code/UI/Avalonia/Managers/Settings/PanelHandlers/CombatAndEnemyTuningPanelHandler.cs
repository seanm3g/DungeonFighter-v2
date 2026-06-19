using Avalonia.Controls;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>Delegates wire/load/save to Combat and Enemy tuning child panels.</summary>
    public sealed class CombatAndEnemyTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly CombatTuningPanelHandler combatHandler;
        private readonly EnemyTuningPanelHandler enemyHandler;

        public CombatAndEnemyTuningPanelHandler(
            CombatTuningPanelHandler combatHandler,
            EnemyTuningPanelHandler enemyHandler)
        {
            this.combatHandler = combatHandler;
            this.enemyHandler = enemyHandler;
        }

        public string PanelType => "CombatTuning";

        public void WireUp(UserControl panel)
        {
            if (panel is not CombatAndEnemyTuningSettingsPanel container)
                return;
            combatHandler.WireUp(container.CombatPanel);
            enemyHandler.WireUp(container.EnemyPanel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not CombatAndEnemyTuningSettingsPanel container)
                return;
            combatHandler.LoadSettings(container.CombatPanel);
            enemyHandler.LoadSettings(container.EnemyPanel);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not CombatAndEnemyTuningSettingsPanel container)
                return;
            enemyHandler.SaveSettings(container.EnemyPanel);
            combatHandler.SaveSettings(container.CombatPanel);
        }
    }
}
