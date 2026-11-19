using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Base class for item generation that provides common patterns and utilities
    /// Eliminates duplication between GameDataGenerator.cs and LootGenerator.cs
    /// </summary>
    public static class ItemGenerator
    {
        /// <summary>Stat suffix keywords appended after the base name: Uncommon, then Common, then Rare, then higher tiers.</summary>
        private static readonly string[] StatSuffixDisplayRarityOrder =
            { "Uncommon", "Common", "Rare", "Epic", "Legendary", "Mythic" };

        /// <summary>Lower sorts earlier in the displayed item name among stat suffixes.</summary>
        internal static int StatSuffixDisplayOrderRank(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 999;
            string t = rarity.Trim();
            for (int i = 0; i < StatSuffixDisplayRarityOrder.Length; i++)
            {
                if (string.Equals(StatSuffixDisplayRarityOrder[i], t, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 100;
        }

        /// <summary>
        /// One-time roll for catalog <see cref="Item.ExtraActionSlots"/> on armor or weapons.
        /// When both min and max are zero after clamp/swap, returns <paramref name="legacyFixed"/> (JSON <c>extraActionSlots</c>).
        /// Otherwise rolls inclusive between min and max (same pattern as damage bonus min/max).
        /// </summary>
        internal static int RollCatalogExtraActionSlots(int legacyFixed, int minBound, int maxBound)
        {
            int minS = Math.Max(0, minBound);
            int maxS = Math.Max(0, maxBound);
            if (maxS < minS)
                (minS, maxS) = (maxS, minS);
            if (maxS > 0 || minS > 0)
                return RandomUtility.Next(minS, maxS + 1);
            return Math.Max(0, legacyFixed);
        }

        /// <summary>
        /// Generates a weapon item from weapon data
        /// </summary>
        /// <param name="weaponData">The weapon data to use</param>
        /// <returns>A new WeaponItem instance</returns>
        public static WeaponItem GenerateWeaponItem(WeaponData weaponData)
        {
            var weaponType = Enum.Parse<WeaponType>(weaponData.Type);
            int minB = weaponData.DamageBonusMin;
            int maxB = weaponData.DamageBonusMax;
            if (maxB < minB)
                (minB, maxB) = (maxB, minB);
            minB = Math.Max(0, minB);
            maxB = Math.Max(0, maxB);
            int rolledBonus = RandomUtility.Next(minB, maxB + 1);

            var weapon = new WeaponItem(weaponData.Name, weaponData.Tier,
                weaponData.BaseDamage, weaponData.AttackSpeed, weaponType)
            {
                RolledDamageBonus = rolledBonus,
                ExtraActionSlots = RollCatalogExtraActionSlots(
                    weaponData.ExtraActionSlots,
                    weaponData.ExtraActionSlotsMin,
                    weaponData.ExtraActionSlotsMax)
            };
            
            // Copy attribute requirements if present (catalog snapshot used when recomputing after prefix rolls)
            if (weaponData.AttributeRequirements != null && weaponData.AttributeRequirements.Count > 0)
            {
                weapon.AttributeRequirements = new AttributeRequirements(weaponData.AttributeRequirements);
                weapon.CatalogAttributeRequirements = new AttributeRequirements(weaponData.AttributeRequirements);
            }

            weapon.Tags = GameDataTagHelper.NormalizeDistinct(weaponData.Tags);
            
            return weapon;
        }

        /// <summary>
        /// Generates an armor item from armor data
        /// </summary>
        /// <param name="armorData">The armor data to use</param>
        /// <returns>A new armor Item instance</returns>
        public static Item GenerateArmorItem(ArmorData armorData)
        {
            Item item = armorData.Slot.ToLower() switch
            {
                "head" => new HeadItem(armorData.Name, armorData.Tier, armorData.Armor),
                "chest" => new ChestItem(armorData.Name, armorData.Tier, armorData.Armor),
                "feet" => new FeetItem(armorData.Name, armorData.Tier, armorData.Armor),
                "legs" => new LegsItem(armorData.Name, armorData.Tier, armorData.Armor),
                _ => throw new ArgumentException($"Unknown armor slot: {armorData.Slot}")
            };
            
            // Copy attribute requirements if present (catalog snapshot used when recomputing after prefix rolls)
            if (armorData.AttributeRequirements != null && armorData.AttributeRequirements.Count > 0)
            {
                item.AttributeRequirements = new AttributeRequirements(armorData.AttributeRequirements);
                item.CatalogAttributeRequirements = new AttributeRequirements(armorData.AttributeRequirements);
            }

            item.Tags = GameDataTagHelper.NormalizeDistinct(armorData.Tags);

            item.BaseStrength = armorData.Strength;
            item.BaseAgility = armorData.Agility;
            item.BaseTechnique = armorData.Technique;
            item.BaseIntelligence = armorData.Intelligence;
            item.BaseHit = armorData.Hit;
            item.BaseCombo = armorData.Combo;
            item.BaseCrit = armorData.Crit;
            item.ExtraActionSlots = RollCatalogExtraActionSlots(
                armorData.ExtraActionSlots,
                armorData.ExtraActionSlotsMin,
                armorData.ExtraActionSlotsMax);
            item.MinGeneratedActionBonuses = armorData.MinActionBonuses;
            item.CatalogAttackSpeed = armorData.AttackSpeed;

            return item;
        }

        /// <summary>
        /// Selects a random item from a list of items of the specified tier
        /// </summary>
        /// <typeparam name="T">The type of item data</typeparam>
        /// <param name="items">The list of items to select from</param>
        /// <param name="tier">The tier to filter by</param>
        /// <returns>A random item of the specified tier, or null if none found</returns>
        public static T? SelectRandomItemByTier<T>(IEnumerable<T> items, int tier) where T : class
        {
            var itemsInTier = items.Where(t => GetTierValue(t) == tier).ToList();
            if (!itemsInTier.Any()) return null;
            
            return itemsInTier[RandomUtility.Next(itemsInTier.Count)];
        }

        /// <summary>
        /// Gets the tier value from an item (works with both WeaponData and ArmorData)
        /// </summary>
        /// <typeparam name="T">The type of item data</typeparam>
        /// <param name="item">The item to get the tier from</param>
        /// <returns>The tier value</returns>
        private static int GetTierValue<T>(T item)
        {
            if (item is WeaponData weapon)
                return weapon.Tier;
            if (item is ArmorData armor)
                return armor.Tier; // Fixed: was using armor.Armor instead of armor.Tier
            return 0;
        }

        /// <summary>
        /// Generates an item name with bonuses and modifications
        /// Note: Rarity is NOT included in the name - it should be displayed separately in brackets
        /// </summary>
        /// <param name="item">The item to generate a name for</param>
        /// <returns>The generated item name (without rarity prefix)</returns>
        public static string GenerateItemNameWithBonuses(Item item)
        {
            if (!string.IsNullOrWhiteSpace(item.BaseName))
                return ComposeRolledItemName(item);

            string baseName = GetBaseItemName(item);
            var nameParts = new List<string>();
            
            // Do NOT add rarity prefix - rarity should only come from item.Rarity property
            // and be displayed separately in brackets, not as part of the name
            
            // Prefix slots: Quality, then Adjective, then Material (see ItemPrefixHelper)
            foreach (var modification in ItemPrefixHelper.OrderedPrefixModifications(item.Modifications))
            {
                if (!string.IsNullOrEmpty(modification.Name))
                    nameParts.Add(modification.Name.Trim());
            }

            // Add base name
            nameParts.Add(baseName);

            // Modification names used as suffixes ("of …")
            foreach (var modification in item.Modifications)
            {
                if (!string.IsNullOrEmpty(modification.Name) &&
                    modification.Name.TrimStart().StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                {
                    nameParts.Add(modification.Name.Trim());
                }
            }
            
            // Add stat bonus names as suffixes (these are typically "of Protection", "of Swiftness", etc.)
            foreach (var statBonus in item.StatBonuses
                         .OrderBy(s => StatSuffixDisplayOrderRank(s.Rarity))
                         .ThenBy(s => s.Name ?? "", StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(statBonus.Name))
                {
                    // Trim the stat bonus name to ensure no leading/trailing spaces
                    nameParts.Add(statBonus.Name.Trim());
                }
            }
            
            // Join with spaces and normalize multiple spaces to single space
            string result = string.Join(" ", nameParts);
            // Normalize any multiple spaces that might have been introduced
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
            return result.Trim();
        }

        /// <summary>Common-only tiers carry this word in their keyword ("Uncanny Sharp", "of the Uncanny Ox"). Not
        /// "Lucky": that is the Leather set's roll advantage.</summary>
        internal const string ExceptionalWord = "Uncanny";

        /// <summary>
        /// Name of an item built by the item roller: <c>[Uncanny] [best prefix keyword] [Material] Base [best suffix keyword]</c>
        /// ("Uncanny Wicked Iron Cuirass of the Ox"). "Best" = highest tier among that affix's lines (first rolled wins
        /// ties); every line still shows on the tooltip. A common-only ("Uncanny") line moves its "Uncanny" to the front
        /// of the name, once.
        /// </summary>
        internal static string ComposeRolledItemName(Item item)
        {
            var lines = (item.Modifications ?? new List<Modification>()).Select(m => m.ModLine)
                .Concat((item.StatBonuses ?? new List<StatBonus>()).Select(s => s.ModLine))
                .Where(l => l != null && !string.IsNullOrWhiteSpace(l.Keyword))
                .Select(l => l!)
                .ToList();
            ItemModLineInfo? Best(string affix) => lines
                .Where(l => string.Equals(l.Affix, affix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.Tier)
                .FirstOrDefault();

            bool exceptional = false;
            string Keyword(ItemModLineInfo line)
            {
                string k = System.Text.RegularExpressions.Regex.Replace(line.Keyword.Trim(), @"\b" + ExceptionalWord + @"\s+", "");
                exceptional |= k.Length != line.Keyword.Trim().Length;
                return k;
            }

            var parts = new List<string>();
            if (Best("prefix") is { } prefix)
                parts.Add(Keyword(prefix));
            if (!string.IsNullOrWhiteSpace(item.Material))
                parts.Add(item.Material.Trim());
            parts.Add(item.BaseName.Trim());

            // Suffixes: one per kind, in kind order (descriptor, animal, poetic phrase). A hybrid's parts share one
            // keyword, so they print once. Old lines without a kind keep the single best suffix.
            var suffixes = lines.Where(l => string.Equals(l.Affix, "suffix", StringComparison.OrdinalIgnoreCase)).ToList();
            if (suffixes.Any(l => !string.IsNullOrWhiteSpace(l.SuffixKind)))
            {
                foreach (var group in suffixes.Where(l => !string.IsNullOrWhiteSpace(l.SuffixKind))
                             .GroupBy(l => l.SuffixKind!, StringComparer.OrdinalIgnoreCase)
                             .OrderBy(g => RPGGame.Data.ItemGeneration.SuffixKinds.Rank(g.Key)))
                    parts.Add(Keyword(group.OrderByDescending(l => l.Tier).First()));
            }
            else if (Best("suffix") is { } suffix)
            {
                parts.Add(Keyword(suffix));
            }
            if (exceptional)
                parts.Insert(0, ExceptionalWord);
            return System.Text.RegularExpressions.Regex.Replace(string.Join(" ", parts), @"\s+", " ").Trim();
        }

        /// <summary>
        /// Gets the base item name without modifications
        /// </summary>
        /// <param name="item">The item to get the base name from</param>
        /// <returns>The base item name</returns>
        private static string GetBaseItemName(Item item)
        {
            // Remove any existing prefixes/suffixes to get the base name
            string name = item.Name ?? "";

            // Remove legacy rarity-as-prefix in saved names (rarity is a separate property now)
            string[] rarityPrefixes = { "Legendary", "Epic", "Rare", "Uncommon", "Common" };
            foreach (string prefix in rarityPrefixes)
            {
                if (name.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length + 1);
                    break;
                }
            }

            // Strip leading prefix-slot words (Quality / Adjective / Material) so re-building the name
            // does not concatenate them twice when item.Name already includes rolled prefixes.
            if (item.Modifications != null && item.Modifications.Count > 0)
            {
                bool changed = true;
                while (changed && !string.IsNullOrWhiteSpace(name))
                {
                    changed = false;
                    foreach (var m in ItemPrefixHelper.OrderedPrefixModifications(item.Modifications))
                    {
                        if (m == null || string.IsNullOrWhiteSpace(m.Name))
                            continue;
                        if (m.Name.TrimStart().StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string token = m.Name.Trim();
                        if (name.StartsWith(token + " ", StringComparison.OrdinalIgnoreCase))
                        {
                            name = name.Substring(token.Length).TrimStart();
                            changed = true;
                            break;
                        }

                        if (name.Equals(token, StringComparison.OrdinalIgnoreCase))
                        {
                            name = "";
                            changed = true;
                            break;
                        }
                    }
                }
            }

            // Strip known suffix tokens from the end (stat bonuses and "of …" modification names).
            // Multiple passes are required when two suffixes share the same length: a single pass ordered
            // by length may match only the outer token first, leaving the inner suffix on the "base" name
            // so a subsequent GenerateItemNameWithBonuses would append all suffixes again (duplicate).
            if (item.StatBonuses != null && item.StatBonuses.Count > 0)
            {
                bool removedStatSuffix;
                do
                {
                    removedStatSuffix = false;
                    foreach (var sb in item.StatBonuses.OrderByDescending(s => (s.Name ?? "").Length))
                    {
                        if (string.IsNullOrEmpty(sb.Name)) continue;
                        string suffix = sb.Name.Trim();
                        if (name.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
                        {
                            name = name[..^(suffix.Length + 1)].TrimEnd();
                            removedStatSuffix = true;
                            break;
                        }

                        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && name.Length == suffix.Length)
                        {
                            name = "";
                            removedStatSuffix = true;
                            break;
                        }
                    }
                } while (removedStatSuffix && !string.IsNullOrWhiteSpace(name));
            }

            if (item.Modifications != null)
            {
                var modSuffixes = item.Modifications
                    .Where(m => m != null && !string.IsNullOrEmpty(m.Name) &&
                                m.Name.TrimStart().StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.Name!.Length)
                    .ToList();
                if (modSuffixes.Count > 0)
                {
                    bool removedModSuffix;
                    do
                    {
                        removedModSuffix = false;
                        foreach (var m in modSuffixes)
                        {
                            string suffix = m.Name!.Trim();
                            if (name.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
                            {
                                name = name[..^(suffix.Length + 1)].TrimEnd();
                                removedModSuffix = true;
                                break;
                            }
                        }
                    } while (removedModSuffix && !string.IsNullOrWhiteSpace(name));
                }
            }

            return name.Trim();
        }

    }
}
