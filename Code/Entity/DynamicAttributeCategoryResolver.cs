using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Maps internal dynamic labels (PRIMARY / SECONDARY / NEGLECTED / WEAKNESS) to concrete
    /// STR / AGI / TECH / INT based on the character's current <em>effective</em> attribute values.
    /// Tie-break when values are equal: Strength &gt; Agility &gt; Technique &gt; Intelligence.
    /// Not intended for player-facing display.
    /// </summary>
    public static class DynamicAttributeCategoryResolver
    {
        /// <summary>Canonical stat codes used by temp-bonus and roll-bonus pipelines.</summary>
        public const string CodeStrength = "STR";
        public const string CodeAgility = "AGI";
        public const string CodeTechnique = "TECH";
        public const string CodeIntelligence = "INT";

        /// <summary>Returns the four stats ordered highest effective value first.</summary>
        public static IReadOnlyList<string> GetRankedAttributeCodes(Character character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            return GetRankedAttributeCodes(
                character.GetEffectiveStrength(),
                character.GetEffectiveAgility(),
                character.GetEffectiveTechnique(),
                character.GetEffectiveIntelligence());
        }

        /// <summary>
        /// Ranks the four attributes by value (descending). On ties, prefers STR, then AGI, then TECH, then INT.
        /// </summary>
        public static IReadOnlyList<string> GetRankedAttributeCodes(
            int strength,
            int agility,
            int technique,
            int intelligence)
        {
            // Tuple: (value, tieBreakOrder smaller = earlier in fixed order STR..INT)
            var rows = new[]
            {
                (strength, 0, CodeStrength),
                (agility, 1, CodeAgility),
                (technique, 2, CodeTechnique),
                (intelligence, 3, CodeIntelligence),
            };
            return rows
                .OrderByDescending(r => r.Item1)
                .ThenBy(r => r.Item2)
                .Select(r => r.Item3)
                .ToList();
        }

        /// <summary>
        /// If <paramref name="raw"/> is PRIMARY / SECONDARY / NEGLECTED / WEAKNESS (any common casing), returns the normalized uppercase token; otherwise null.
        /// </summary>
        public static string? TryNormalizeDynamicCategory(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            string u = raw.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return u switch
            {
                "PRIMARY" => "PRIMARY",
                "SECONDARY" => "SECONDARY",
                "NEGLECTED" => "NEGLECTED",
                "WEAKNESS" => "WEAKNESS",
                _ => null
            };
        }

        /// <summary>
        /// Resolves a dynamic category (PRIMARY..WEAKNESS) to STR / AGI / TECH / INT for <paramref name="character"/>.
        /// </summary>
        public static string ResolveCategoryToStatCode(Character character, string categoryUpper)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (string.IsNullOrEmpty(categoryUpper))
                throw new ArgumentException("Category must be non-empty.", nameof(categoryUpper));

            var ranked = GetRankedAttributeCodes(character);
            return categoryUpper switch
            {
                "PRIMARY" => ranked[0],
                "SECONDARY" => ranked[1],
                "NEGLECTED" => ranked[2],
                "WEAKNESS" => ranked[3],
                _ => throw new ArgumentOutOfRangeException(nameof(categoryUpper), categoryUpper, "Not a dynamic attribute category.")
            };
        }

        /// <summary>
        /// If <paramref name="rawStatType"/> names a dynamic category, returns the concrete code; otherwise null.
        /// </summary>
        public static string? TryResolveToConcreteStatCode(Character? character, string? rawStatType)
        {
            if (character == null || string.IsNullOrWhiteSpace(rawStatType))
                return null;
            string? cat = TryNormalizeDynamicCategory(rawStatType);
            if (cat == null)
                return null;
            return ResolveCategoryToStatCode(character, cat);
        }

        /// <summary>
        /// Resolves dynamic categories to STR/AGI/TECH/INT; otherwise normalizes spreadsheet aliases to the same four codes.
        /// Unknown types return the trimmed uppercase input (callers may still reject).
        /// </summary>
        public static string ResolveStatBonusTypeToConcreteCode(Character character, string statType)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (string.IsNullOrWhiteSpace(statType))
                return string.Empty;

            string? fromCategory = TryResolveToConcreteStatCode(character, statType);
            if (fromCategory != null)
                return fromCategory;

            string u = statType.Trim().ToUpperInvariant();
            return u switch
            {
                "STR" or "STRENGTH" => CodeStrength,
                "AGI" or "AGILITY" => CodeAgility,
                "TEC" or "TECH" or "TECHNIQUE" => CodeTechnique,
                "INT" or "INTELLIGENCE" => CodeIntelligence,
                _ => u
            };
        }

        /// <summary>True if <paramref name="type"/> is a core stat alias or a dynamic category.</summary>
        public static bool IsStatOrDynamicCategoryType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;
            if (TryNormalizeDynamicCategory(type) != null)
                return true;
            string u = type.Trim().ToUpperInvariant();
            return u is "STR" or "STRENGTH" or "AGI" or "AGILITY" or "TEC" or "TECH" or "TECHNIQUE" or "INT" or "INTELLIGENCE";
        }
    }
}
