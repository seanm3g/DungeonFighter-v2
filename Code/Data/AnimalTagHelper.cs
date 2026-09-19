using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RPGGame.World.Tags;

namespace RPGGame.Data
{
    /// <summary>
    /// Animal suffix tag helpers: taxon detection, specific-name normalization, and Item.Tags stamping.
    /// </summary>
    public static class AnimalTagHelper
    {
        public const string AnimalTag = "animal";

        public static readonly string[] TaxonTags =
        {
            "shell", "reptile", "bird", "bug", "fish", "beast", "mythic"
        };

        private static readonly HashSet<string> TaxonSet = new(TaxonTags, StringComparer.OrdinalIgnoreCase);

        private static readonly Regex OfTheName = new(
            @"^of\s+the\s+(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool IsTaxon(string? tag) =>
            !string.IsNullOrWhiteSpace(tag) && TaxonSet.Contains(tag.Trim());

        /// <summary>
        /// <c>of the Cape Buffalo</c> → <c>cape_buffalo</c>; returns empty when not an animal-style name.
        /// </summary>
        public static string NormalizeSpecificAnimalTag(string? suffixName)
        {
            if (string.IsNullOrWhiteSpace(suffixName))
                return "";
            var m = OfTheName.Match(suffixName.Trim());
            if (!m.Success)
                return "";
            string raw = m.Groups[1].Value.Trim().ToLowerInvariant();
            if (raw.Length == 0)
                return "";
            var sb = new StringBuilder(raw.Length);
            bool prevUnderscore = false;
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    prevUnderscore = false;
                }
                else if (!prevUnderscore)
                {
                    sb.Append('_');
                    prevUnderscore = true;
                }
            }
            string result = sb.ToString().Trim('_');
            return result;
        }

        public static bool IsAnimalSuffix(StatBonus? bonus)
        {
            if (bonus == null)
                return false;
            if (bonus.Tags != null)
            {
                foreach (var t in bonus.Tags)
                {
                    if (IsTaxon(t))
                        return true;
                }
            }
            return NormalizeSpecificAnimalTag(bonus.Name).Length > 0
                && (bonus.Mechanics == null || bonus.Mechanics.Count == 0)
                && string.IsNullOrWhiteSpace(bonus.StatType);
        }

        /// <summary>
        /// Unions animal / taxon / specific tags from rolled <see cref="Item.StatBonuses"/> onto <see cref="Item.Tags"/>.
        /// </summary>
        public static void SyncSuffixTags(Item item)
        {
            if (item?.StatBonuses == null || item.StatBonuses.Count == 0)
                return;

            var tags = GameDataTagHelper.NormalizeDistinct(item.Tags);
            bool anyAnimal = false;

            foreach (var bonus in item.StatBonuses)
            {
                if (bonus == null || !IsAnimalSuffix(bonus))
                    continue;

                anyAnimal = true;
                if (bonus.Tags != null)
                {
                    foreach (var raw in bonus.Tags)
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                            continue;
                        string tag = raw.Trim();
                        if (!GameDataTagHelper.HasTag(tags, tag))
                            tags.Add(tag);
                        if (IsTaxon(tag))
                            TagDefinitions.EnsureDynamicItemMatchTag(tag);
                    }
                }

                string specific = NormalizeSpecificAnimalTag(bonus.Name);
                if (specific.Length > 0)
                {
                    if (!GameDataTagHelper.HasTag(tags, specific))
                        tags.Add(specific);
                    TagDefinitions.EnsureDynamicItemMatchTag(specific);
                }
            }

            if (anyAnimal && !GameDataTagHelper.HasTag(tags, AnimalTag))
                tags.Add(AnimalTag);

            item.Tags = tags;
        }
    }
}
