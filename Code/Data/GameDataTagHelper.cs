using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.World.Tags;

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

        /// <summary>True when the tag list includes <c>item</c> pool gate.</summary>
        public static bool HasItemPoolTag(IEnumerable<string>? tags) => HasTag(tags, "item");

        /// <summary>True when the tag list includes <c>action</c> pool gate.</summary>
        public static bool HasActionPoolTag(IEnumerable<string>? tags) => HasTag(tags, "action");

        /// <summary>True when the tag list includes <c>enemy</c> (case-insensitive). Such actions are for enemy pools only, not the hero.</summary>
        public static bool HasEnemyTag(IEnumerable<string>? tags)
        {
            return HasTag(tags, "enemy");
        }

        /// <summary>
        /// True when an action may be granted on hero equipment (loot affixes, gear action pool, combo).
        /// Excludes <c>environment</c> (room hazards) and <c>enemy</c> (enemy-only) tags.
        /// </summary>
        public static bool IsGrantableOnHeroGear(IEnumerable<string>? tags)
        {
            return !HasEnvironmentTag(tags) && !HasEnemyTag(tags);
        }

        /// <summary>Resolves action tags from loaded game data and applies <see cref="IsGrantableOnHeroGear"/>.</summary>
        public static bool IsGrantableOnHeroGearByName(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return false;

            // Use GetActionData (loads once) — not LoadActions(), which re-reads Actions.json every call.
            var actionData = ActionLoader.GetActionData(actionName);
            if (actionData != null)
                return IsGrantableOnHeroGear(actionData.Tags);

            var action = ActionLoader.GetAction(actionName);
            if (action != null)
                return IsGrantableOnHeroGear(action.Tags);

            return true;
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

        public static bool IsKnownRegistryTag(string? tag) => TagDefinitions.IsKnownTag(tag);

        public static List<string> ValidateRegistryTags(TagEntityScope scope, IEnumerable<string>? tags) =>
            TagDefinitions.ValidateTagList(scope, tags);
    }
}
