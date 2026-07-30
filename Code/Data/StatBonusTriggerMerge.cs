using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Merges animal-suffix <see cref="StatBonus.TriggerName"/> / tags / taxon synergies onto a generated item.
    /// </summary>
    public static class StatBonusTriggerMerge
    {
        public static readonly string[] TaxonTags =
        {
            "shell", "reptile", "bird", "bug", "fish", "beast", "mythic"
        };

        /// <summary>Convention: taxon <c>shell</c> → <c>ShellSetFrom</c> / <c>ShellAmpTo</c>.</summary>
        public static string TaxonSetFromName(string taxon) =>
            Capitalize(taxon) + "SetFrom";

        public static string TaxonAmpToName(string taxon) =>
            Capitalize(taxon) + "AmpTo";

        /// <summary>
        /// Append resolved trigger identities and taxon tags from <paramref name="suffix"/> onto <paramref name="item"/>.
        /// Dedupes by identity name within the item's combat/equip lists.
        /// When <see cref="StatBonus.TriggerName"/> is blank (stale loot cache), resolves from
        /// <see cref="AnimalSuffixCatalog"/> by suffix name.
        /// </summary>
        public static void ApplySuffixToItem(Item item, StatBonus suffix)
        {
            if (item == null || suffix == null)
                return;

            EnsureSuffixTriggerFields(suffix);
            MergeTags(item, suffix.Tags);
            foreach (string name in EnumerateTriggerNames(suffix))
                AppendTriggerByName(item, name);
        }

        /// <summary>Re-apply triggers/tags from every suffix already on the item (lab / tooltip / after load).</summary>
        public static void RefreshFromItemSuffixes(Item item)
        {
            if (item?.StatBonuses == null)
                return;
            foreach (var suffix in item.StatBonuses)
            {
                if (suffix != null)
                    ApplySuffixToItem(item, suffix);
            }
        }

        /// <summary>
        /// Fills blank <see cref="StatBonus.TriggerName"/> / <see cref="StatBonus.Tags"/> from
        /// <see cref="AnimalSuffixCatalog"/> when the suffix name matches an animal row.
        /// </summary>
        public static void EnsureSuffixTriggerFields(StatBonus suffix)
        {
            if (suffix == null || string.IsNullOrWhiteSpace(suffix.Name))
                return;
            if (!string.IsNullOrWhiteSpace(suffix.TriggerName)
                && suffix.Tags != null
                && suffix.Tags.Count > 0)
                return;

            foreach (var def in AnimalSuffixCatalog.All)
            {
                if (!string.Equals(def.SuffixName, suffix.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(suffix.TriggerName))
                    suffix.TriggerName = def.TriggerName;
                if (suffix.Tags == null || suffix.Tags.Count == 0)
                    suffix.Tags = new List<string> { def.Taxon };
                if (string.IsNullOrWhiteSpace(suffix.Description))
                    suffix.Description = def.Description;
                return;
            }
        }

        public static IEnumerable<string> EnumerateTriggerNames(StatBonus suffix)
        {
            if (suffix == null)
                yield break;

            if (!string.IsNullOrWhiteSpace(suffix.TriggerName))
                yield return suffix.TriggerName.Trim();

            if (suffix.TriggerNames != null)
            {
                foreach (var n in suffix.TriggerNames)
                {
                    if (!string.IsNullOrWhiteSpace(n))
                        yield return n.Trim();
                }
            }

            if (suffix.Tags == null)
                yield break;

            foreach (var raw in suffix.Tags)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                string taxon = raw.Trim().ToLowerInvariant();
                if (!IsTaxonTag(taxon))
                    continue;
                yield return TaxonSetFromName(taxon);
                yield return TaxonAmpToName(taxon);
            }
        }

        public static bool IsTaxonTag(string? tag) =>
            !string.IsNullOrWhiteSpace(tag)
            && TaxonTags.Any(t => string.Equals(t, tag.Trim(), StringComparison.OrdinalIgnoreCase));

        private static void MergeTags(Item item, List<string>? tags)
        {
            if (tags == null || tags.Count == 0)
                return;
            var merged = GameDataTagHelper.NormalizeDistinct(item.Tags);
            foreach (var raw in tags)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                string tag = raw.Trim();
                if (!GameDataTagHelper.HasTag(merged, tag))
                    merged.Add(tag);
            }

            item.Tags = merged;
        }

        private static void AppendTriggerByName(Item item, string triggerName)
        {
            if (string.IsNullOrWhiteSpace(triggerName))
                return;
            if (!TriggersLoader.TryGetByName(triggerName, out var identity))
                return;

            var bundle = identity.ToBundle();
            if (identity.IsEquipEffect)
            {
                item.EquipEffects ??= new List<ActionTriggerBundle>();
                if (!ContainsIdentity(item.EquipEffects, identity.Name))
                    item.EquipEffects.Add(bundle);
            }
            else
            {
                item.TriggerBundles ??= new List<ActionTriggerBundle>();
                if (!ContainsIdentity(item.TriggerBundles, identity.Name))
                    item.TriggerBundles.Add(bundle);
            }
        }

        private static bool ContainsIdentity(List<ActionTriggerBundle> list, string identityName)
        {
            if (string.IsNullOrWhiteSpace(identityName))
                return false;
            return list.Any(b =>
                b != null
                && !string.IsNullOrWhiteSpace(b.IdentityName)
                && string.Equals(b.IdentityName, identityName, StringComparison.OrdinalIgnoreCase));
        }

        private static string Capitalize(string taxon)
        {
            if (string.IsNullOrWhiteSpace(taxon))
                return "";
            string t = taxon.Trim().ToLowerInvariant();
            return char.ToUpperInvariant(t[0]) + t.Substring(1);
        }
    }
}
