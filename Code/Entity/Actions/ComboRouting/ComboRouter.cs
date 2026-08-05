using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;

namespace RPGGame.Entity.Actions.ComboRouting
{
    /// <summary>
    /// Handles combo flow control (jump, skip, repeat, loop, stop, disable, random)
    /// plus fight-scoped strip mutations from <see cref="StripMutationState"/>.
    /// </summary>
    public class ComboRouter
    {
        public enum RoutingAction
        {
            None,
            JumpToSlot,
            JumpRelative,
            SkipNext,
            RepeatPrevious,
            LoopToStart,
            StopEarly,
            DisableSlot,
            RandomAction
        }

        /// <summary>
        /// Routes combo execution based on pending strip mutations, then action properties.
        /// </summary>
        public static RoutingResult RouteCombo(Character character, Action currentAction, int currentSlotIndex, List<Action> comboSequence)
        {
            var result = new RoutingResult { ContinueCombo = true, NextSlotIndex = currentSlotIndex + 1 };
            int count = comboSequence?.Count ?? 0;
            if (count == 0)
                return result;

            var strip = CombatTriggerContext.GetStripState(character);

            // Fight-scoped pending routing from trigger mechanics (consumed once).
            if (strip != null && strip.HasPendingRouting)
            {
                result = ApplyRoutingAction(strip.PendingRoutingAction, strip.PendingJumpToSlot1Based, currentSlotIndex, count, character);
                strip.ClearPendingRouting();
                result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
                return result;
            }

            // Always-on ComboRouting fields
            if (currentAction?.ComboRouting != null)
            {
                if (currentAction.ComboRouting.JumpToSlot > 0)
                {
                    result.NextSlotIndex = Math.Min(currentAction.ComboRouting.JumpToSlot - 1, count - 1);
                    result.RoutingAction = RoutingAction.JumpToSlot;
                    result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
                    return result;
                }

                if (currentAction.ComboRouting.JumpRelativeSlots > 0)
                {
                    int next = currentSlotIndex + 1 + currentAction.ComboRouting.JumpRelativeSlots;
                    result.NextSlotIndex = Math.Min(next, count - 1);
                    result.RoutingAction = RoutingAction.JumpRelative;
                    result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
                    return result;
                }

                if (currentAction.ComboRouting.SkipNext)
                {
                    result.NextSlotIndex = currentSlotIndex + 2;
                    result.RoutingAction = RoutingAction.SkipNext;
                    result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
                    return result;
                }

                if (currentAction.ComboRouting.RepeatPrevious && currentSlotIndex > 0)
                {
                    result.NextSlotIndex = currentSlotIndex - 1;
                    result.RoutingAction = RoutingAction.RepeatPrevious;
                    result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: false);
                    return result;
                }

                if (currentAction.ComboRouting.LoopToStart)
                {
                    result.NextSlotIndex = 0;
                    result.RoutingAction = RoutingAction.LoopToStart;
                    result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
                    return result;
                }

                if (currentAction.ComboRouting.StopEarly)
                {
                    result.ContinueCombo = false;
                    result.RoutingAction = RoutingAction.StopEarly;
                    return result;
                }

                if (currentAction.ComboRouting.RandomAction)
                {
                    result.NextSlotIndex = PickRandomEnabledSlot(strip, count);
                    result.RoutingAction = RoutingAction.RandomAction;
                    return result;
                }

                // DisableSlot on the action: disable current slot for the fight, then advance past it
                if (currentAction.ComboRouting.DisableSlot)
                {
                    strip ??= CombatTriggerContext.GetOrCreateStripState(character);
                    strip.DisableSlot(currentSlotIndex);
                    result.NextSlotIndex = currentSlotIndex + 1;
                    result.RoutingAction = RoutingAction.DisableSlot;
                    result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
                    return result;
                }
            }

            // Default advance, then skip disabled
            result.NextSlotIndex = SkipDisabledSlots(strip, currentSlotIndex + 1, count, preferForward: true);

            // Shuffle overlay: map logical next through permutation when present
            if (strip?.ShufflePermutation != null && strip.ShufflePermutation.Count == count)
            {
                int logical = (currentSlotIndex + 1) % count;
                result.NextSlotIndex = strip.LogicalToPhysical(logical, count);
                result.NextSlotIndex = SkipDisabledSlots(strip, result.NextSlotIndex, count, preferForward: true);
            }

            return result;
        }

        private static RoutingResult ApplyRoutingAction(
            RoutingAction action,
            int jumpToSlot1Based,
            int currentSlotIndex,
            int count,
            Character character)
        {
            var result = new RoutingResult { ContinueCombo = true, NextSlotIndex = currentSlotIndex + 1, RoutingAction = action };
            switch (action)
            {
                case RoutingAction.JumpToSlot:
                    result.NextSlotIndex = Math.Min(Math.Max(jumpToSlot1Based - 1, 0), count - 1);
                    break;
                case RoutingAction.SkipNext:
                    result.NextSlotIndex = currentSlotIndex + 2;
                    break;
                case RoutingAction.RepeatPrevious:
                    result.NextSlotIndex = Math.Max(0, currentSlotIndex - 1);
                    break;
                case RoutingAction.LoopToStart:
                    result.NextSlotIndex = 0;
                    break;
                case RoutingAction.StopEarly:
                    result.ContinueCombo = false;
                    break;
                case RoutingAction.RandomAction:
                    result.NextSlotIndex = PickRandomEnabledSlot(CombatTriggerContext.GetStripState(character), count);
                    break;
                default:
                    result.NextSlotIndex = currentSlotIndex + 1;
                    break;
            }

            if (result.NextSlotIndex >= count)
                result.NextSlotIndex %= count;
            return result;
        }

        /// <summary>
        /// Advances from <paramref name="start"/> until a non-disabled slot is found (wraps once).
        /// </summary>
        public static int SkipDisabledSlots(StripMutationState? strip, int start, int count, bool preferForward)
        {
            if (count <= 0)
                return 0;
            int idx = start;
            if (idx < 0) idx = 0;
            if (idx >= count) idx %= count;

            if (strip == null || strip.DisabledSlots.Count == 0)
                return idx;

            for (int i = 0; i < count; i++)
            {
                int candidate = preferForward
                    ? (idx + i) % count
                    : (idx - i + count * 2) % count;
                if (!strip.IsSlotDisabled(candidate))
                    return candidate;
            }

            return idx;
        }

        private static int PickRandomEnabledSlot(StripMutationState? strip, int count)
        {
            if (count <= 0)
                return 0;
            var enabled = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (strip == null || !strip.IsSlotDisabled(i))
                    enabled.Add(i);
            }
            if (enabled.Count == 0)
                return 0;
            int roll = Dice.Roll(1, enabled.Count);
            return enabled[roll - 1];
        }
    }

    public class RoutingResult
    {
        public bool ContinueCombo { get; set; } = true;
        public int NextSlotIndex { get; set; } = 0;
        public ComboRouter.RoutingAction RoutingAction { get; set; } = ComboRouter.RoutingAction.None;
    }
}
