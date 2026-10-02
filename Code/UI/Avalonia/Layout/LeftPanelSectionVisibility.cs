namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Left-panel GEAR / STATUS EFFECTS visibility by dungeon-run membership.
    /// GEAR hides for the whole dungeon run (select → leave); Action Lab keeps it for right-click edit.
    /// STATUS EFFECTS show for the whole dungeon run and in Action Lab; hide once the run ends.
    /// </summary>
    public static class LeftPanelSectionVisibility
    {
        /// <param name="state">Current game state (Action Lab overrides dungeon-run gating).</param>
        /// <param name="inDungeonRun">True while <see cref="GameStateManager.HasCurrentDungeon"/>.</param>
        public static bool ShowGear(GameState? state, bool inDungeonRun = false)
        {
            if (state == GameState.ActionInteractionLab)
                return true;
            return !inDungeonRun;
        }

        /// <param name="state">Current game state (Action Lab always shows status effects).</param>
        /// <param name="inDungeonRun">True while <see cref="GameStateManager.HasCurrentDungeon"/>.</param>
        public static bool ShowStatusEffects(GameState? state, bool inDungeonRun = false)
        {
            if (state == GameState.ActionInteractionLab)
                return true;
            return inDungeonRun;
        }
    }
}
