using System;
using System.Collections.Generic;
using RPGGame.Entity.Actions.ComboRouting;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Fight-scoped combo-strip mutations (disable, shuffle overlay, pending routing, replace-next).
    /// Reset with <see cref="CombatTriggerContext.ResetForBattle"/>.
    /// </summary>
    public sealed class StripMutationState
    {
        public HashSet<int> DisabledSlots { get; } = new();

        /// <summary>
        /// When non-null, maps logical play order index → physical strip index for the fight.
        /// Length must match strip length when applied.
        /// </summary>
        public List<int>? ShufflePermutation { get; set; }

        public bool HasPendingRouting { get; set; }
        public ComboRouter.RoutingAction PendingRoutingAction { get; set; } = ComboRouter.RoutingAction.None;
        /// <summary>1-based slot for <see cref="ComboRouter.RoutingAction.JumpToSlot"/>; otherwise unused.</summary>
        public int PendingJumpToSlot1Based { get; set; }

        /// <summary>One-shot: next resolve uses this pool/strip action name, then clears.</summary>
        public string? ReplaceNextActionName { get; set; }

        public void Clear()
        {
            DisabledSlots.Clear();
            ShufflePermutation = null;
            ClearPendingRouting();
            ReplaceNextActionName = null;
        }

        public void ClearPendingRouting()
        {
            HasPendingRouting = false;
            PendingRoutingAction = ComboRouter.RoutingAction.None;
            PendingJumpToSlot1Based = 0;
        }

        public void SetPendingRouting(ComboRouter.RoutingAction action, int jumpToSlot1Based = 0)
        {
            HasPendingRouting = true;
            PendingRoutingAction = action;
            PendingJumpToSlot1Based = jumpToSlot1Based;
        }

        public bool IsSlotDisabled(int slotIndex) => DisabledSlots.Contains(slotIndex);

        public void DisableSlot(int slotIndex)
        {
            if (slotIndex >= 0)
                DisabledSlots.Add(slotIndex);
        }

        /// <summary>
        /// Maps a physical strip index through the shuffle overlay for display/order walks.
        /// </summary>
        public int MapPhysicalIndex(int physicalIndex, int stripLength)
        {
            if (ShufflePermutation == null || ShufflePermutation.Count != stripLength)
                return physicalIndex;
            if (physicalIndex < 0 || physicalIndex >= stripLength)
                return physicalIndex;
            return ShufflePermutation[physicalIndex];
        }

        /// <summary>
        /// Inverse: given a logical order position, which physical slot fires.
        /// </summary>
        public int LogicalToPhysical(int logicalIndex, int stripLength)
        {
            if (ShufflePermutation == null || ShufflePermutation.Count != stripLength)
                return logicalIndex;
            if (logicalIndex < 0 || logicalIndex >= stripLength)
                return logicalIndex;
            // ShufflePermutation[i] = physical at logical i
            return ShufflePermutation[logicalIndex];
        }
    }
}
