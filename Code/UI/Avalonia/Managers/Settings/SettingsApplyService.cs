using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Single place for applying saved settings to the running game. Called by the settings panel after a successful save.
    /// Documented in SETTINGS_SYSTEM_ARCHITECTURE.md: "saved settings apply immediately to the running game".
    /// </summary>
    public static class SettingsApplyService
    {
        /// <summary>
        /// Applies saved settings to the running game. Call once after the orchestrator reports a successful save.
        /// - When actions were saved: refreshes the current player's action pool from ActionLoader so in-game actions match Settings.
        /// - Text delay changes are already applied in-place by TextDelaysPanelHandler / TextDelayConfiguration during save; no extra step here.
        /// - Gameplay/difficulty changes apply to the current GameSettings instance and take effect on next combat or when the game reads them.
        /// </summary>
        public static void ApplyAfterSave(SettingsSaveResult result, GameStateManager? gameStateManager)
        {
            if (!result.Success) return;
            if (result.ActionsSaved && gameStateManager?.CurrentPlayer != null)
                ActionsTabManager.RefreshCurrentPlayerActionPool(gameStateManager.CurrentPlayer);
            // TextDelaysSaved: TextDelayConfiguration was updated during save; consumers read from it directly. No reload needed.
        }
    }
}
