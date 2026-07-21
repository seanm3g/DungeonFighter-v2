using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Entity.Actions.ComboRouting;
using RPGGame.Utils;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Applies strip_* trigger mechanics onto fight-scoped <see cref="StripMutationState"/>.
    /// </summary>
    public static class StripMutationApplier
    {
        public static bool IsStripMechanic(string? mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId ?? "");
            return id.StartsWith("strip_", StringComparison.OrdinalIgnoreCase)
                   || id is "combo_jump" or "loop_chain" or "shuffle" or "replace_action" or "skip";
        }

        /// <summary>
        /// Applies a strip mechanic. Returns true when state changed.
        /// Optional arg from <c>strip_jump:3</c> or <c>strip_replace_next:SLASH</c>.
        /// </summary>
        public static bool TryApply(
            string mechanicId,
            string? mechanicArg,
            string? bundleCount,
            Action action,
            Actor source,
            List<string> messages)
        {
            if (source is not Character character)
                return false;

            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            // Legacy aliases
            id = id switch
            {
                "combo_jump" => "strip_jump",
                "loop_chain" => "strip_loop",
                "shuffle" => "strip_shuffle",
                "replace_action" => "strip_replace_next",
                "skip" => "strip_skip",
                _ => id
            };

            var state = CombatTriggerContext.GetOrCreateStripState(character);
            var combo = ActionUtilities.GetComboActions(character);
            int stripLen = combo.Count;
            int currentSlot = stripLen > 0 ? character.ComboStep % stripLen : 0;

            switch (id)
            {
                case "strip_jump":
                {
                    int slot1 = ParsePositive(mechanicArg, bundleCount, fallback: action.ComboRouting?.JumpToSlot ?? 0);
                    if (slot1 <= 0)
                        return false;
                    state.SetPendingRouting(ComboRouter.RoutingAction.JumpToSlot, slot1);
                    messages.Add($"{character.Name}'s combo jumps to slot {slot1}.");
                    return true;
                }
                case "strip_skip":
                    state.SetPendingRouting(ComboRouter.RoutingAction.SkipNext);
                    messages.Add($"{character.Name} skips the next combo slot.");
                    return true;
                case "strip_repeat":
                    state.SetPendingRouting(ComboRouter.RoutingAction.RepeatPrevious);
                    messages.Add($"{character.Name} repeats the previous combo slot.");
                    return true;
                case "strip_loop":
                    state.SetPendingRouting(ComboRouter.RoutingAction.LoopToStart);
                    messages.Add($"{character.Name}'s combo loops to the opener.");
                    return true;
                case "strip_stop":
                    state.SetPendingRouting(ComboRouter.RoutingAction.StopEarly);
                    messages.Add($"{character.Name}'s combo stops early.");
                    return true;
                case "strip_random":
                    state.SetPendingRouting(ComboRouter.RoutingAction.RandomAction);
                    messages.Add($"{character.Name}'s next combo slot is randomized.");
                    return true;
                case "strip_disable":
                {
                    int slot0 = ParsePositive(mechanicArg, bundleCount, fallback: currentSlot + 1) - 1;
                    if (slot0 < 0)
                        slot0 = currentSlot;
                    state.DisableSlot(slot0);
                    messages.Add($"{character.Name} disables combo slot {slot0 + 1} for this fight.");
                    return true;
                }
                case "strip_shuffle":
                {
                    if (stripLen <= 1)
                        return false;
                    var order = Enumerable.Range(0, stripLen).ToList();
                    // Fisher–Yates via project Dice
                    for (int i = order.Count - 1; i > 0; i--)
                    {
                        int j = Dice.Roll(1, i + 1) - 1;
                        (order[i], order[j]) = (order[j], order[i]);
                    }
                    state.ShufflePermutation = order;
                    messages.Add($"{character.Name}'s combo strip is shuffled for this fight.");
                    return true;
                }
                case "strip_replace_next":
                {
                    string? name = mechanicArg;
                    if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(bundleCount)
                        && !int.TryParse(bundleCount.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                        name = bundleCount.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        // Fall back: next physical strip slot's action name
                        if (stripLen > 0)
                        {
                            int next = (currentSlot + 1) % stripLen;
                            name = combo[next].Name;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(name))
                        return false;
                    state.ReplaceNextActionName = name.Trim();
                    messages.Add($"{character.Name}'s next swing becomes {state.ReplaceNextActionName}.");
                    return true;
                }
                default:
                    return false;
            }
        }

        public static bool TryConsumeReplaceNext(Actor source, out Action? replacement)
        {
            replacement = null;
            if (source is not Character character)
                return false;
            var state = CombatTriggerContext.GetStripState(character);
            if (state == null || string.IsNullOrWhiteSpace(state.ReplaceNextActionName))
                return false;

            string wanted = state.ReplaceNextActionName!;
            state.ReplaceNextActionName = null;

            var combo = ActionUtilities.GetComboActions(character);
            replacement = combo.FirstOrDefault(a =>
                string.Equals(a.Name, wanted, StringComparison.OrdinalIgnoreCase));
            if (replacement != null)
                return true;

            foreach (var act in character.GetActionPool())
            {
                if (act != null && string.Equals(act.Name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    replacement = act;
                    return true;
                }
            }

            return false;
        }

        private static int ParsePositive(string? arg, string? count, int fallback)
        {
            if (int.TryParse((arg ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) && a > 0)
                return a;
            if (int.TryParse((count ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) && c > 0)
                return c;
            return fallback;
        }
    }
}
