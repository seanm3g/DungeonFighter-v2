using System;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Standing BLOCK % from the last named hero action. Covers incoming hits until the next named action.
    /// Unnamed hit/miss (synthetic normal) clears standing BLOCK so only class DEFENSE applies.
    /// Designer values are percent points (0–100); runtime stores 0–1.
    /// </summary>
    public static class StandingBlock
    {
        public const double DefaultBlockPercent = 0.25;

        /// <summary>Unnamed synthetic normal (ACTION shows hit/miss) — not a stance action.</summary>
        public static bool IsUnnamedSwing(Action? action) =>
            action == null || (string.IsNullOrEmpty(action.Name) && !action.IsComboAction);

        /// <summary>Clamp a 0–1 fraction.</summary>
        public static double ClampFraction(double fraction)
        {
            if (double.IsNaN(fraction) || double.IsInfinity(fraction))
                return DefaultBlockPercent;
            if (fraction < 0) return 0;
            if (fraction > 1) return 1;
            return fraction;
        }

        /// <summary>
        /// Parse designer percent points (e.g. "45" → 0.45). Empty/invalid → default 25%.
        /// Values in (0, 1] written as fractions are accepted as-is.
        /// </summary>
        public static double ParsePercentPoints(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return DefaultBlockPercent;

            string s = raw.Trim();
            if (s.EndsWith("%", StringComparison.Ordinal))
                s = s[..^1].Trim();

            if (!double.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double n))
                return DefaultBlockPercent;

            // Designer authoring is percent points (0–100). Fractions ≤1 with a decimal are also accepted.
            if (n < 0)
                return DefaultBlockPercent;
            if (n > 100)
                return 1.0;
            // Decimal fractions in (0,1] (e.g. "0.45") accepted as-is; otherwise percent points.
            if (n > 0 && n <= 1.0 && s.Contains('.'))
                return ClampFraction(n);
            return ClampFraction(n / 100.0);
        }

        /// <summary>Format 0–1 as whole percent points for sheet/JSON cells.</summary>
        public static string FormatPercentPoints(double fraction)
        {
            int pts = (int)Math.Round(ClampFraction(fraction) * 100.0, MidpointRounding.AwayFromZero);
            return pts.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static double ResolveFromAction(Action? action)
        {
            if (IsUnnamedSwing(action))
                return 0;
            return ClampFraction(action!.BlockPercent);
        }

        /// <summary>
        /// Hero-only: named actions set standing BLOCK (last named swing wins until the next named action).
        /// Unnamed hit/miss clears standing BLOCK (DEFENSE only).
        /// </summary>
        public static void ApplyFromAction(Actor? actor, Action? action)
        {
            if (actor is Character hero && hero is not Enemy)
                hero.StandingBlockPercent = ResolveFromAction(action);
        }

        /// <summary>Combat / room start: Open until the hero acts.</summary>
        public static void Reset(Actor? actor)
        {
            if (actor is Character hero && hero is not Enemy)
            {
                hero.StandingBlockPercent = 0;
                hero.EnergyShieldCurrent = 0;
                hero.EnergyShieldMax = 0;
            }
        }

        /// <summary>Migrate legacy energy cost 1–3 to block percent points.</summary>
        public static double FromLegacyEnergyCost(int cost)
        {
            return cost switch
            {
                1 => 0.45,
                3 => 0.0,
                _ => DefaultBlockPercent // 2 and invalid
            };
        }
    }
}
