using System;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Standing Defense stance from the last resolved swing (hero or enemy).
    /// Authored BLOCK percents map to three stances: aggressive 0%, neutral 100%, defensive 180%.
    /// A named hit sets that action's stance. A miss, critical miss, or unnamed normal hit sets neutral.
    /// Combat/room start is neutral. A successful incoming hit returns standing to neutral.
    /// </summary>
    public static class StandingBlock
    {
        public const double DefaultBlockPercent = 1.0;
        public const double AggressiveMultiplier = 0.0;
        public const double DefensiveMultiplier = 1.80;
        public const double MaxMultiplier = 5.0;
        public const int MaxPercentPoints = 500;

        /// <summary>Unnamed synthetic normal (ACTION shows hit/miss) — not a stance action.</summary>
        public static bool IsUnnamedSwing(Action? action) =>
            action == null || (string.IsNullOrEmpty(action.Name) && !action.IsComboAction);

        /// <summary>Clamp a 0–5 Defense multiplier.</summary>
        public static double ClampFraction(double fraction) => ClampMultiplier(fraction);

        /// <summary>Clamp a 0–5 Defense multiplier. Invalid → default 1.0.</summary>
        public static double ClampMultiplier(double multiplier)
        {
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                return DefaultBlockPercent;
            if (multiplier < 0) return 0;
            if (multiplier > MaxMultiplier) return MaxMultiplier;
            return multiplier;
        }

        /// <summary>
        /// Parse designer percent points (e.g. "180" → 1.80). Empty/invalid → default 100%.
        /// Values above 500 clamp to 5.0. Negative/non-numeric → default.
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

            if (n < 0)
                return DefaultBlockPercent;
            if (n > MaxPercentPoints)
                return MaxMultiplier;
            return ClampMultiplier(n / 100.0);
        }

        /// <summary>Format 0–5 as whole percent points for sheet/JSON cells.</summary>
        public static string FormatPercentPoints(double fraction)
        {
            int pts = (int)Math.Round(ClampMultiplier(fraction) * 100.0, MidpointRounding.AwayFromZero);
            return pts.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static double ResolveFromAction(Action? action)
        {
            if (IsUnnamedSwing(action))
                return DefaultBlockPercent;
            return ClampMultiplier(action!.BlockPercent);
        }

        /// <summary>0 → aggressive, 180+ → defensive, anything between → neutral.</summary>
        public static string GetStanceName(double multiplier)
        {
            int pts = (int)Math.Round(ClampMultiplier(multiplier) * 100.0, MidpointRounding.AwayFromZero);
            if (pts <= 0)
                return "aggressive";
            if (pts >= 180)
                return "defensive";
            return "neutral";
        }

        /// <summary>Title-case stance for the action editor (Aggressive / Neutral / Defensive).</summary>
        public static string FormatStanceLabel(double multiplier)
        {
            string name = GetStanceName(multiplier);
            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        /// <summary>
        /// Card line for a lingering stance, e.g. <c>result: defensive stance</c>.
        /// Neutral is omitted so the card does not show a stance line.
        /// </summary>
        public static string FormatCardLine(double multiplier)
        {
            string name = GetStanceName(multiplier);
            if (name == "neutral")
                return "";
            return $"result: {name} stance";
        }

        /// <summary>Editor label → stored multiplier. Unknown labels are neutral (100%).</summary>
        public static double MultiplierFromStanceLabel(string? label)
        {
            if (string.Equals(label?.Trim(), "aggressive", StringComparison.OrdinalIgnoreCase))
                return AggressiveMultiplier;
            if (string.Equals(label?.Trim(), "defensive", StringComparison.OrdinalIgnoreCase))
                return DefensiveMultiplier;
            return DefaultBlockPercent;
        }

        /// <summary>
        /// Named actions set standing Defense % (last named swing wins until the next named action or a hit).
        /// Unnamed hit/miss sets standing to 100% (base Defense).
        /// </summary>
        public static void ApplyFromAction(Actor? actor, Action? action)
        {
            if (actor is Character defender)
                defender.StandingBlockPercent = ResolveFromAction(action);
        }

        /// <summary>
        /// Named hit sets that action's stance. A miss or an unnamed normal hit sets neutral.
        /// </summary>
        public static void ApplyFromResolvedSwing(Actor? actor, Action? action, bool hit)
        {
            if (actor is not Character defender)
                return;
            defender.StandingBlockPercent = hit && !IsUnnamedSwing(action)
                ? ResolveFromAction(action)
                : DefaultBlockPercent;
        }

        /// <summary>Combat / room start: Open at 100% (base Defense).</summary>
        public static void Reset(Actor? actor)
        {
            if (actor is not Character defender)
                return;

            defender.StandingBlockPercent = DefaultBlockPercent;
            if (defender is Enemy)
                return;

            defender.EnergyShieldCurrent = 0;
            defender.EnergyShieldMax = 0;
            defender.Effects.PendingDefenseTempoSpeedPct = 0;
            defender.Effects.PendingDefenseCounterDamagePct = 0;
        }

        /// <summary>
        /// After a successful incoming hit, drop the combo BLOCK bonus so later hits use base Defense (100%).
        /// </summary>
        public static void ConsumeAfterHit(Actor? actor)
        {
            if (actor is Character defender)
                defender.StandingBlockPercent = DefaultBlockPercent;
        }

        /// <summary>Migrate legacy energy cost 1–3 to Defense % multipliers (180 / 100 / 0).</summary>
        public static double FromLegacyEnergyCost(int cost)
        {
            return cost switch
            {
                1 => 1.80,
                3 => 0.0,
                _ => DefaultBlockPercent // 2 and invalid
            };
        }
    }
}
