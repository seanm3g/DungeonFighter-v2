using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Mechanics an action's trigger bundles "own": they fire only through the card's rules
    /// (<see cref="RPGGame.Combat.Rules.ActionRules"/>) on a matching event, so the whole-row status / sheet-mod apply skips them.
    /// </summary>
    public static class ActionTriggerOwnership
    {
        /// <summary>Mechanic IDs pointed at by any enabled trigger bundle on this action.</summary>
        public static HashSet<string> GetOwnedMechanicIds(Action? action)
        {
            var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (action?.Triggers?.Bundles == null)
                return owned;
            foreach (var bundle in action.Triggers.Bundles)
            {
                if (bundle == null || !bundle.IsEnabled)
                    continue;
                foreach (string id in bundle.ParseMechanicIds())
                    owned.Add(ActionMechanicsRegistry.NormalizeMechanicId(id));
            }

            return owned;
        }

        public static bool IsMechanicOwned(Action? action, string? mechanicId)
        {
            if (string.IsNullOrWhiteSpace(mechanicId) || action == null)
                return false;
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            var owned = GetOwnedMechanicIds(action);
            if (owned.Contains(id))
                return true;
            // Status aliases between sheet IDs and effect registry names
            if (id is "confusion" && owned.Contains("confuse")) return true;
            if (id is "confuse" && owned.Contains("confusion")) return true;
            if (id is "statdrain" && owned.Contains("stat_drain")) return true;
            if (id is "stat_drain" && owned.Contains("statdrain")) return true;
            return false;
        }

        public static bool IsStatusEffectOwned(Action? action, string? effectType) =>
            IsMechanicOwned(action, effectType);

        public static bool IsSheetModTypeOwned(Action? action, string? bonusType, bool useEnemySpreadsheetMods = false)
        {
            if (string.IsNullOrWhiteSpace(bonusType) || action == null)
                return false;
            if (!ActionMechanicsRegistry.TryGetMechanicIdFromBonusType(bonusType, out string mechanicId, out _))
                return false;
            if (useEnemySpreadsheetMods
                && mechanicId.StartsWith("hero_", StringComparison.OrdinalIgnoreCase))
            {
                mechanicId = "enemy_" + mechanicId.Substring("hero_".Length);
            }

            return IsMechanicOwned(action, mechanicId);
        }

        /// <summary>
        /// Removes sheet SPEED/DAMAGE/MULTIHIT/AMP items whose mechanic IDs are owned by TRIGGERS → pointers.
        /// </summary>
        public static List<ActionAttackBonusItem> FilterOutOwnedSheetMods(
            Action? action,
            List<ActionAttackBonusItem> bonuses,
            bool useEnemySpreadsheetMods = false)
        {
            if (action == null || bonuses == null || bonuses.Count == 0)
                return bonuses ?? new List<ActionAttackBonusItem>();
            if (GetOwnedMechanicIds(action).Count == 0)
                return bonuses;
            return bonuses
                .Where(b => !IsSheetModTypeOwned(action, b.Type, useEnemySpreadsheetMods))
                .ToList();
        }
    }
}
