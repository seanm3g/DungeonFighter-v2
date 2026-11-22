using System;
using System.Collections.Generic;
using RPGGame;
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

        /// <summary>Cards burned out this fight (Fragile, Burn out); by reference, so two copies of a card burn apart.</summary>
        public HashSet<Action> BurnedOut { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>Swings each Fragile card has used this fight.</summary>
        public Dictionary<Action, int> FragileUses { get; } = new(ReferenceEqualityComparer.Instance);

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

        /// <summary>
        /// Bit per strip slot the current combo run jumped over (STRIP JUMP / SKIP): those cards never fired, so the strip
        /// must not draw them lit. One long so the UI thread reads it atomically while combat writes it.
        /// </summary>
        public long SkippedThisRunMask { get; private set; }

        public bool WasSkippedThisRun(int slotIndex) =>
            slotIndex >= 0 && slotIndex < 64 && (SkippedThisRunMask & (1L << slotIndex)) != 0;

        /// <summary>
        /// Records a strip advance from <paramref name="fromSlot"/> to <paramref name="toSlot"/>: a new run (from the
        /// opener, or a wrap / loop back) starts clean; a forward jump marks the slots in between as skipped.
        /// </summary>
        public void RecordAdvance(int fromSlot, int toSlot)
        {
            long mask = fromSlot <= 0 || toSlot <= fromSlot ? 0 : SkippedThisRunMask;
            for (int s = Math.Max(0, fromSlot + 1); s < toSlot && s < 64; s++)
                mask |= 1L << s;
            SkippedThisRunMask = mask;
        }

        public void Clear()
        {
            SkippedThisRunMask = 0;
            DisabledSlots.Clear();
            BurnedOut.Clear();
            FragileUses.Clear();
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
