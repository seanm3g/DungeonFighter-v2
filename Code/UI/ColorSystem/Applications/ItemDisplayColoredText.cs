using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using RPGGame;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Formats item displays using the new ColoredText system
    /// Provides clean, maintainable item formatting with proper color separation
    /// </summary>
    public static class ItemDisplayColoredText
    {
        /// <summary>
        /// Rarity color mapping
        /// </summary>
        private static readonly Dictionary<string, ColorPalette> RarityColors = new Dictionary<string, ColorPalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["Common"] = ColorPalette.Common,
            ["Uncommon"] = ColorPalette.Uncommon,
            ["Rare"] = ColorPalette.Rare,
            ["Epic"] = ColorPalette.Epic,
            ["Legendary"] = ColorPalette.Legendary,
            ["Mythic"] = ColorPalette.Purple,
            ["Transcendent"] = ColorPalette.Gold
        };
        
        /// <summary>
        /// Formats a simple item name with rarity color
        /// </summary>
        public static List<ColoredText> FormatSimpleItemName(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            var rarityColor = GetRarityColor(item.Rarity);
            builder.Add(item.Name, rarityColor);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a full item name with prefixes, base name, and suffixes
        /// Each element (prefixes, base name, mods) gets its own color
        /// Note: Rarity should NOT be in the name - it's displayed separately in brackets
        /// </summary>
        public static List<ColoredText> FormatFullItemName(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Parse the item name to extract each component
            string fullName = item.Name;
            string remainingName = fullName;
            
            // Remove any rarity prefixes from the name if they exist (for backwards compatibility)
            // Rarity should only come from item.Rarity property, not from the name
            string[] rarityPrefixes = { "Legendary", "Epic", "Rare", "Uncommon", "Common" };
            foreach (string rarity in rarityPrefixes)
            {
                if (remainingName.StartsWith(rarity + " ", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove rarity prefix from name - it shouldn't be there
                    remainingName = remainingName.Substring(rarity.Length + 1);
                    break;
                }
            }
            
            // 2. Extract and color modification prefixes (not starting with "of ")
            // Process in order they appear in the name, handling multiple prefixes
            var prefixMods = item.Modifications
                .Where(m => !m.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
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
                        // Check if it's at word boundary
                        int modLength = mod.Name.Length;
                        if (remainingName.Length == modLength || 
                            (remainingName.Length > modLength && remainingName[modLength] == ' '))
                        {
                            // Color modification prefix with its own color
                            builder.Add(mod.Name, ColorPalette.Success);
                            
                            // Remove modification from remaining name
                            remainingName = remainingName.Substring(modLength).TrimStart();
                            foundPrefix = true;
                            break; // Restart loop to check for more prefixes
                        }
                    }
                }
            }
            
            // 3. Extract base item name (what's left after removing rarity and prefix mods)
            // Also need to remove suffix mods and stat bonuses from the end
            string baseName = remainingName;
            
            // Collect all suffixes in reverse order (from end to start)
            var allSuffixes = new List<(string name, bool isStatBonus)>();
            
            // Get modification suffixes
            var suffixMods = item.Modifications
                .Where(m => m.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            // Process suffixes from the end until no more are found
            bool foundSuffix = true;
            while (foundSuffix && !string.IsNullOrWhiteSpace(baseName))
            {
                foundSuffix = false;
                
                // Try to match stat bonus suffixes first (they take priority)
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
            
            // Color base item name with its own color (white/neutral)
            // Normalize spaces in base name (replace multiple spaces with single space)
            if (!string.IsNullOrWhiteSpace(baseName))
            {
                string normalizedBaseName = Regex.Replace(baseName.Trim(), @"\s+", " ");
                builder.Add(normalizedBaseName, Colors.White);
            }
            
            // 4. Add all suffixes with their own colors (only color the keyword)
            foreach (var (suffixName, isStatBonus) in allSuffixes)
            {
                builder.Add(" ", Colors.White);
                var (prefix, keyword) = ExtractKeyword(suffixName);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Color only the keyword
                if (isStatBonus)
                {
                    // Color stat bonus keyword with Info color (cyan/blue)
                    builder.Add(keyword, ColorPalette.Info);
                }
                else
                {
                    // Color modification keyword with a different color (Magenta/Purple)
                    builder.Add(keyword, ColorPalette.Magenta);
                }
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats item with rarity tag
        /// </summary>
        public static List<ColoredText> FormatItemWithRarity(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Full item name
            var nameSegments = FormatFullItemName(item);
            builder.Add(nameSegments);
            
            // Rarity tag
            builder.Add("[", Colors.Gray);
            var rarityColor = GetRarityColor(item.Rarity);
            builder.Add(item.Rarity.Trim(), rarityColor);
            builder.Add("]", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats item stats and bonuses
        /// </summary>
        public static List<List<ColoredText>> FormatItemStats(Item item)
        {
            var lines = new List<List<ColoredText>>();
            
            // Type and Tier
            var typeLine = new ColoredTextBuilder();
            typeLine.Add("  Type: ", ColorPalette.Info);
            typeLine.Add(item.Type.ToString(), Colors.White);
            typeLine.Add(" | Tier: ", Colors.Gray);
            typeLine.Add(item.Tier.ToString(), ColorPalette.Warning);
            lines.Add(typeLine.Build());
            
            // Armor value (if applicable)
            if (item is HeadItem headArmor)
            {
                var armorLine = new ColoredTextBuilder();
                armorLine.Add("  Armor: ", ColorPalette.Info);
                armorLine.Add(headArmor.Armor.ToString(), ColorPalette.Success);
                lines.Add(armorLine.Build());
            }
            else if (item is ChestItem bodyArmor)
            {
                var armorLine = new ColoredTextBuilder();
                armorLine.Add("  Armor: ", ColorPalette.Info);
                armorLine.Add(bodyArmor.Armor.ToString(), ColorPalette.Success);
                lines.Add(armorLine.Build());
            }
            else if (item is FeetItem feetArmor)
            {
                var armorLine = new ColoredTextBuilder();
                armorLine.Add("  Armor: ", ColorPalette.Info);
                armorLine.Add(feetArmor.Armor.ToString(), ColorPalette.Success);
                lines.Add(armorLine.Build());
            }
            
            // Damage (for weapons)
            if (item is WeaponItem weapon)
            {
                var damageLine = new ColoredTextBuilder();
                damageLine.Add("  Damage: ", ColorPalette.Info);
                damageLine.Add(weapon.BaseDamage.ToString(), ColorPalette.Damage);
                lines.Add(damageLine.Build());
            }
            
            // Stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                var statsLine = new ColoredTextBuilder();
                statsLine.Add("  Stats: ", ColorPalette.Info);
                
                var bonuses = item.StatBonuses.Select(b => FormatStatBonus(b)).ToList();
                for (int i = 0; i < bonuses.Count; i++)
                {
                    statsLine.Add(bonuses[i]);
                    if (i < bonuses.Count - 1)
                    {
                        statsLine.Add(", ", Colors.Gray);
                    }
                }
                
                lines.Add(statsLine.Build());
            }
            
            // Action bonuses
            if (item.ActionBonuses.Count > 0)
            {
                var actionsLine = new ColoredTextBuilder();
                actionsLine.Add("  Actions: ", ColorPalette.Info);
                
                for (int i = 0; i < item.ActionBonuses.Count; i++)
                {
                    var bonus = item.ActionBonuses[i];
                    actionsLine.Add(bonus.Name, ColorPalette.Success);
                    actionsLine.Add(" +", Colors.White);
                    actionsLine.Add(bonus.Weight.ToString(), ColorPalette.Warning);
                    
                    if (i < item.ActionBonuses.Count - 1)
                    {
                        actionsLine.Add(", ", Colors.Gray);
                    }
                }
                
                lines.Add(actionsLine.Build());
            }
            
            // Modifications (only color the keyword)
            if (item.Modifications.Count > 0)
            {
                var modsLine = new ColoredTextBuilder();
                modsLine.Add("  Modifiers: ", ColorPalette.Info);
                
                for (int i = 0; i < item.Modifications.Count; i++)
                {
                    var mod = item.Modifications[i];
                    var (prefix, keyword) = ExtractKeyword(mod.Name);
                    
                    // Add prefix in white (e.g., "of the ")
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        modsLine.Add(prefix, Colors.White);
                    }
                    
                    // Color only the keyword
                    bool isSuffix = mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase);
                    var modColor = isSuffix ? ColorPalette.Magenta : ColorPalette.Success;
                    modsLine.Add(keyword, modColor);
                    
                    if (i < item.Modifications.Count - 1)
                    {
                        modsLine.Add(", ", Colors.Gray);
                    }
                }
                
                lines.Add(modsLine.Build());
            }
            
            return lines;
        }
        
        /// <summary>
        /// Formats a stat bonus with appropriate color
        /// </summary>
        public static List<ColoredText> FormatStatBonus(StatBonus bonus)
        {
            var builder = new ColoredTextBuilder();
            
            var sign = bonus.Value >= 0 ? "+" : "";
            builder.Add($"{sign}{bonus.Value} ", bonus.Value >= 0 ? ColorPalette.Success : ColorPalette.Error);
            builder.Add(bonus.StatType, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats an item modification (only colors the keyword)
        /// </summary>
        public static List<ColoredText> FormatModification(Modification mod)
        {
            var builder = new ColoredTextBuilder();
            
            var (prefix, keyword) = ExtractKeyword(mod.Name);
            
            // Add prefix in white (e.g., "of the ")
            if (!string.IsNullOrEmpty(prefix))
            {
                builder.Add(prefix, Colors.White);
            }
            
            // Color only the keyword
            bool isSuffix = mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase);
            var color = isSuffix ? ColorPalette.Magenta : ColorPalette.Success;
            builder.Add(keyword, color);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats an inventory list item
        /// </summary>
        public static List<ColoredText> FormatInventoryItem(int index, Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Index
            builder.Add($"{index}. ", Colors.Gray);
            
            // Item name with colors
            var nameSegments = FormatFullItemName(item);
            builder.Add(nameSegments);
            
            // Type and tier in brackets
            builder.Add("[ ", Colors.DarkGray);
            builder.Add(item.Type.ToString(), ColorPalette.Info);
            builder.Add(" T", Colors.Gray);
            builder.Add(item.Tier.ToString(), ColorPalette.Warning);
            builder.Add("]", Colors.DarkGray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats equipped item display
        /// </summary>
        public static List<ColoredText> FormatEquippedItem(string slotName, Item? item)
        {
            var builder = new ColoredTextBuilder();
            
            // Slot name
            builder.Add(slotName, ColorPalette.Info);
            builder.Add(": ", Colors.White);
            
            if (item != null)
            {
                // Equipped item
                var itemSegments = FormatFullItemName(item);
                builder.Add(itemSegments);
                
                // Quick stats
                if (item is WeaponItem weaponItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(weaponItem.BaseDamage.ToString(), ColorPalette.Damage);
                    builder.Add(" dmg", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
                else if (item is HeadItem headItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(headItem.Armor.ToString(), ColorPalette.Success);
                    builder.Add(" armor", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
                else if (item is ChestItem bodyItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(bodyItem.Armor.ToString(), ColorPalette.Success);
                    builder.Add(" armor", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
                else if (item is FeetItem feetItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(feetItem.Armor.ToString(), ColorPalette.Success);
                    builder.Add(" armor", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
            }
            else
            {
                // Empty slot
                builder.Add("(empty)", Colors.DarkGray);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats item comparison for equip decisions
        /// </summary>
        public static List<List<ColoredText>> FormatItemComparison(Item newItem, Item? currentItem)
        {
            var lines = new List<List<ColoredText>>();
            
            // New item header
            var newHeader = new ColoredTextBuilder();
            newHeader.Add("NEW: ", ColorPalette.Success);
            newHeader.Add(FormatItemWithRarity(newItem));
            lines.Add(newHeader.Build());
            
            // New item stats
            lines.AddRange(FormatItemStats(newItem));
            
            // Spacer
            lines.Add(new List<ColoredText>());
            
            // Current item header
            if (currentItem != null)
            {
                var currentHeader = new ColoredTextBuilder();
                currentHeader.Add("CURRENT: ", ColorPalette.Warning);
                currentHeader.Add(FormatItemWithRarity(currentItem));
                lines.Add(currentHeader.Build());
                
                // Current item stats
                lines.AddRange(FormatItemStats(currentItem));
            }
            else
            {
                var emptyLine = new ColoredTextBuilder();
                emptyLine.Add("CURRENT: ", ColorPalette.Warning);
                emptyLine.Add("(empty slot)", Colors.DarkGray);
                lines.Add(emptyLine.Build());
            }
            
            return lines;
        }
        
        /// <summary>
        /// Formats loot drop message
        /// </summary>
        public static List<ColoredText> FormatLootDrop(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("Found: ", ColorPalette.Success);
            var itemSegments = FormatItemWithRarity(item);
            builder.Add(itemSegments);
            builder.Add("!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats loot for dungeon completion screen with rarity in brackets
        /// Format: [Rarity] ItemName (with each element colored)
        /// </summary>
        public static List<ColoredText> FormatLootForCompletion(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Add rarity in brackets with rarity color
            builder.Add("[", Colors.Gray);
            var rarityColor = GetRarityColor(item.Rarity);
            builder.Add(item.Rarity.Trim(), rarityColor);
            builder.Add("]", Colors.Gray);
            
            // Add full item name with all colored elements
            var itemSegments = FormatFullItemName(item);
            builder.AddRange(itemSegments);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Extracts the keyword from a modification or stat bonus name
        /// For "of the X" patterns, returns only "X" (the keyword)
        /// For other patterns, returns the full name
        /// </summary>
        private static (string prefix, string keyword) ExtractKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
                return ("", "");
            
            // Check for "of the X" pattern
            if (name.StartsWith("of the ", StringComparison.OrdinalIgnoreCase))
            {
                string keyword = name.Substring(7); // "of the " is 7 characters
                return ("of the ", keyword);
            }
            
            // Check for "of X" pattern (without "the")
            if (name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
            {
                string keyword = name.Substring(3); // "of " is 3 characters
                return ("of ", keyword);
            }
            
            // No prefix pattern, return full name as keyword
            return ("", name);
        }
        
        /// <summary>
        /// Gets the color for an item rarity
        /// </summary>
        private static ColorPalette GetRarityColor(string rarity)
        {
            if (RarityColors.TryGetValue(rarity, out var color))
            {
                return color;
            }
            return ColorPalette.Common; // Default to common/white
        }
        
        /// <summary>
        /// Extension method to add multiple colored text segments
        /// </summary>
        private static ColoredTextBuilder Add(this ColoredTextBuilder builder, List<ColoredText> segments)
        {
            foreach (var segment in segments)
            {
                builder.Add(segment.Text, segment.Color);
            }
            return builder;
        }
    }
}
