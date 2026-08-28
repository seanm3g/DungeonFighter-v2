using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>
    /// Obsolete shim: animal-suffix triggers moved to <see cref="RPGGame.MaterialTriggerMerge"/>.
    /// Kept so merges from main that still call this API compile against material ownership.
    /// </summary>
    [Obsolete("Use MaterialTriggerMerge / MaterialTriggerCatalog. Animal suffix triggers are retired.")]
    public static class StatBonusTriggerMerge
    {
        public static readonly string[] TaxonTags = Array.Empty<string>();

        public static string TaxonSetFromName(string taxon) =>
            string.IsNullOrWhiteSpace(taxon) ? "" : char.ToUpperInvariant(taxon.Trim()[0]) + taxon.Trim().Substring(1) + "SetFrom";

        public static string TaxonAmpToName(string taxon) =>
            string.IsNullOrWhiteSpace(taxon) ? "" : char.ToUpperInvariant(taxon.Trim()[0]) + taxon.Trim().Substring(1) + "AmpTo";

        /// <summary>No-op: combat procs come from the item material, not suffixes.</summary>
        public static void ApplySuffixToItem(Item item, StatBonus suffix)
        {
            // Intentionally empty — material pools own gear procs.
        }

        /// <summary>No-op.</summary>
        public static void RefreshFromItemSuffixes(Item item)
        {
        }

        public static void EnsureSuffixTriggerFields(StatBonus suffix)
        {
        }

        public static IEnumerable<string> EnumerateTriggerNames(StatBonus suffix) =>
            Array.Empty<string>();

        public static bool IsTaxonTag(string? tag) => false;
    }
}
