using System;
using System.Collections.Generic;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Combat.Sequence;

namespace RPGGame.UI
{
    /// <summary>
    /// Rebuild 1.1 (D0.1, D2.2, D2.7, D0.11, D9.6): the numbers the combat board draws, kept out of the renderer so they
    /// can be tested. The combo is a ladder of odds (the strip cards stopped printing the
    /// "needs 16+ · 25%" line after the 11-21 playtest); lit cards escalate per link (the hero loudly, the enemy a bit quieter); the roll ruler shows
    /// the d20 bands with a marker on the roll, its nudge and any extra die.
    /// </summary>
    public static class ComboBoardModel
    {
        // ---------------------------------------------------------------- odds (D2.7)

        /// <summary>Lowest d20 total that fires the card in 0-based strip slot <paramref name="slotIndex"/> (14, then +1 per link).</summary>
        public static int NeededForSlot(Actor actor, int slotIndex)
        {
            var tm = RollModificationManager.GetThresholdManager();
            int stored = tm.GetStoredComboThreshold(actor);
            var combo = GameConfiguration.Instance.ComboSystem;
            int rise = combo == null ? 0 : ThresholdManager.GetComboChainRise(Math.Max(1, slotIndex + 1),
                combo.ComboThresholdRisePerSlot, combo.ComboThresholdRiseEvery, combo.ComboThresholdRiseStartSlot);
            return rise > 0 ? Math.Max(stored, Math.Min(ThresholdManager.MaxChainComboThreshold, stored + rise)) : stored;
        }

        /// <summary>
        /// Chance that d20 + <paramref name="rollBonus"/> reaches <paramref name="needed"/>. A natural 20 always lands and a
        /// natural 1 never does. <paramref name="advantage"/> rolls two dice and keeps the higher (Lucky).
        /// </summary>
        public static double Chance(int needed, int rollBonus = 0, bool advantage = false)
        {
            int faces = 0;
            for (int face = 1; face <= 20; face++)
            {
                if (face == 1) continue;
                if (face == 20 || face + rollBonus >= needed) faces++;
            }
            double p = faces / 20.0;
            return advantage ? 1.0 - (1.0 - p) * (1.0 - p) : p;
        }

        /// <summary>True when <paramref name="actor"/> rolls two dice and keeps the higher (the Leather set's Lucky).</summary>
        public static bool HasExtraDie(Actor actor) =>
            actor is Character hero && actor is not Enemy && LeatherSetBonus.HasLuckAdvantage(hero);

        public static string FormatPercent(double p)
        {
            if (p >= 0.9999) return "100%";
            if (p <= 0.0001) return "0%";
            int pct = (int)Math.Round(p * 100 / 5.0) * 5;
            pct = Math.Clamp(pct, 5, 95);
            return pct + "%";
        }

        // ---------------------------------------------------------------- escalation (D0.1, D0.11, D9.6)

        /// <summary>How far a lit card has escalated: 0 for the first link, rising to 1 at the strip's end.</summary>
        public static double LinkFraction(int linkIndex, int stripLength) =>
            stripLength <= 1 ? 1.0 : Math.Clamp(linkIndex / (double)(stripLength - 1), 0.0, 1.0);

        /// <summary>Border thickness of a lit card: the hero's grows 1 → 4 along the chain, the enemy's only 1 → 2 (quieter).</summary>
        public static int LitBorderThickness(int linkIndex, int stripLength, bool enemy)
        {
            double f = LinkFraction(linkIndex, stripLength);
            return enemy ? 1 + (int)Math.Round(f) : 1 + (int)Math.Round(f * 3);
        }

        /// <summary>How much of the gold finisher color a lit card's border takes: the hero's up to 100%, the enemy's up to 40%.</summary>
        public static double LitGoldShare(int linkIndex, int stripLength, bool enemy) =>
            LinkFraction(linkIndex, stripLength) * (enemy ? 0.4 : 1.0);

        // ---------------------------------------------------------------- roll ruler (D2.2)

        public enum RulerBand { CritMiss, Miss, Hit, Combo, Crit }

        /// <summary>One band on the ruler: its label ("2–5", "14+") and whether the roll landed in it.</summary>
        public readonly record struct RulerSegment(RulerBand Band, string Label, bool Marked);

        /// <summary>
        /// The five bands for <paramref name="t"/> (the HIT band disappears mid-chain, D2.8), with <paramref name="roll"/>
        /// marked. Labels: <c>1</c>, <c>2–5</c>, <c>6–13</c>, <c>14+</c>, <c>20</c>.
        /// </summary>
        public static List<RulerSegment> Ruler(CombatSequenceThresholds t, int roll)
        {
            var list = new List<RulerSegment>();
            int critMiss = Math.Max(1, t.CritMiss);
            int crit = Math.Clamp(t.Crit, 2, 20);
            int combo = Math.Clamp(t.Combo, 2, crit);
            int minHit = Math.Clamp(t.MinHit, critMiss + 1, combo);

            void Add(RulerBand band, int lo, int hi, string? label = null)
            {
                if (hi < lo) return;
                bool marked = roll >= lo && roll <= hi
                    || band == RulerBand.Crit && roll >= crit
                    || band == RulerBand.CritMiss && roll <= critMiss;
                list.Add(new RulerSegment(band, label ?? (lo == hi ? lo.ToString() : $"{lo}–{hi}"), marked));
            }

            Add(RulerBand.CritMiss, 1, critMiss);
            Add(RulerBand.Miss, critMiss + 1, minHit - 1);
            Add(RulerBand.Hit, minHit, combo - 1);
            Add(RulerBand.Combo, combo, crit - 1, combo < crit ? $"{combo}+" : null);
            Add(RulerBand.Crit, crit, crit);
            return list;
        }

        /// <summary>The ruler as one line, the landed band in brackets with the roll: <c>1 │ 2–5 │ [6–13: 9] │ 14+ │ 20</c>.</summary>
        public static string RulerText(CombatSequenceThresholds t, int roll, int nudge = 0, bool extraDie = false)
        {
            var parts = new List<string>();
            foreach (var s in Ruler(t, roll))
                parts.Add(s.Marked ? $"[{s.Label}: {roll}]" : s.Label);
            string line = string.Join(" │ ", parts);
            if (nudge != 0)
                line += $"   {(nudge > 0 ? "+" : "−")}{Math.Abs(nudge)} nudge";
            if (extraDie)
                line += "   2d20 keep high";
            return line;
        }
    }
}
