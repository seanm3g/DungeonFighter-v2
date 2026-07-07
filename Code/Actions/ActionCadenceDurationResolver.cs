using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame.Actions
{
    /// <summary>
    /// Resolves TURN / ACTION cadence layer counts from spreadsheet cadence duration
    /// (<see cref="Action.ComboBonusDuration"/> — column K STATUS EFFECT / DURATION) and keeps them aligned
    /// with <see cref="Action.ActionAttackBonuses"/> group counts after edits or imports.
    /// </summary>
    public static class ActionCadenceDurationResolver
    {
        public static bool IsKeywordCadenceGroup(ActionAttackBonusGroup? group)
        {
            if (group == null) return false;
            var ct = CadenceKeywords.NormalizeCadenceType(
                string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType);
            return CadenceKeywords.IsKeywordCadence(ct);
        }

        /// <summary>
        /// Spreadsheet cadence duration (STATUS EFFECT / DURATION) → <see cref="ActionData.ComboBonusDuration"/>.
        /// Prefers live <see cref="ActionLoader"/> data so combo-strip instances stay correct after sheet edits.
        /// </summary>
        public static int GetRequestedLayerCount(Action? action, ActionAttackBonusGroup? group)
        {
            if (action != null && !string.IsNullOrWhiteSpace(action.Name))
            {
                var data = ActionLoader.GetActionData(action.Name);
                if (data != null && data.ComboBonusDuration > 0)
                {
                    action.ComboBonusDuration = data.ComboBonusDuration;
                    SyncBonusGroupCountsFromDuration(action);
                    return data.ComboBonusDuration;
                }
            }

            if (action == null)
                return group?.Count > 0 ? group.Count : 1;

            if (action.ComboBonusDuration > 0)
            {
                SyncBonusGroupCountsFromDuration(action);
                return action.ComboBonusDuration;
            }

            if (group?.Count > 0)
                return group.Count;

            return 1;
        }

        /// <summary>
        /// Requested layers clipped to remaining combo strip slots (no wrap within the current cycle).
        /// </summary>
        public static int ResolveGrantedLayers(Action? action, ActionAttackBonusGroup? group, IReadOnlyList<Action>? comboActions, Action? executed = null)
        {
            int requested = GetRequestedLayerCount(action, group);
            if (requested < 1) requested = 1;

            if (comboActions == null || comboActions.Count == 0 || executed == null)
                return requested;

            int remaining = ActionUtilities.CountRemainingComboActionsAfter(executed, comboActions);
            return remaining >= 0 ? Math.Min(requested, remaining) : requested;
        }

        /// <summary>Display count for ACTION xN card / tooltip lines.</summary>
        public static int GetDisplayCount(Action? action, ActionAttackBonusGroup? group)
            => GetRequestedLayerCount(action, group);

        /// <summary>
        /// After Duration edits, keep keyword bonus group counts aligned with <see cref="ActionData.ComboBonusDuration"/>.
        /// </summary>
        public static void SyncBonusGroupCountsFromDuration(ActionData? data)
        {
            if (data?.ActionAttackBonuses?.BonusGroups == null || data.ComboBonusDuration <= 0)
                return;

            foreach (var group in data.ActionAttackBonuses.BonusGroups)
            {
                if (IsKeywordCadenceGroup(group))
                    group.Count = data.ComboBonusDuration;
            }
        }

        public static void SyncBonusGroupCountsFromDuration(Action? action)
        {
            if (action?.ActionAttackBonuses?.BonusGroups == null || action.ComboBonusDuration <= 0)
                return;

            foreach (var group in action.ActionAttackBonuses.BonusGroups)
            {
                if (IsKeywordCadenceGroup(group))
                    group.Count = action.ComboBonusDuration;
            }
        }
    }
}
