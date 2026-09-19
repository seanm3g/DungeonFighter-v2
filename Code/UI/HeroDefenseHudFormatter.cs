using System;
using RPGGame.Combat.Calculators;

namespace RPGGame
{
    /// <summary>
    /// Player-facing standing BLOCK %, class DEFENSE, and action block strings for HUD and tooltips.
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

        public static string FormatClassLayerLine(Character hero)
        {
            var lines = ClassDefenseCalculator.FormatHudLines(hero, pierce: false);
            if (lines.Count < 2)
                return "";
            string raw = lines[1];
            if (raw.StartsWith("shield ", StringComparison.Ordinal))
                return "Shield " + raw.Substring("shield ".Length);
            if (raw.StartsWith("TEMPO ", StringComparison.Ordinal))
                return "TEMPO " + raw.Substring("TEMPO ".Length);
            if (raw.StartsWith("COUNTER ", StringComparison.Ordinal))
                return "COUNTER " + raw.Substring("COUNTER ".Length);
            if (raw.StartsWith("GRIT ", StringComparison.Ordinal))
                return "GRIT " + raw.Substring("GRIT ".Length);
            return raw;
        }

        public static string FormatActionBlockSuffix(Action action) =>
            $"Block {Percent(StandingBlock.ResolveFromAction(action))}%";

        public static string FormatActionBlockTooltip(Action action)
        {
            int block = Percent(StandingBlock.ResolveFromAction(action));
            if (block <= 0)
                return "Block 0% (DEFENSE only)";
            return $"Block {block}%";
        }

        private static int Percent(double fraction) =>
            (int)Math.Round(fraction * 100.0, MidpointRounding.AwayFromZero);
    }
}
