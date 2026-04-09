using RPGGame;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Whether the action-info strip may be reordered by drag-and-drop (mouse), given state.
    /// <paramref name="currentEnemy"/> is reserved for future use; do not use it to gate dungeon reorder:
    /// enemy context often stays set after encounters while combat clears it mid-fight, which inverted lock/allow.
    /// Full gating is in <see cref="RPGGame.UI.Avalonia.Handlers.MouseInteractionHandler.CanReorderComboOnStrip"/>:
    /// <see cref="GameStateManager.HasCurrentDungeon"/> locks reorder for the whole run (including inventory);
    /// <see cref="GameState.Combat"/> and <see cref="GameStateManager.IsComboStripEncounterLocked"/> cover encounters.
    /// Do not gate strip reorder on <c>CombatDisplayMode</c> alone — it drifts vs game state.
    /// </summary>
    public static class ActionStripReorderPolicy
    {
        /// <summary>
        /// Returns true when combo strip drag-reorder is allowed for the given game state.
        /// </summary>
        /// <param name="state">Current <see cref="GameState"/>.</param>
        /// <param name="currentEnemy">Unused; pass null.</param>
        public static bool AllowsReorder(GameState state, Enemy? currentEnemy)
        {
            if (state == GameState.Combat || state == GameState.ActionInteractionLab)
                return false;
            // Persistent chrome with combo strip: hub, character view, inventory, dungeon exploration, post-run summary.
            if (state == GameState.GameLoop
                || state == GameState.CharacterInfo
                || state == GameState.Inventory
                || state == GameState.Dungeon
                || state == GameState.DungeonCompletion)
                return true;
            return false;
        }
    }
}
