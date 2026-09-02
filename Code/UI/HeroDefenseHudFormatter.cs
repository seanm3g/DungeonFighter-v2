using System;
using RPGGame.Combat.Calculators;

namespace RPGGame
{
    /// <summary>
    /// Player-facing leftover energy, BLOCK %, class DEFENSE, and action ENERGY strings for HUD and tooltips.
    /// </summary>
    public static class HeroDefenseHudFormatter
    {
        public static string FormatLeftoverBlockLine(Character hero)
        {
            int leftover = Math.Max(0, hero.LeftoverEnergy);
            int block = Percent(ClassDefenseCalculator.GetBlockPercent(leftover));
            return $"Leftover {leftover}  BLOCK {block}%";
        }

        public static string FormatClassLayerLine(Character hero)
        {
            var lines = ClassDefenseCalculator.FormatHudLines(hero, pierce: false);
            if (lines.Count < 3)
                return "";
            string raw = lines[2];
            if (raw.StartsWith("dodge ", StringComparison.Ordinal))
                return "Dodge " + raw.Substring("dodge ".Length);
            if (raw.StartsWith("shield ", StringComparison.Ordinal))
                return "Shield " + raw.Substring("shield ".Length);
            return raw;
        }

        public static string FormatActionEnergySuffix(Action action) =>
            $"E{LeftoverEnergy.ResolveCost(action)}";

        public static string FormatActionEnergyTooltip(Action action)
        {
            int cost = LeftoverEnergy.ResolveCost(action);
            int leftover = LeftoverEnergy.LeftoverFromCost(cost);
            int block = Percent(ClassDefenseCalculator.GetBlockPercent(leftover));
            if (leftover <= 0)
                return $"Energy {cost} (leftover 0, DEFENSE only)";
            return $"Energy {cost} (leftover {leftover} → BLOCK {block}%)";
        }

        private static int Percent(double fraction) =>
            (int)Math.Round(fraction * 100.0, MidpointRounding.AwayFromZero);
    }
}
