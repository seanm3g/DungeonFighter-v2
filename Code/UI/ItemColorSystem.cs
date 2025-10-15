using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.UI
{
    /// <summary>
    /// Handles color formatting for items based on rarity and modifications
    /// </summary>
    public static class ItemColorSystem
    {
        // Color templates for modification effects
        private static readonly Dictionary<string, string> ModificationColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Damage-related
            { "damage", "fiery" },
            { "damageMultiplier", "fiery" },
            
            // Speed-related
            { "speedMultiplier", "electric" },
            { "attackSpeed", "electric" },
            
            // Magical effects
            { "rollBonus", "arcane" },
            { "magicFind", "ethereal" },
            { "uniqueActionChance", "arcane" },
            
            // Life/Death
            { "lifesteal", "bloodied" },
            { "bleedChance", "bleeding" },
            
            // Special
            { "godlike", "holy" },
            { "autoSuccess", "golden" },
            { "reroll", "crystalline" },
            { "durability", "natural" }
        };

        // Color templates for stat bonuses
        private static readonly Dictionary<string, string> StatBonusColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "STR", "fiery" },
            { "AGI", "electric" },
            { "TEC", "crystalline" },
            { "INT", "arcane" },
            { "Health", "heal" },
            { "Armor", "natural" },
            { "AttackSpeed", "electric" }
        };

        /// <summary>
        /// Gets the color template name for a given rarity
        /// </summary>
        public static string GetRarityTemplate(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => "common",
                "uncommon" => "uncommon",
                "rare" => "rare",
                "epic" => "epic",
                "legendary" => "legendary",
                "mythic" => "mythic",
                "transcendent" => "transcendent",
                _ => "common"
            };
        }

        /// <summary>
        /// Formats an item name with rarity color
        /// </summary>
        public static string FormatItemName(Item item)
        {
            string rarityTemplate = GetRarityTemplate(item.Rarity);
            return $"{{{{{rarityTemplate}|{item.Name}}}}}";
        }

        /// <summary>
        /// Formats an item name with rarity and modification prefixes/suffixes
        /// </summary>
        public static string FormatFullItemName(Item item)
        {
            var parts = new List<string>();
            
            // Get modifications (prefixes and suffixes)
            var prefixes = item.Modifications.Where(m => m.ItemRank == "prefix" || string.IsNullOrEmpty(m.ItemRank)).ToList();
            var suffixes = item.Modifications.Where(m => m.ItemRank == "suffix").ToList();

            // Add colored prefixes
            foreach (var prefix in prefixes)
            {
                string prefixName = ExtractModificationName(prefix.Name);
                string colorTemplate = GetModificationColorTemplate(prefix.Effect);
                parts.Add($"{{{{{colorTemplate}|{prefixName}}}}}");
            }

            // Add the main item name with rarity color
            string rarityTemplate = GetRarityTemplate(item.Rarity);
            string baseName = GetBaseItemName(item);
            parts.Add($"{{{{{rarityTemplate}|{baseName}}}}}");

            // Add colored suffixes
            foreach (var suffix in suffixes)
            {
                string suffixName = ExtractModificationName(suffix.Name);
                string colorTemplate = GetModificationColorTemplate(suffix.Effect);
                parts.Add($"{{{{{colorTemplate}|{suffixName}}}}}");
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Formats a modification with appropriate color
        /// </summary>
        public static string FormatModification(Modification modification)
        {
            string colorTemplate = GetModificationColorTemplate(modification.Effect);
            string modName = ExtractModificationName(modification.Name);
            return $"{{{{{colorTemplate}|{modName}}}}}";
        }

        /// <summary>
        /// Formats a stat bonus with appropriate color
        /// </summary>
        public static string FormatStatBonus(StatBonus bonus)
        {
            string colorTemplate = GetStatBonusColorTemplate(bonus.StatType);
            string bonusName = CleanStatBonusName(bonus.Name);
            return $"{{{{{colorTemplate}|{bonusName}}}}}";
        }

        /// <summary>
        /// Gets the color template for a modification effect
        /// </summary>
        private static string GetModificationColorTemplate(string effect)
        {
            if (ModificationColorMap.TryGetValue(effect, out string? template))
            {
                return template;
            }
            return "arcane"; // Default for unknown effects
        }

        /// <summary>
        /// Gets the color template for a stat bonus
        /// </summary>
        private static string GetStatBonusColorTemplate(string statType)
        {
            if (StatBonusColorMap.TryGetValue(statType, out string? template))
            {
                return template;
            }
            return "natural"; // Default for unknown stats
        }

        /// <summary>
        /// Extracts the modification name, removing common prefixes
        /// </summary>
        private static string ExtractModificationName(string name)
        {
            // Remove "of " prefix if present
            if (name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(3);
            }
            return name;
        }

        /// <summary>
        /// Gets the base item name without modifications
        /// </summary>
        private static string GetBaseItemName(Item item)
        {
            string name = item.Name;
            
            // Remove modification names from the full name
            foreach (var mod in item.Modifications)
            {
                string modName = ExtractModificationName(mod.Name);
                name = name.Replace(modName, "").Trim();
            }

            // Clean up multiple spaces
            while (name.Contains("  "))
            {
                name = name.Replace("  ", " ");
            }

            return name.Trim();
        }

        /// <summary>
        /// Cleans a stat bonus name by removing the "of " prefix if present
        /// </summary>
        private static string CleanStatBonusName(string name)
        {
            if (name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(3);
            }
            return name;
        }

        /// <summary>
        /// Creates a simple colored item display (just name + rarity)
        /// </summary>
        public static string FormatSimpleItemDisplay(Item item)
        {
            string rarityTemplate = GetRarityTemplate(item.Rarity);
            return $"{{{{{rarityTemplate}|{item.Name}}}}}";
        }

        /// <summary>
        /// Creates a detailed item display with all bonuses colored
        /// </summary>
        public static List<string> FormatDetailedItemDisplay(Item item)
        {
            var lines = new List<string>();
            
            // Main item name
            lines.Add(FormatSimpleItemDisplay(item));
            
            // Stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                var bonusTexts = item.StatBonuses.Select(b => FormatStatBonus(b));
                lines.Add($"    &CStats:&y {string.Join(", ", bonusTexts)}");
            }
            
            // Action bonuses
            if (item.ActionBonuses.Count > 0)
            {
                var actionTexts = item.ActionBonuses.Select(a => $"&G{a.Name}&y");
                lines.Add($"    &CActions:&y {string.Join(", ", actionTexts)}");
            }
            
            // Modifications
            if (item.Modifications.Count > 0)
            {
                var modTexts = item.Modifications.Select(m => FormatModification(m));
                lines.Add($"    &CModifiers:&y {string.Join(", ", modTexts)}");
            }
            
            return lines;
        }

        /// <summary>
        /// Gets a simple rarity indicator with color
        /// </summary>
        public static string GetRarityIndicator(string rarity)
        {
            string template = GetRarityTemplate(rarity);
            return $"{{{{{template}|[{rarity.ToUpper()}]}}}}";
        }
    }
}

