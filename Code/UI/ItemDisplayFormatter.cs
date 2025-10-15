using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI;

namespace RPGGame
{
    /// <summary>
    /// Centralized formatter for item display logic to eliminate duplication between display managers
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
        /// Gets colored item name with rarity
        /// </summary>
        public static string GetColoredItemName(Item item)
        {
            return ItemColorSystem.FormatSimpleItemDisplay(item);
        }

        /// <summary>
        /// Gets colored item name with prefixes and suffixes
        /// </summary>
        public static string GetColoredFullItemName(Item item)
        {
            return ItemColorSystem.FormatFullItemName(item);
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
                    string coloredName = ItemColorSystem.FormatStatBonus(b);
                    string formatted = FormatStatBonus(b);
                    return $"{coloredName} ({formatted})";
                });
                writeLine($"    &CStats:&y {string.Join(", ", bonusTexts)}");
            }
            
            // Show action bonuses (legacy system)
            if (item.ActionBonuses.Count > 0)
            {
                writeLine($"    &CActions:&y {string.Join(", ", item.ActionBonuses.Select(b => $"&G{b.Name}&y +{b.Weight}"))}");
            }
            
            // Show modifications with color
            if (item.Modifications.Count > 0)
            {
                var modificationTexts = item.Modifications.Select(m => 
                {
                    string coloredName = ItemColorSystem.FormatModification(m);
                    string details = GetModificationDisplayText(m);
                    return $"{coloredName} &y({details.Substring(m.Name.Length + 1)})"; // Remove name from details since we already colored it
                });
                writeLine($"    &CModifiers:&y {string.Join(", ", modificationTexts)}");
            }
        }
    }
}
