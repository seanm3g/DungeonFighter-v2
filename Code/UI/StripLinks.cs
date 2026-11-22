using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions;
using RPGGame.Data;

namespace RPGGame.UI
{
    /// <summary>One arrow in the strip editor: the card in <see cref="From"/> puts something on the card in <see cref="To"/> (0-based).</summary>
    public readonly record struct StripLink(int From, int To, string Label);

    /// <summary>
    /// The arrows between linked cards in the strip editor (Phase 5 proposal 7, rebuild step 3.1): a card whose ACTION
    /// grant lands on another slot points at the slot(s) it affects, following the same placement the combat code uses
    /// (<see cref="CadenceTiming.ResolveActionSlots"/>, the slot / delay / charge / slotIf timing, and "next N actions"
    /// clipped at the strip end, a finisher's ×1 carrying to the opener). Pure: reads the strip, changes nothing.
    /// </summary>
    public static class StripLinks
    {
        public static List<StripLink> Build(IReadOnlyList<Action> strip, Character? hero = null)
        {
            var links = new List<StripLink>();
            if (strip == null || strip.Count < 2)
                return links;
            int n = strip.Count;
            for (int i = 0; i < n; i++)
            {
                var action = strip[i];
                var groups = action?.ActionAttackBonuses?.BonusGroups;
                if (groups == null)
                    continue;
                foreach (var group in groups)
                {
                    if (group == null || !CadenceKeywords.IsAction(CadenceKeywords.NormalizeCadenceType(
                            string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType)))
                        continue;
                    string label = CadenceBonusDisplayFormatter.FormatBonusItemsShort(group.Bonuses ?? new List<ActionAttackBonusItem>());
                    foreach (int to in TargetSlots(action!, group, i, n))
                    {
                        if (to != i && !links.Any(l => l.From == i && l.To == to))
                            links.Add(new StripLink(i, to, label));
                    }
                }
            }
            return links;
        }

        /// <summary>0-based slots one ACTION group from the card at <paramref name="slot"/> lands on, on a strip of <paramref name="n"/>.</summary>
        public static List<int> TargetSlots(Action action, ActionAttackBonusGroup group, int slot, int n)
        {
            var timing = group.Timing;
            if (timing != null && !timing.SlotIfHolds(slot, n))
                return new List<int>();
            int requested = Math.Max(1, ActionCadenceDurationResolver.GetRequestedLayerCount(action, group));
            // Charge: every copy on the next combo action.
            if (timing?.IsCharge == true)
                return slot + 1 < n ? new List<int> { slot + 1 } : new List<int>();
            // Explicit placement (slot / delay) may wrap into the next combo cycle.
            if (!string.IsNullOrWhiteSpace(timing?.Slot) || (timing?.Delay ?? 0) > 0)
                return CadenceTiming.ResolveActionSlots(timing, slot, n, requested).Distinct().ToList();
            // "Next N actions", clipped to the slots left in this cycle; a finisher's ×1 carries to the opener.
            int left = n - 1 - slot;
            int granted = Math.Min(requested, left);
            if (granted <= 0)
                return requested <= 1 ? new List<int> { 0 } : new List<int>();
            return CadenceTiming.ResolveActionSlots(timing, slot, n, granted).Distinct().ToList();
        }
    }
}
