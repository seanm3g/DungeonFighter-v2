using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>All mechanical modifiers on this item (catalog, affixes, suffixes).</summary>
        public static List<ItemStatContribution> GetStatContributions(Item item) =>
            ItemStatContributionCollector.Collect(item);

        /// <summary>
        /// Gets formatted item stats for display as a list (one stat per line)
        /// </summary>
        public static List<string> GetItemStats(Item item, Character character)
        {
            if (item == null)
                return new List<string>();

            return GetStatContributions(item)
                .Select(c => c.ToPlainLine())
                .ToList();
        }

        /// <summary>
        /// Colored stat line for tooltips with buff values highlighted.
        /// </summary>
        public static List<ColoredText> FormatContributionLine(
            ItemStatContribution contribution,
            Item? displayedItem = null,
            WeaponItem? weaponSpeedBaseline = null,
            Item? armorComparisonBaseline = null)
        {
            var builder = new ColoredTextBuilder();
            ItemBuffHighlightFormatting.AppendContributionLine(builder, contribution);
            return ApplyComparisonRecolor(builder.Build(), contribution, displayedItem, weaponSpeedBaseline, armorComparisonBaseline);
        }

        private static List<ColoredText> ApplyComparisonRecolor(
            List<ColoredText> segments,
            ItemStatContribution contribution,
            Item? displayedItem,
            WeaponItem? weaponSpeedBaseline,
            Item? armorComparisonBaseline)
        {
            if (displayedItem == null)
                return segments;

            if (contribution.Label == "Damage" && displayedItem is WeaponItem w && weaponSpeedBaseline != null)
                return RecolorLastNumeric(segments, w.GetTotalDamage(), weaponSpeedBaseline.GetTotalDamage(), higherIsBetter: true);

            if (contribution.Label == "Attack speed" && displayedItem is WeaponItem ws && weaponSpeedBaseline != null)
                return RecolorLastNumeric(segments, ws.GetTotalAttackSpeed(), weaponSpeedBaseline.GetTotalAttackSpeed(), higherIsBetter: false);

            if (contribution.Label == "Armor" && TryGetArmorPieceTotal(displayedItem, out int mine) &&
                TryGetArmorPieceTotal(armorComparisonBaseline, out int baseline))
                return RecolorLastNumeric(segments, mine, baseline, higherIsBetter: true);

            if (contribution.Label == "Action slots" &&
                TryGetActionSlotDisplayTotal(displayedItem, out int mySlots) &&
                TryGetActionSlotDisplayTotal(armorComparisonBaseline, out int baseSlots))
                return RecolorLastNumeric(segments, mySlots, baseSlots, higherIsBetter: true);

            return segments;
        }

        private static List<ColoredText> RecolorLastNumeric(
            List<ColoredText> segments,
            double mine,
            double baseline,
            bool higherIsBetter)
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                string t = segments[i].Text ?? "";
                if (t.Length == 0 || !char.IsDigit(t[0]) && t[0] != '+' && t[0] != '×')
                    continue;

                Color c = Colors.White;
                if (mine > baseline)
                    c = higherIsBetter ? ColorPalette.Success.GetColor() : ColorPalette.Error.GetColor();
                else if (mine < baseline)
                    c = higherIsBetter ? ColorPalette.Error.GetColor() : ColorPalette.Success.GetColor();

                segments[i] = new ColoredText(t, c, segments[i].SourceTemplate, segments[i].ColorReadyForCanvas);
                break;
            }

            return segments;
        }

        public static int GetActionSlotDisplayTotalPublic(Item item) => GetActionSlotDisplayTotal(item);

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
            else if (TryFormatWeaponAttackSpeedLine(stat, builder, displayedItem, weaponSpeedBaseline))
            {
            }
            else if (TryFormatAttributeStyleLine(stat, builder))
            {
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
                        builder.Add(rest.Substring(0, spaceIndex + 1), ColorPalette.Highlight);
                        builder.Add(rest.Substring(spaceIndex + 1), Colors.White);
                    }
                    else
                    {
                        var seg = new ColoredText(rest, ColorPalette.Highlight.GetColor());
                        seg.Undulate(0.07);
                        builder.Add(seg);
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

        private static bool TryFormatWeaponAttackSpeedLine(
            string stat,
            ColoredTextBuilder builder,
            Item? displayedItem,
            WeaponItem? weaponSpeedBaseline)
        {
            string labelPrefix;
            if (stat.StartsWith("Attack speed: ", StringComparison.Ordinal))
                labelPrefix = "Attack speed: ";
            else if (stat.StartsWith("Speed: ", StringComparison.Ordinal))
                labelPrefix = "Speed: ";
            else
                return false;

            string valuePart = stat.Substring(labelPrefix.Length);
            if (valuePart.Length == 0)
            {
                builder.Add(stat, Colors.White);
                return true;
            }

            builder.Add(labelPrefix, ColorPalette.Info);
            if (displayedItem is WeaponItem w && weaponSpeedBaseline != null)
            {
                double mine = w.GetTotalAttackSpeed();
                double baseline = weaponSpeedBaseline.GetTotalAttackSpeed();
                if (mine < baseline)
                    builder.Add(valuePart, ColorPalette.Success);
                else if (mine > baseline)
                    builder.Add(valuePart, ColorPalette.Error);
                else
                    builder.Add(valuePart, Colors.White);
            }
            else
            {
                builder.Add(valuePart, Colors.White);
            }

            return true;
        }

        private static bool TryFormatAttributeStyleLine(string stat, ColoredTextBuilder builder)
        {
            string work = stat;
            int sep = stat.IndexOf(" — ", StringComparison.Ordinal);
            if (sep >= 0)
            {
                builder.Add(stat.Substring(0, sep), Colors.DarkGray);
                builder.Add(" — ", Colors.Gray);
                work = stat.Substring(sep + 3);
            }

            int colon = work.IndexOf(':');
            if (colon <= 0 || colon >= work.Length - 1)
                return false;

            string label = work.Substring(0, colon + 1);
            string valuePart = work.Substring(colon + 1).TrimStart();
            if (valuePart.Length == 0)
                return false;

            builder.Add(label + " ", ColorPalette.Info);
            bool debuff = valuePart.StartsWith("-", StringComparison.Ordinal) ||
                            valuePart.StartsWith("−", StringComparison.Ordinal);
            ItemBuffHighlightFormatting.AppendHighlightedValue(builder, valuePart, debuff);
            return true;
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

