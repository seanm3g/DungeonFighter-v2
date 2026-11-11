using System;
using System.Collections.Generic;
using System.Globalization;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame
{
    /// <summary>
    /// Totals for the whole combo sequence, from the same per-card numbers the strip shows.
    /// </summary>
    public readonly struct ActionSequenceSummary
    {
        public int ActionCount { get; }
        /// <summary>Sum of every card's damage × hits (one full pass through the sequence).</summary>
        public int TotalDamage { get; }
        /// <summary>Sum of each card's own swing seconds (one action per turn, no chaining).</summary>
        public double SoloSeconds { get; }
        /// <summary>Average card swing seconds.</summary>
        public double AverageSwingSeconds { get; }
        /// <summary>Combo growth g from <c>ComboSystemConfig.ComboChainGrowth</c>.</summary>
        public double Growth { get; }
        /// <summary>1 + g × (n − 1): what the average swing is multiplied by when every combo roll lands.</summary>
        public double ChainFactor { get; }
        /// <summary>Full-chain time: <see cref="ActionSpeedSystem.ChainCost"/> over all cards (combo speed).</summary>
        public double ComboSeconds { get; }

        public double ComboDamagePerSecond => ComboSeconds > 0 ? TotalDamage / ComboSeconds : 0;
        public double SoloDamagePerSecond => SoloSeconds > 0 ? TotalDamage / SoloSeconds : 0;

        public ActionSequenceSummary(int actionCount, int totalDamage, double soloSeconds, double growth)
        {
            ActionCount = actionCount;
            TotalDamage = totalDamage;
            SoloSeconds = soloSeconds;
            AverageSwingSeconds = actionCount > 0 ? soloSeconds / actionCount : 0;
            Growth = Math.Clamp(growth, 0.0, 1.0);
            ChainFactor = actionCount > 0 ? 1.0 + Growth * (actionCount - 1) : 0;
            ComboSeconds = ActionSpeedSystem.ChainCost(soloSeconds, actionCount, Growth);
        }
    }

    public static partial class CombatActionStripBuilder
    {
        /// <summary>
        /// Sums the sequence cards (effective damage with combo amp, card seconds) and applies the combo chain formula
        /// avg swing × (1 + g × (n − 1)) to get the full-combo time, i.e. the time when every combo roll lands.
        /// </summary>
        public static ActionSequenceSummary BuildSequenceSummary(Character? character)
        {
            double growth = GameConfiguration.Instance.ComboSystem?.ComboChainGrowth ?? 0.25;
            if (character == null)
                return new ActionSequenceSummary(0, 0, 0, growth);

            var combo = character.GetComboActions();
            var panels = BuildPanelData(character);
            int n = Math.Min(combo?.Count ?? 0, panels.Count);
            long damage = 0;
            double seconds = 0;
            for (int i = 0; i < n; i++)
            {
                var info = panels[i];
                GetStripSwingDisplayValues(in info, character, combo![i], ActionStripDamageLineMode.EffectiveWithComboAmp,
                    out int dmg, out double secs, i);
                damage += (long)Math.Max(0, dmg) * info.EffectiveMultiHitCount;
                seconds += Math.Max(0, secs);
            }
            return new ActionSequenceSummary(n, (int)Math.Min(int.MaxValue, damage), seconds, growth);
        }

        /// <summary>
        /// One-line readout for the sequence editor, e.g.
        /// <c>Full combo: 1240 dmg in 14.2s (avg 9.5s x 1.75)  |  87 dmg/s  (one per turn: 38.0s, 33 dmg/s)</c>.
        /// </summary>
        public static string FormatSequenceSummaryLine(in ActionSequenceSummary s)
        {
            if (s.ActionCount <= 0)
                return "Full combo: (empty sequence)";
            var c = CultureInfo.InvariantCulture;
            string line = $"Full combo: {s.TotalDamage} dmg in {s.ComboSeconds.ToString("0.0", c)}s"
                + $" (avg {s.AverageSwingSeconds.ToString("0.0", c)}s x {s.ChainFactor.ToString("0.00", c)})"
                + $"  |  {s.ComboDamagePerSecond.ToString("0.#", c)} dmg/s";
            if (s.ActionCount > 1)
                line += $"  (one per turn: {s.SoloSeconds.ToString("0.0", c)}s, {s.SoloDamagePerSecond.ToString("0.#", c)} dmg/s)";
            return line;
        }
    }
}
