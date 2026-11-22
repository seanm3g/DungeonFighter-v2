using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI
{
    /// <summary>What a <see cref="NowCardContent"/> line is about (the board colors it by kind).</summary>
    public enum NowCardLineKind
    {
        /// <summary>A status on the swinger or its target that changes this swing.</summary>
        Status,
        /// <summary>A queued grant or charge this swing uses (cadence banks, TURN grants, Lucky, rerolls).</summary>
        Grant,
        /// <summary>A store this swing spends, or the stores it could scale with.</summary>
        Store,
        /// <summary>The odds of the next link.</summary>
        Odds
    }

    public readonly record struct NowCardLine(string Text, NowCardLineKind Kind);

    /// <summary>
    /// Everything attached to the turn on the NOW card (D3.7, Phase 9 proposal 3): the statuses that apply to this swing
    /// (on the swinger and on its target), queued grants on this slot (per-slot ACTION banks, the next-action bank, TURN
    /// grants), charges it uses (Lucky, Guarantee hit, Reroll), the stores it spends or scales, and the odds of the next
    /// link. Pure: reads state, changes nothing; the board draws the lines under NOW.
    /// </summary>
    public static class NowCardContent
    {
        /// <param name="actor">Who swings.</param>
        /// <param name="target">Who the swing lands on (null for a room hazard).</param>
        /// <param name="action">The card on NOW (null when it isn't known yet).</param>
        /// <param name="slot">0-based strip slot of <paramref name="action"/>, or -1.</param>
        /// <param name="nextLinkFace">The lowest d20 face that links (the combo band's bottom) for the actor's next roll; null to leave the odds off.</param>
        public static List<NowCardLine> Build(Actor actor, Actor? target, Action? action, int slot, int? nextLinkFace = null)
        {
            var lines = new List<NowCardLine>();
            if (actor == null)
                return lines;
            AddSwingerStatuses(lines, actor);
            if (target != null && !ReferenceEquals(target, actor))
                AddTargetStatuses(lines, target);
            if (actor is Character ch)
            {
                AddGrants(lines, ch, action, slot);
                AddStores(lines, ch, target, action);
            }
            if (nextLinkFace is int face)
                lines.Add(new NowCardLine(NextLinkLine(face), NowCardLineKind.Odds));
            return lines;
        }

        /// <summary>"Next link: 35% (14+)": the chance a d20 lands at or above <paramref name="face"/>.</summary>
        public static string NextLinkLine(int face)
        {
            int f = Math.Clamp(face, 1, 21);
            int pct = (21 - f) * 5;
            return f > 20 ? "Next link: 0% (can't link)" : $"Next link: {pct}% ({f}+)";
        }

        private static CombatConfig Cfg => GameConfiguration.Instance.Combat;

        private static string Pct(double fraction) => (fraction * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

        private static int Capped(int stacks)
        {
            int cap = Cfg.StatusMaxStacks;
            return cap > 0 ? Math.Min(stacks, cap) : stacks;
        }

        private static void AddSwingerStatuses(List<NowCardLine> lines, Actor a)
        {
            int focus = a.FocusTurns > 0 ? Capped(a.FocusStacks ?? 0) : 0;
            if (focus > 0)
                lines.Add(new NowCardLine($"Focus {focus}: +{Pct(focus * Cfg.StatusFocusDamagePerStack)} damage", NowCardLineKind.Status));
            int rattle = TempoStatuses.Rattle(a);
            if (rattle > 0)
                lines.Add(new NowCardLine($"Rattle {rattle}: -{rattle} to the roll", NowCardLineKind.Status));
            int stagger = TempoStatuses.Stagger(a);
            if (stagger > 0)
                lines.Add(new NowCardLine($"Stagger {stagger}: this action takes +{Pct(stagger * Math.Max(0, Cfg.StatusStaggerLengthPerStack))} longer", NowCardLineKind.Status));
            int haste = TempoStatuses.Haste(a);
            if (haste > 0)
                lines.Add(new NowCardLine($"Haste {haste}: this action is {Pct(haste * Math.Max(0, Cfg.StatusHasteSpeedPerStack))} faster", NowCardLineKind.Status));
        }

        private static void AddTargetStatuses(List<NowCardLine> lines, Actor t)
        {
            string on = string.IsNullOrWhiteSpace(t.Name) ? "the target" : t.Name;
            if (t.IsMarked && t.MarkTurns > 0)
                lines.Add(new NowCardLine($"Mark on {on}: a hit crits", NowCardLineKind.Status));
            int expose = ExposeStatus.IsExposed(t) ? Capped(t.ExposeStacks ?? 0) : 0;
            if (expose > 0)
                lines.Add(new NowCardLine($"Expose {expose} on {on}: it takes more damage", NowCardLineKind.Status));
            int guard = t.HardenTurns > 0 ? Capped(t.HardenStacks ?? 0) : 0;
            if (guard > 0)
                lines.Add(new NowCardLine($"Guard {guard} on {on}: it takes less damage", NowCardLineKind.Status));
        }

        private static void AddGrants(List<NowCardLine> lines, Character c, Action? action, int slot)
        {
            if (slot >= 0)
            {
                string slotItems = CadenceBonusDisplayFormatter.FormatBonusItemsShort(c.Effects.GetPendingActionBonusesForSlot(slot));
                if (!string.IsNullOrEmpty(slotItems))
                    lines.Add(new NowCardLine($"Queued for slot {slot + 1}: {slotItems}", NowCardLineKind.Grant));
            }
            string nextItems = CadenceBonusDisplayFormatter.FormatBonusItemsShort(c.Effects.PeekPendingActionBonusesNextHeroRoll());
            if (!string.IsNullOrEmpty(nextItems))
                lines.Add(new NowCardLine($"Next action: {nextItems}", NowCardLineKind.Grant));
            string turnItems = CadenceBonusDisplayFormatter.FormatBonusItemsShort(c.Effects.PeekTurnBonuses(action));
            if (!string.IsNullOrEmpty(turnItems))
                lines.Add(new NowCardLine($"TURN: {turnItems}", NowCardLineKind.Grant));
            int tempRoll = c.Effects.TempRollBonusTurns > 0 ? c.Effects.GetTempRollBonus() : 0;
            if (tempRoll != 0 && string.IsNullOrEmpty(turnItems))
                lines.Add(new NowCardLine($"Roll {(tempRoll > 0 ? "+" : "")}{tempRoll}", NowCardLineKind.Grant));
            if (c is not Enemy && LeatherSetBonus.HasLuckyStatus(c))
                lines.Add(new NowCardLine(LeatherSetBonus.FormatLuckyStatusLine(c), NowCardLineKind.Grant));
            if (c.Effects.GuaranteeNextSuccess)
                lines.Add(new NowCardLine("Guarantee hit: this roll hits", NowCardLineKind.Grant));
            if (c.Effects.RerollCharges > 0)
                lines.Add(new NowCardLine($"Reroll x{c.Effects.RerollCharges}", NowCardLineKind.Grant));
            if (c.Effects.ExtraAttacks > 0)
                lines.Add(new NowCardLine($"Extra atk x{c.Effects.ExtraAttacks}", NowCardLineKind.Grant));
        }

        private static void AddStores(List<NowCardLine> lines, Character c, Actor? target, Action? action)
        {
            if (c is Enemy)
                return;
            var spec = VerbPresets.GetSpec(action);
            string? spending = null;
            if (spec != null && (spec.Kind == VerbKind.Cash || spec.Kind == VerbKind.Detonate))
            {
                string? id = spec.Kind == VerbKind.Cash ? VerbPresets.ResolveStore(c, spec.Store) : VerbPresets.LargestTargetStatus(target);
                if (id != null)
                {
                    var holder = VerbPresets.Holder(c, target, id);
                    int units = Stores.Get(holder, id);
                    string label = Stores.TryGet(id, out var def) ? def.Label : id;
                    string rate = spec.Rate.ToString("0.#", CultureInfo.InvariantCulture);
                    lines.Add(new NowCardLine(units > 0 ? $"Spends {label} {units} at {rate}%" : $"Spends {label}: empty", NowCardLineKind.Store));
                    spending = id;
                }
                else
                    lines.Add(new NowCardLine(spec.Kind == VerbKind.Cash ? "Spends your fullest store: empty" : "Detonates: no status on the target", NowCardLineKind.Store));
            }
            // The material stores (CRISIS x4 ...) left the status panel: the card shows what the hero is carrying.
            var held = c.Effects.GetMaterialKeywordCounts()
                .Where(kv => kv.Count > 0 && !string.Equals(kv.Keyword, spending, StringComparison.OrdinalIgnoreCase))
                .Select(kv => $"{kv.Keyword} {kv.Count}")
                .ToList();
            if (held.Count > 0)
                lines.Add(new NowCardLine("Stores: " + string.Join(", ", held), NowCardLineKind.Store));
        }
    }
}
