using System;
using System.Globalization;

namespace RPGGame.Data
{
    /// <summary>
    /// Parses action modifier strings with correct semantics: SpeedMod and DamageMod are percent-based
    /// (e.g. "10" = 10%); MultiHitMod and AmpMod are raw numerical values.
    /// </summary>
    public static class ModifierParser
    {
        /// <summary>
        /// Parses a percent-based modifier (SpeedMod, DamageMod). "10" means 10% → 0.10 multiplier.
        /// Returns null if the value is empty or not a valid number.
        /// </summary>
        public static double? ParsePercent(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!double.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double num)) return null;
            return num / 100.0;
        }

        /// <summary>
        /// Parses a raw numerical modifier (MultiHitMod, AmpMod). "2" stays 2, "1.5" stays 1.5.
        /// Returns null if the value is empty or not a valid number.
        /// </summary>
        public static double? ParseValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!double.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double num)) return null;
            return num;
        }
    }
}
