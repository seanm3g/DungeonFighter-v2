using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RPGGame;

namespace RPGGame.UI.ColorSystem.Applications.ItemFormatting
{
    /// <summary>
    /// Parses item names to extract components (rarity, prefixes, base name, suffixes)
    /// </summary>
    public static class ItemNameParser
    {
        /// <summary>
        /// Parsed item name components
        /// </summary>
        public class ParsedItemName
        {
            public string BaseName { get; set; } = "";
            public List<(string name, bool isStatBonus)> Suffixes { get; set; } = new List<(string, bool)>();
            public string RemainingName { get; set; } = "";
        }
        
        /// <summary>
        /// Parses an item name to extract base name and suffixes
        /// </summary>
        public static ParsedItemName ParseItemName(Item item, string remainingName)
        {
            var result = new ParsedItemName
            {
                RemainingName = remainingName ?? ""
            };
            
            string baseName = remainingName ?? "";
            
            // Collect all suffixes in reverse order (from end to start)
            var allSuffixes = new List<(string name, bool isStatBonus)>();
            
            if (item?.Modifications == null)
            {
                result.BaseName = baseName;
                result.Suffixes = allSuffixes;
                return result;
            }
            
            // Get modification suffixes
            var suffixMods = item.Modifications
                .Where(m => m != null && !string.IsNullOrEmpty(m.Name) && m.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            // Process suffixes from the end until no more are found
            bool foundSuffix = true;
            while (foundSuffix && !string.IsNullOrWhiteSpace(baseName))
            {
                foundSuffix = false;
                
                // Try to match stat bonus suffixes first (they take priority)
                if (item.StatBonuses != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        if (!string.IsNullOrEmpty(statBonus.Name) &&
                            baseName.EndsWith(statBonus.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            int bonusLength = statBonus.Name.Length;
                            int startIndex = baseName.Length - bonusLength;
                            if (startIndex == 0 || baseName[startIndex - 1] == ' ')
                            {
                                allSuffixes.Insert(0, (statBonus.Name, true));
                                baseName = baseName.Substring(0, startIndex).TrimEnd();
                                foundSuffix = true;
                                break;
                            }
                        }
                    }
                }
                
                // If no stat bonus found, try modification suffixes
                if (!foundSuffix)
                {
                    foreach (var mod in suffixMods)
                    {
                        if (!string.IsNullOrEmpty(mod.Name) &&
                            baseName.EndsWith(mod.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            int modLength = mod.Name.Length;
                            int startIndex = baseName.Length - modLength;
                            if (startIndex == 0 || baseName[startIndex - 1] == ' ')
                            {
                                allSuffixes.Insert(0, (mod.Name, false));
                                baseName = baseName.Substring(0, startIndex).TrimEnd();
                                foundSuffix = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            // Normalize spaces in base name (replace multiple spaces with single space)
            if (!string.IsNullOrWhiteSpace(baseName))
            {
                result.BaseName = Regex.Replace(baseName.Trim(), @"\s+", " ");
            }
            
            result.Suffixes = allSuffixes;
            
            return result;
        }
        
        /// <summary>
        /// Removes rarity prefix from item name if present
        /// </summary>
        public static string RemoveRarityPrefix(string name)
        {
            string remainingName = name;
            string[] rarityPrefixes = { "Legendary", "Epic", "Rare", "Uncommon", "Common" };
            
            foreach (string rarity in rarityPrefixes)
            {
                if (remainingName.StartsWith(rarity + " ", StringComparison.OrdinalIgnoreCase))
                {
                    remainingName = remainingName.Substring(rarity.Length + 1);
                    break;
                }
            }
            
            return remainingName;
        }
        
        /// <summary>
        /// Extracts modification prefixes from the beginning of a name
        /// </summary>
        public static (string remainingName, List<string> prefixes) ExtractPrefixModifications(Item item, string name)
        {
            var prefixes = new List<string>();
            string remainingName = name ?? "";
            
            if (item?.Modifications == null)
            {
                return (remainingName, prefixes);
            }
            
            var prefixMods = item.Modifications
                .Where(m => m != null && !string.IsNullOrEmpty(m.Name) && !m.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            bool foundPrefix = true;
            while (foundPrefix && !string.IsNullOrWhiteSpace(remainingName))
            {
                foundPrefix = false;
                foreach (var mod in prefixMods)
                {
                    if (!string.IsNullOrEmpty(mod.Name) && 
                        remainingName.StartsWith(mod.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        int modLength = mod.Name.Length;
                        if (remainingName.Length == modLength || 
                            (remainingName.Length > modLength && remainingName[modLength] == ' '))
                        {
                            prefixes.Add(mod.Name);
                            remainingName = remainingName.Substring(modLength).TrimStart();
                            foundPrefix = true;
                            break;
                        }
                    }
                }
            }
            
            return (remainingName, prefixes);
        }
    }
}

