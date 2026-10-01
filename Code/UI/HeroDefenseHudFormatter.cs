using System;
using RPGGame.Combat.Calculators;

namespace RPGGame
{
    /// <summary>
    /// Player-facing live DR %, action Block % strings, and leftover standing BLOCK helpers.
    /// Left-panel HERO/STATS show live Defense (base × standing BLOCK); % DR is on Defense hover.
    /// </summary>
    public static class HeroDefenseHudFormatter
    {
        public static string FormatStandingBlockLine(Character hero)
        {
            int block = Percent(ClassDefenseCalculator.GetBlockPercent(hero));
            return $"BLOCK {block}%";
        }

        /// <summary>Backward-compatible alias for standing BLOCK line.</summary>
        public static string FormatLeftoverBlockLine(Character hero) => FormatStandingBlockLine(hero);

        public static string FormatClassLayerLine(Character hero) =>
            ClassDefenseCalculator.FormatDrLine(hero);

        /// <summary>Live incoming damage reduction, e.g. <c>12%</c>.</summary>
        public static string FormatDamageReductionPercent(Character hero) =>
            FormatClassLayerLine(hero).Replace("DR ", "", StringComparison.Ordinal);

        /// <summary>Lingering stance on the action card, e.g. <c>result: defensive stance</c>. Empty for neutral.</summary>
        public static string FormatActionBlockSuffix(Action action) =>
            StandingBlock.FormatCardLine(StandingBlock.ResolveFromAction(action));

        public static string FormatActionBlockTooltip(Action action) =>
            FormatActionBlockSuffix(action);

        private static int Percent(double fraction) =>
            (int)Math.Round(fraction * 100.0, MidpointRounding.AwayFromZero);
    }
}
