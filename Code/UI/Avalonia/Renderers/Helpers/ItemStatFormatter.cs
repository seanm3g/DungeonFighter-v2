using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Formats item statistics for display
    /// </summary>
    public static class ItemStatFormatter
    {
        /// <summary>
        /// Gets formatted item stats for display as a list (one stat per line)
        /// </summary>
        public static List<string> GetItemStats(Item item, Character character)
        {
            var stats = new List<string>();
            if (item == null)
                return stats;

            if (item.Type == ItemType.Consumable && item.RoomSearchConsumableKind != RoomSearchConsumableKind.None)
            {
                if (item.RoomSearchConsumableKind == RoomSearchConsumableKind.Food)
                    stats.Add($"Eat: restores up to {Math.Max(1, item.ConsumableHealAmount)} health.");
                else
                    stats.Add($"Drink: dungeon buff (+{Math.Max(1, item.ConsumablePotionPotency)}).");
                return stats;
            }
            
            if (item is WeaponItem weapon)
            {
                stats.Add($"Damage: {weapon.GetTotalDamage()}");
                stats.Add($"Speed: {weapon.GetTotalAttackSpeed():F2}×");
            }
            else if (item is HeadItem headItem)
            {
                stats.Add($"Armor: +{headItem.GetTotalArmor()}");
            }
            else if (item is ChestItem chestItem)
            {
                stats.Add($"Armor: +{chestItem.GetTotalArmor()}");
            }
            else if (item is LegsItem legsItem)
            {
                stats.Add($"Armor: +{legsItem.GetTotalArmor()}");
            }
            else if (item is FeetItem feetItem)
            {
                stats.Add($"Armor: +{feetItem.GetTotalArmor()}");
            }

            int actionSlotDisplayTotal = GetActionSlotDisplayTotal(item);
            if (item is FeetItem || actionSlotDisplayTotal > 0)
            {
                stats.Add($"Action slots: +{actionSlotDisplayTotal}");
            }
            
            if (item.StatBonuses.Count > 0)
            {
                foreach (var bonus in item.StatBonuses)
                {
                    var parts = new List<string>();
                    foreach (var (contribType, contribValue) in bonus.EnumerateContributions())
                    {
                        string formattedValue = contribType switch
                        {
                            "AttackSpeed" => $"+{contribValue:F3} AttackSpeed",
                            _ => $"+{contribValue} {contribType}"
                        };
                        parts.Add(formattedValue);
                    }

                    string line = string.Join(", ", parts);
                    if (!string.IsNullOrEmpty(bonus.Name))
                        stats.Add($"{bonus.Name}: {line}");
                    else
                        stats.Add(line);
                }
            }
            
            return stats;
        }

        /// <summary>
        /// Formats a stat line string into colored text segments.
        /// </summary>
        /// <param name="stat">Plain stat line (e.g. "Speed: 0.81×").</param>
        /// <param name="displayedItem">Item this line belongs to; used for weapon stat comparison.</param>
        /// <param name="weaponSpeedBaseline">Other weapon for side-by-side compare: attack speed (lower total = faster) and damage (higher total = better). When null, speed/damage values use default styling.</param>
        /// <param name="armorComparisonBaseline">Other armor piece for equip comparison: higher armor = green, lower = red. Head/Chest/Legs/Feet only; when null or not armor, armor value uses default success styling.</param>
        public static List<ColoredText> FormatStatLine(string stat, Item? displayedItem = null, WeaponItem? weaponSpeedBaseline = null, Item? armorComparisonBaseline = null)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("    ", Colors.White);

            if (stat.StartsWith("Armor: +"))
            {
                var parts = stat.Split(new[] { ": +" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Armor: +", ColorPalette.Info);
                    if (TryGetArmorPieceTotal(displayedItem, out int mine) && TryGetArmorPieceTotal(armorComparisonBaseline, out int baseline))
                    {
                        if (mine > baseline)
                            builder.Add(parts[1], ColorPalette.Success);
                        else if (mine < baseline)
                            builder.Add(parts[1], ColorPalette.Error);
                        else
                            builder.Add(parts[1], Colors.White);
                    }
                    else
                    {
                        builder.Add(parts[1], ColorPalette.Success);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Action slots: +"))
            {
                var parts = stat.Split(new[] { ": +" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Action slots: +", ColorPalette.Info);
                    if (TryGetActionSlotDisplayTotal(displayedItem, out int mine) &&
                        TryGetActionSlotDisplayTotal(armorComparisonBaseline, out int baseline))
                    {
                        if (mine > baseline)
                            builder.Add(parts[1], ColorPalette.Success);
                        else if (mine < baseline)
                            builder.Add(parts[1], ColorPalette.Error);
                        else
                            builder.Add(parts[1], Colors.White);
                    }
                    else if (int.TryParse(parts[1], out int slots) && slots > 0)
                    {
                        builder.Add(parts[1], ColorPalette.Success);
                    }
                    else
                    {
                        builder.Add(parts[1], Colors.White);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Damage: "))
            {
                var parts = stat.Split(new[] { ": " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Damage: ", ColorPalette.Info);
                    if (displayedItem is WeaponItem w && weaponSpeedBaseline != null)
                    {
                        int mine = w.GetTotalDamage();
                        int baselineDamage = weaponSpeedBaseline.GetTotalDamage();
                        if (mine > baselineDamage)
                            builder.Add(parts[1], ColorPalette.Success);
                        else if (mine < baselineDamage)
                            builder.Add(parts[1], ColorPalette.Error);
                        else
                            builder.Add(parts[1], Colors.White);
                    }
                    else
                    {
                        builder.Add(parts[1], ColorPalette.Damage);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Speed: "))
            {
                var parts = stat.Split(new[] { ": " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Speed: ", ColorPalette.Info);
                    if (displayedItem is WeaponItem w && weaponSpeedBaseline != null)
                    {
                        double mine = w.GetTotalAttackSpeed();
                        double baseline = weaponSpeedBaseline.GetTotalAttackSpeed();
                        if (mine < baseline)
                            builder.Add(parts[1], ColorPalette.Success);
                        else if (mine > baseline)
                            builder.Add(parts[1], ColorPalette.Error);
                        else
                            builder.Add(parts[1], Colors.White);
                    }
                    else
                    {
                        builder.Add(parts[1], Colors.White);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.Contains("+") && stat.Contains(" "))
            {
                var plusIndex = stat.IndexOf("+");
                if (plusIndex > 0)
                {
                    builder.Add(stat.Substring(0, plusIndex), ColorPalette.Info);
                    var rest = stat.Substring(plusIndex);
                    var spaceIndex = rest.IndexOf(" ");
                    if (spaceIndex > 0)
                    {
                        builder.Add(rest.Substring(0, spaceIndex + 1), ColorPalette.Success);
                        builder.Add(rest.Substring(spaceIndex + 1), Colors.White);
                    }
                    else
                    {
                        builder.Add(rest, ColorPalette.Success);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else
            {
                builder.Add(stat, Colors.White);
            }
            
            return builder.Build();
        }

        private static bool TryGetArmorPieceTotal(Item? item, out int armor)
        {
            armor = 0;
            switch (item)
            {
                case HeadItem h:
                    armor = h.GetTotalArmor();
                    return true;
                case ChestItem c:
                    armor = c.GetTotalArmor();
                    return true;
                case LegsItem l:
                    armor = l.GetTotalArmor();
                    return true;
                case FeetItem f:
                    armor = f.GetTotalArmor();
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetActionSlotDisplayTotal(Item? item, out int slots)
        {
            slots = 0;
            if (item == null)
                return false;

            slots = GetActionSlotDisplayTotal(item);
            return item is FeetItem || slots > 0;
        }

        private static int GetActionSlotDisplayTotal(Item item)
        {
            int slots = Math.Max(0, item.ExtraActionSlots);
            if (item is WeaponItem weapon)
                slots += ClassPresentationConfig.GetEquippedWeaponComboSlotBonus(weapon.WeaponType);
            return slots;
        }
    }
}

