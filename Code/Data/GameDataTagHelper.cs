using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>Normalize tag lists from JSON, sheets, or catalog rows for consistent comparisons.</summary>
    public static class GameDataTagHelper
    {
        public static List<string> NormalizeDistinct(IEnumerable<string>? source)
        {
            if (source == null)
                return new List<string>();

            return source
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>True when the tag list includes <paramref name="tag"/> (case-insensitive, trimmed).</summary>
        public static bool HasTag(IEnumerable<string>? tags, string? tag)
        {
            if (tags == null || string.IsNullOrWhiteSpace(tag))
                return false;
            var needle = tag.Trim();
            return tags.Any(t => !string.IsNullOrWhiteSpace(t) && string.Equals(t.Trim(), needle, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>True when the tag list includes <c>environment</c> (case-insensitive).</summary>
        public static bool HasEnvironmentTag(IEnumerable<string>? tags)
        {
            return HasTag(tags, "environment");
        }

        /// <summary>Split comma/semicolon/pipe cell or settings text into normalized tags (empty if none).</summary>
        public static List<string> ParseCommaSeparatedTags(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return new List<string>();
            var parts = s.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return NormalizeDistinct(parts);
        }
    }
}
