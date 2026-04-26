using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public static class RarityChanceMath
    {
        private static readonly string[] RarityOrder =
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
        };

        /// <summary>
        /// Builds a geometric rarity distribution based on a base ratio \(r\), where
        /// Common is r× more common than Uncommon, Uncommon is r× more common than Rare, etc.
        /// Returns percent chances that sum to 100 (subject to floating-point precision).
        /// </summary>
        public static Dictionary<string, double> BuildGeometricRarityChancesPercent(double baseRatio)
        {
            if (double.IsNaN(baseRatio) || double.IsInfinity(baseRatio) || baseRatio <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(baseRatio), "Base ratio must be a finite positive number.");

            int n = RarityOrder.Length;
            var weights = new double[n];
            for (int i = 0; i < n; i++)
            {
                // Common should have the largest weight; Mythic the smallest.
                int exponent = (n - 1) - i;
                weights[i] = Math.Pow(baseRatio, exponent);
            }

            double sum = weights.Sum();
            if (sum <= 0.0)
                throw new InvalidOperationException("Could not compute a valid weight sum.");

            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < n; i++)
            {
                result[RarityOrder[i]] = (weights[i] / sum) * 100.0;
            }

            return result;
        }
    }
}

