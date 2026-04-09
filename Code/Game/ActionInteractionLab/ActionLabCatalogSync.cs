using System;
using RPGGame;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Pure catalog selection for the lab: which forced action name should match the hero strip for the upcoming Step.
    /// </summary>
    public static class ActionLabCatalogSync
    {
        /// <summary>
        /// Returns the forced catalog action name matching <paramref name="labPlayer"/>'s combo strip and
        /// <see cref="Character.ComboStep"/> (next slot). <paramref name="next"/> is unused but kept for call-site clarity.
        /// When the speed wheel has no ready actor yet, this still returns the hero slot so the lab catalog does not go blank.
        /// </summary>
        public static string? ComputeSelectedCatalogName(Actor? next, Character labPlayer, Enemy labEnemy)
        {
            ArgumentNullException.ThrowIfNull(labPlayer);
            ArgumentNullException.ThrowIfNull(labEnemy);

            var combo = ActionUtilities.GetComboActions(labPlayer);
            int stepForIndex = ActionUtilities.GetComboStep(labPlayer);

            if (combo.Count == 0) return null;

            int idx = stepForIndex % combo.Count;
            string? name = combo[idx].Name;
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
