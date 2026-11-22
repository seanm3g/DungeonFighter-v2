using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Region travel requirements (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D1.4): each region lists
    /// "level n" and / or "STR n" / "AGI n" / "TEC n" / "INT n" (effective attributes). All must be met.
    /// </summary>
    public static class RegionRequirements
    {
        /// <summary>True when the hero meets every requirement; <paramref name="missing"/> names the unmet ones ("level 11, STR 20").</summary>
        public static bool Meets(TravelRegion? region, Character? hero, out string missing)
        {
            missing = "";
            if (region == null || hero == null || region.Requirements == null || region.Requirements.Count == 0)
                return true;
            var unmet = new List<string>();
            foreach (string raw in region.Requirements)
            {
                if (!TryParse(raw, out string key, out int need))
                    continue;
                if (Value(hero, key) < need)
                    unmet.Add(key == "LEVEL" ? $"level {need}" : $"{key} {need}");
            }
            missing = string.Join(", ", unmet);
            return unmet.Count == 0;
        }

        public static bool TryParse(string? raw, out string key, out int value)
        {
            key = "";
            value = 0;
            var parts = (raw ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return false;
            key = parts[0].ToUpperInvariant() switch
            {
                "LEVEL" or "LVL" => "LEVEL",
                "STR" or "STRENGTH" => "STR",
                "AGI" or "AGILITY" => "AGI",
                "TEC" or "TECH" or "TECHNIQUE" => "TEC",
                "INT" or "INTELLIGENCE" => "INT",
                _ => ""
            };
            return key.Length > 0;
        }

        private static int Value(Character hero, string key) => key switch
        {
            "LEVEL" => hero.Level,
            "STR" => hero.GetEffectiveStrength(),
            "AGI" => hero.GetEffectiveAgility(),
            "TEC" => hero.GetEffectiveTechnique(),
            "INT" => hero.GetEffectiveIntelligence(),
            _ => int.MaxValue
        };
    }
}
