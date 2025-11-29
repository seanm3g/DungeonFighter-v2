using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame
{
    /// <summary>
    /// Centralized formatter for item display logic to eliminate duplication between display managers.
    /// 
    /// 📖 FOR COMPLETE FORMATTING GUIDE: See Documentation/04-Reference/FORMATTING_SYSTEM_GUIDE.md
    /// 
    /// Quick Reference:
    /// - Format item name: GetColoredItemName() or GetColoredFullItemName()
    /// - Format item stats: GetItemStatsDisplay()
    /// - Format bonuses: FormatItemBonusesWithColor()
    /// - All methods use the new ColoredText system for consistent output
    /// </summary>
    public static class ItemDisplayFormatter
    {
        /// <summary>
        /// Formats a stat bonus with appropriate display format
        /// </summary>
        public static string FormatStatBonus(StatBonus bonus)
        {
            return bonus.StatType switch
            {
                "AttackSpeed" => $"AttackSpeed +{bonus.Value:F2}s",
                _ => $"{bonus.StatType} +{bonus.Value}"
            };
        }

        /// <summary>
        /// Cleans a stat bonus name by removing the "of " prefix if present
        /// </summary>
        public static string CleanStatBonusName(string name)
        {
            if (name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(3); // Remove "of " (3 characters)
            }
            return name;
        }

        /// <summary>
        /// Gets a descriptive text for a modification showing what the value does
        /// </summary>
        public static string GetModificationDisplayText(Modification modification)
        {
            return modification.Effect switch
            {
                "damage" => $"{modification.Name} ({modification.RolledValue:+0;-0;+0} damage)",
                "speedMultiplier" => $"{modification.Name} ({(modification.RolledValue - 1.0) * 100:F0}% faster)",
                "rollBonus" => $"{modification.Name} (+{modification.RolledValue:F0} to rolls)",
                "damageMultiplier" => $"{modification.Name} ({(modification.RolledValue - 1.0) * 100:F0}% more damage)",
                "lifesteal" => $"{modification.Name} ({modification.RolledValue * 100:F1}% lifesteal)",
                "magicFind" => $"{modification.Name} (+{modification.RolledValue:F0} magic find)",
                "bleedChance" => $"{modification.Name} ({modification.RolledValue * 100:F0}% bleed chance)",
                "uniqueActionChance" => $"{modification.Name} ({modification.RolledValue * 100:F1}% unique action chance)",
                "godlike" => $"{modification.Name} (+{modification.RolledValue:F0} to rolls & +1 STR)",
                "autoSuccess" => $"{modification.Name} (auto-success)",
                "reroll" => $"{modification.Name} (reroll with +{modification.RolledValue:F0} bonus)",
                "durability" => $"{modification.Name} (+{modification.RolledValue:F0} durability)",
                _ => $"{modification.Name} ({modification.RolledValue:F1})"
            };
        }

        /// <summary>
        /// Gets the display type for an item
        /// </summary>
        public static string GetDisplayType(Item item)
        {
            return item.Type switch
            {
                ItemType.Weapon => GetWeaponClassDisplay(item as WeaponItem),
                ItemType.Head => "Head",
                ItemType.Chest => "Chest",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }

        /// <summary>
        /// Gets the weapon class display string
        /// </summary>
        public static string GetWeaponClassDisplay(WeaponItem? weapon)
        {
            if (weapon == null) return "Weapon";
            
            return weapon.WeaponType switch
            {
                WeaponType.Mace => "Barbarian Weapon",
                WeaponType.Sword => "Warrior Weapon",
                WeaponType.Dagger => "Rogue Weapon",
                WeaponType.Wand => "Wizard Weapon",
                _ => "Weapon"
            };
        }

        /// <summary>
        /// Gets armor difference display for comparison
        /// </summary>
        public static string GetArmorDiff(Item invItem, Item? equipped)
        {
            if (equipped == null) return " (NEW)";
            
            int invArmor = invItem switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };
            
            int equippedArmor = equipped switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };
            
            int diff = invArmor - equippedArmor;
            if (diff > 0) return $" (+{diff})";
            if (diff < 0) return $" ({diff})";
            return " (=)";
        }

        /// <summary>
        /// Gets weapon difference display for comparison
        /// </summary>
        public static string GetWeaponDiff(WeaponItem invWeapon, WeaponItem? equipped)
        {
            if (equipped == null) return " (NEW)";
            
            int damageDiff = invWeapon.GetTotalDamage() - equipped.GetTotalDamage();
            if (damageDiff > 0) return $" (+{damageDiff} damage)";
            if (damageDiff < 0) return $" ({damageDiff} damage)";
            return " (= damage)";
        }

        /// <summary>
        /// Formats item bonuses and modifications for display
        /// </summary>
        public static void FormatItemBonuses(Item item, Action<string> writeLine)
        {
            // Show stat bonuses with clear articulation
            if (item.StatBonuses.Count > 0)
            {
                var bonusTexts = item.StatBonuses.Select(b => $"{CleanStatBonusName(b.Name)} ({FormatStatBonus(b)})");
                writeLine($"    Stat Bonuses: {string.Join(", ", bonusTexts)}");
            }
            
            // Show action bonuses (legacy system)
            if (item.ActionBonuses.Count > 0)
            {
                writeLine($"    Action Bonuses: {string.Join(", ", item.ActionBonuses.Select(b => $"{b.Name} +{b.Weight}"))}");
            }
            
            // Show modifications with clear articulation
            if (item.Modifications.Count > 0)
            {
                var modificationTexts = item.Modifications.Select(m => GetModificationDisplayText(m));
                writeLine($"    Modifications: {string.Join(", ", modificationTexts)}");
            }
        }

        /// <summary>
        /// Gets item stats display string for inventory items
        /// </summary>
        public static string GetItemStatsDisplay(Item item, Character player)
        {
            return item switch
            {
                WeaponItem weapon => $"Damage: {weapon.GetTotalDamage()}{GetWeaponDiff(weapon, player.Weapon as WeaponItem)}, Attack Speed: {weapon.GetAttackSpeedMultiplier():F1}x",
                HeadItem head => $"Armor: {head.GetTotalArmor()}{GetArmorDiff(head, player.Head)}",
                ChestItem chest => $"Armor: {chest.GetTotalArmor()}{GetArmorDiff(chest, player.Body)}",
                FeetItem feet => $"Armor: {feet.GetTotalArmor()}{GetArmorDiff(feet, player.Feet)}",
                _ => ""
            };
        }

        /// <summary>
        /// Gets colored item name with rarity, modifications, and proper element coloring
        /// </summary>
        public static string GetColoredItemName(Item item)
        {
            // Use full item name formatting to include modifications with proper colors
            var coloredText = ItemColorSystem.FormatFullItemName(item);
            return ColoredTextRenderer.RenderAsPlainText(coloredText);
        }

        /// <summary>
        /// Gets colored item name with prefixes and suffixes
        /// </summary>
        public static string GetColoredFullItemName(Item item)
        {
            var coloredText = ItemColorSystem.FormatFullItemName(item);
            return ColoredTextRenderer.RenderAsPlainText(coloredText);
        }

        /// <summary>
        /// Formats item bonuses with colors for display
        /// </summary>
        public static void FormatItemBonusesWithColor(Item item, Action<string> writeLine)
        {
            // Show stat bonuses with color
            if (item.StatBonuses.Count > 0)
            {
                var bonusTexts = item.StatBonuses.Select(b => 
                {
                    var coloredNameList = ItemColorSystem.FormatStatBonus(b);
                    string coloredName = ColoredTextRenderer.RenderAsPlainText(coloredNameList);
                    string formatted = FormatStatBonus(b);
                    return $"{coloredName} ({formatted})";
                });
                var statsBuilder = new ColoredTextBuilder();
                statsBuilder.Add("    ", Colors.White);
                statsBuilder.Add("Stats:", ColorPalette.Cyan);
                statsBuilder.Add(" ", Colors.White);
                statsBuilder.Add(string.Join(", ", bonusTexts), Colors.White);
                writeLine(ColoredTextRenderer.RenderAsMarkup(statsBuilder.Build()));
            }
            
            // Show action bonuses (legacy system)
            if (item.ActionBonuses.Count > 0)
            {
                var actionsBuilder = new ColoredTextBuilder();
                actionsBuilder.Add("    ", Colors.White);
                actionsBuilder.Add("Actions:", ColorPalette.Cyan);
                actionsBuilder.Add(" ", Colors.White);
                var actionParts = item.ActionBonuses.Select(b => 
                {
                    var partBuilder = new ColoredTextBuilder();
                    partBuilder.Add(b.Name, ColorPalette.Green);
                    partBuilder.Add(" +", Colors.White);
                    partBuilder.Add(b.Weight.ToString(), Colors.White);
                    return ColoredTextRenderer.RenderAsPlainText(partBuilder.Build());
                });
                actionsBuilder.Add(string.Join(", ", actionParts), Colors.White);
                writeLine(ColoredTextRenderer.RenderAsMarkup(actionsBuilder.Build()));
            }
            
            // Show modifications with color
            if (item.Modifications.Count > 0)
            {
                var modificationTexts = item.Modifications.Select(m => 
                {
                    var coloredNameList = ItemColorSystem.FormatModification(m);
                    string coloredName = ColoredTextRenderer.RenderAsPlainText(coloredNameList);
                    string details = GetModificationDisplayText(m);
                    var modBuilder = new ColoredTextBuilder();
                    modBuilder.AddRange(coloredNameList);
                    modBuilder.Add(" (", Colors.White);
                    modBuilder.Add(details.Substring(m.Name.Length + 1), Colors.White);
                    modBuilder.Add(")", Colors.White);
                    return ColoredTextRenderer.RenderAsPlainText(modBuilder.Build());
                });
                var modifiersBuilder = new ColoredTextBuilder();
                modifiersBuilder.Add("    ", Colors.White);
                modifiersBuilder.Add("Modifiers:", ColorPalette.Cyan);
                modifiersBuilder.Add(" ", Colors.White);
                modifiersBuilder.Add(string.Join(", ", modificationTexts), Colors.White);
                writeLine(ColoredTextRenderer.RenderAsMarkup(modifiersBuilder.Build()));
            }
        }
        
        // ===== NEW COLORED TEXT SYSTEM WRAPPERS =====
        
        /// <summary>
        /// Gets colored item name (simple) using new ColoredText system
        /// </summary>
        public static List<ColoredText> GetColoredItemNameNew(Item item)
        {
            return ItemDisplayColoredText.FormatSimpleItemName(item);
        }
        
        /// <summary>
        /// Gets colored full item name using new ColoredText system
        /// </summary>
        public static List<ColoredText> GetColoredFullItemNameNew(Item item)
        {
            return ItemDisplayColoredText.FormatFullItemName(item);
        }
        
        /// <summary>
        /// Gets colored item with rarity using new ColoredText system
        /// </summary>
        public static List<ColoredText> GetColoredItemWithRarityNew(Item item)
        {
            return ItemDisplayColoredText.FormatItemWithRarity(item);
        }
        
        /// <summary>
        /// Formats item stats using new ColoredText system
        /// </summary>
        public static List<List<ColoredText>> FormatItemStatsNew(Item item)
        {
            return ItemDisplayColoredText.FormatItemStats(item);
        }
        
        /// <summary>
        /// Formats stat bonus using new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatStatBonusNew(StatBonus bonus)
        {
            return ItemDisplayColoredText.FormatStatBonus(bonus);
        }
        
        /// <summary>
        /// Formats inventory item using new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatInventoryItemNew(int index, Item item)
        {
            return ItemDisplayColoredText.FormatInventoryItem(index, item);
        }
        
        /// <summary>
        /// Formats equipped item using new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEquippedItemNew(string slotName, Item? item)
        {
            return ItemDisplayColoredText.FormatEquippedItem(slotName, item);
        }
        
        /// <summary>
        /// Formats item comparison using new ColoredText system
        /// </summary>
        public static List<List<ColoredText>> FormatItemComparisonNew(Item newItem, Item? currentItem)
        {
            return ItemDisplayColoredText.FormatItemComparison(newItem, currentItem);
        }
        
        /// <summary>
        /// Formats loot drop using new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatLootDropNew(Item item)
        {
            return ItemDisplayColoredText.FormatLootDrop(item);
        }
    }
}
