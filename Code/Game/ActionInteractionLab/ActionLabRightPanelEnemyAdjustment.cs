using System;
using RPGGame.UI;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Action Lab: right-panel enemy row uses <c>rphover:enemy:level</c>; left-click / right-click deltas
    /// are applied by <see cref="RPGGame.UI.Avalonia.Handlers.MouseInteractionHandler"/> when in lab state.
    /// </summary>
    public static class ActionLabRightPanelEnemyAdjustment
    {
        /// <summary>Enemy level line under ENEMY name (<c>Lvl N</c> or <c>Lvl N (±Δ)</c> in lab).</summary>
        public static string EnemyLevelHoverId => RightPanelEnemyLabHoverState.Prefix + "enemy:level";

        /// <summary>
        /// Action Lab right panel: <c>Lvl 9 (-2)</c> when the hero is level 11 (delta = enemy − hero).
        /// </summary>
        public static string FormatEnemyLevelCaptionWithHeroDelta(int enemyLevel, int heroLevel)
        {
            int delta = enemyLevel - heroLevel;
            string inner = delta switch
            {
                0 => "0",
                > 0 => $"+{delta}",
                _ => delta.ToString()
            };
            return $"Lvl {enemyLevel} ({inner})";
        }

        /// <summary>
        /// Returns true if <paramref name="fullHoverValue"/> is the enemy level row and applies the level change via the session.
        /// </summary>
        public static bool TryApply(ActionInteractionLabSession session, string fullHoverValue, int delta)
        {
            if (delta == 0)
                return false;
            if (!string.Equals(fullHoverValue, EnemyLevelHoverId, StringComparison.Ordinal))
                return false;
            session.ApplyLabEnemyLevelDelta(delta);
            return true;
        }
    }

    /// <summary>Prefix for Action Lab right-panel enemy hover hit targets (combat layout).</summary>
    public static class RightPanelEnemyLabHoverState
    {
        public const string Prefix = "rphover:";
    }
}
