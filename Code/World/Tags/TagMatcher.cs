using System.Collections.Generic;
using System.Linq;

namespace RPGGame.World.Tags
{
    /// <summary>
    /// Efficient tag matching algorithms
    /// </summary>
    public static class TagMatcher
    {
        /// <summary>
        /// Checks if source tags contain all required tags
        /// </summary>
        public static bool HasAllTags(IEnumerable<string> sourceTags, IEnumerable<string> requiredTags)
        {
            var sourceSet = new HashSet<string>(sourceTags, System.StringComparer.OrdinalIgnoreCase);
            return requiredTags.All(tag => sourceSet.Contains(tag));
        }

        /// <summary>
        /// Checks if source tags contain any of the required tags
        /// </summary>
        public static bool HasAnyTag(IEnumerable<string> sourceTags, IEnumerable<string> requiredTags)
        {
            var sourceSet = new HashSet<string>(sourceTags, System.StringComparer.OrdinalIgnoreCase);
            return requiredTags.Any(tag => sourceSet.Contains(tag));
        }

        /// <summary>
        /// Counts how many matching tags exist between two collections
        /// </summary>
        public static int CountMatchingTags(IEnumerable<string> sourceTags, IEnumerable<string> targetTags)
        {
            var sourceSet = new HashSet<string>(sourceTags, System.StringComparer.OrdinalIgnoreCase);
            return targetTags.Count(tag => sourceSet.Contains(tag));
        }

        /// <summary>
        /// Checks if source has at least X matching tags
        /// </summary>
        public static bool HasAtLeastTags(IEnumerable<string> sourceTags, IEnumerable<string> targetTags, int minCount)
        {
            return CountMatchingTags(sourceTags, targetTags) >= minCount;
        }
    }
}

