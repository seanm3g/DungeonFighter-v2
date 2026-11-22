using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Fragile cards and the strip effects Jam / Burn out (Documentation/CORE_DESIGN_PASS_2026-11-17.md, Phase 10 step 10;
    /// D5.4, D5.7, D3.15). A <b>Fragile N</b> card works N swings a fight, then burns out. A burned-out card leaves the live
    /// strip for the rest of the fight, so the strip <b>closes up</b>: <see cref="Character.GetComboActions"/> returns the
    /// live cards only while the fight lasts, and the combo, its slot count and its thresholds are one card shorter.
    /// <b>Burn out</b> (strip op <c>strip_burnout</c>) burns out the foe's last live card; <b>Jam</b> (<c>strip_jam</c>)
    /// makes the foe's next card get skipped once. The last live card never burns out.
    /// </summary>
    public static class FragileCards
    {
        /// <summary>Fight-scoped: true between <see cref="CombatTriggerContext.ResetForBattle"/> and
        /// <see cref="CombatTriggerContext.EndBattle"/>. Outside a fight the strip is always whole (strip editor, saves).</summary>
        internal static bool FightActive { get; set; }

        /// <summary>The live strip: <paramref name="full"/> minus this fight's burned-out cards.</summary>
        public static List<Action> LiveStrip(Actor owner, List<Action> full)
        {
            if (!FightActive || full.Count == 0)
                return full;
            var state = CombatTriggerContext.GetStripState(owner);
            if (state == null || state.BurnedOut.Count == 0)
                return full;
            var live = new List<Action>(full.Count);
            foreach (var a in full)
            {
                if (a == null || !state.BurnedOut.Contains(a))
                    live.Add(a!);
            }
            return live.Count > 0 ? live : full;
        }

        /// <summary>True when <paramref name="card"/> burned out this fight.</summary>
        public static bool IsBurnedOut(Actor owner, Action card) =>
            FightActive && CombatTriggerContext.GetStripState(owner)?.BurnedOut.Contains(card) == true;

        /// <summary>Swings a Fragile card has left this fight (its full count before the first swing).</summary>
        public static int UsesLeft(Actor owner, Action card)
        {
            if (card.Fragile <= 0)
                return 0;
            int used = 0;
            if (FightActive)
                CombatTriggerContext.GetStripState(owner)?.FragileUses.TryGetValue(card, out used);
            return Math.Max(0, card.Fragile - used);
        }

        /// <summary>
        /// Counts one swing of a strip card and burns a Fragile card out on its last use. Called once per top-level swing,
        /// after the combo step moved, so the close-up re-points the step at the right card.
        /// </summary>
        public static bool AfterSwing(Actor source, Action? swung, List<string>? messages)
        {
            if (!FightActive || swung == null || swung.Fragile <= 0 || source is not Character holder)
                return false;
            var live = holder.GetComboActions();
            var card = FindCard(live, swung);
            if (card == null)
                return false;
            var state = CombatTriggerContext.GetOrCreateStripState(holder);
            state.FragileUses.TryGetValue(card, out int used);
            used++;
            state.FragileUses[card] = used;
            if (used < card.Fragile)
                return false;
            return BurnOut(holder, card, messages, cause: null);
        }

        /// <summary>
        /// Burns <paramref name="card"/> out of <paramref name="holder"/>'s live strip for the rest of the fight, unless it is
        /// the last live card. Raises <see cref="CardBurnedOut"/>.
        /// </summary>
        public static bool BurnOut(Character holder, Action card, List<string>? messages, string? cause)
        {
            if (!FightActive)
                return false;
            var live = holder.GetComboActions();
            int index = IndexOfRef(live, card);
            if (index < 0 || live.Count <= 1)
                return false;

            int count = live.Count;
            int step = holder.ComboStep;
            int cycles = count > 0 ? step / count : 0;
            int slot = count > 0 ? step % count : 0;

            var state = CombatTriggerContext.GetOrCreateStripState(holder);
            state.BurnedOut.Add(card);

            int newCount = count - 1;
            int newSlot = slot > index ? slot - 1 : slot == index ? index % newCount : slot;
            int newStep = cycles * newCount + newSlot;
            if (step > 0 && newStep == 0)
                newStep = newCount; // still mid-chain, now pointing at the opener
            holder.ComboStep = newStep;

            messages?.Add(cause == null
                ? $"{holder.Name}'s {card.Name} burns out. The strip closes up."
                : $"{cause} burns out {holder.Name}'s {card.Name}. The strip closes up.");
            CardBurnedOut?.Invoke(holder, card);
            return true;
        }

        /// <summary>Burn out works on a strip of this many live cards or more: a rare counter to long strips (D3.15).</summary>
        public const int BurnOutMinStrip = 3;

        /// <summary>Burn out strip effect: the holder's last live card burns out for the fight (strips of
        /// <see cref="BurnOutMinStrip"/>+ only).</summary>
        public static bool BurnOutLast(Actor holder, string cause, List<string> messages)
        {
            if (holder is not Character c)
                return false;
            var live = c.GetComboActions();
            if (live.Count < BurnOutMinStrip)
                return false;
            return BurnOut(c, live[^1], messages, cause);
        }

        /// <summary>Jam strip effect: the holder's next card is skipped once (the combo jumps past it).</summary>
        public static bool Jam(Actor holder, string cause, List<string> messages)
        {
            if (holder is not Character c)
                return false;
            var live = c.GetComboActions();
            if (live.Count <= 1)
                return false;
            int count = live.Count;
            int slot = c.ComboStep % count;
            int next = (slot + 1) % count;
            CombatTriggerContext.GetOrCreateStripState(c).RecordAdvance(slot - 1, slot + 1);
            c.ComboStep = c.ComboStep + 1;
            messages.Add($"{cause} jams {c.Name}'s {live[slot].Name}; the combo jumps to {live[next].Name}.");
            return true;
        }

        /// <summary>Raised when a card burns out (the build-moment callout and Fernis listen).</summary>
        public static event System.Action<Character, Action>? CardBurnedOut;

        private static Action? FindCard(List<Action> live, Action swung)
        {
            int i = IndexOfRef(live, swung);
            if (i >= 0)
                return live[i];
            return live.FirstOrDefault(a => a != null && a.Fragile > 0
                && string.Equals(a.Name, swung.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static int IndexOfRef(List<Action> list, Action card)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], card))
                    return i;
            }
            return -1;
        }
    }
}
