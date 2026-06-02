using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame
{
    /// <summary>
    /// Builds organized, colored hover tooltip lines for inventory and equipped items.
    /// </summary>
    public static class ItemTooltipFormatter
    {
        public static List<List<ColoredText>> BuildItemTooltipLines(
            Character? character,
            Item item,
            string slotLabel,
            int maxLines = 24)
        {
            var lines = new List<List<ColoredText>>();
            if (item == null || maxLines < 1)
                return lines;

            bool equipBlocked = ItemRendererHelper.IsEquipBlockedForCharacter(item, character);

            AddLine(lines, BuildSlotLine(slotLabel));
            AddBlank(lines);
            AddLine(lines, ItemDisplayColoredText.FormatFullItemName(item));
            AddLine(lines, BuildMetaLine(item));
            if (maxLines <= lines.Count) return Trim(lines, maxLines);

            string? reqSummary = item.GetAttributeRequirementsSummaryLine();
            if (!string.IsNullOrEmpty(reqSummary))
            {
                AddBlank(lines);
                AddLine(lines, BuildRequirementsLine(reqSummary, equipBlocked));
            }
            if (maxLines <= lines.Count) return Trim(lines, maxLines);

            var contributions = ItemStatFormatter.GetStatContributions(item);
            if (contributions.Count > 0)
            {
                AddBlank(lines);
                AddLine(lines, SectionHeader("Stats"));
                foreach (var contribution in contributions)
                {
                    if (lines.Count >= maxLines) break;
                    var segments = ItemStatFormatter.FormatContributionLine(contribution, item);
                    if (equipBlocked)
                        segments = RecolorAll(segments, ColorPalette.Error.GetColor());
                    AddLine(lines, segments);
                }
            }

            if (item is not WeaponItem && lines.Count < maxLines)
            {
                string? armorBreakdown = BuildArmorBreakdownLine(item);
                if (!string.IsNullOrEmpty(armorBreakdown))
                {
                    var armorDetail = new ColoredTextBuilder();
                    armorDetail.Add("  ", Colors.Gray);
                    armorDetail.Add("Armor breakdown: ", ColorPalette.Info);
                    ItemBuffHighlightFormatting.AppendHighlightedDescription(armorDetail, armorBreakdown);
                    AddLine(lines, armorDetail.Build());
                }
            }

            if (item.Modifications != null && item.Modifications.Count > 0 && lines.Count < maxLines)
            {
                AddBlank(lines);
                AddLine(lines, SectionHeader("Affixes"));
                foreach (var mod in item.Modifications)
                {
                    if (lines.Count >= maxLines) break;
                    AddLine(lines, BuildModificationLine(item, mod));
                }
            }

            if (character != null)
            {
                var resolvedGearActions = character.Equipment.GetGearActions(item);
                if (resolvedGearActions != null && resolvedGearActions.Count > 0 && lines.Count < maxLines)
                {
                    AddBlank(lines);
                    AddLine(lines, LabelValueLine("", "Grants", string.Join(", ", resolvedGearActions), ColorPalette.Info, ColorPalette.Success.GetColor()));
                }
            }

            if (!string.IsNullOrEmpty(item.GearAction) &&
                Data.GameDataTagHelper.IsGrantableOnHeroGearByName(item.GearAction) &&
                lines.Count < maxLines)
            {
                AddLine(lines, LabelValueLine("", "Gear action", item.GearAction, ColorPalette.Info, Colors.White));
            }

            var actionBonusesToShow = item.ActionBonuses?
                .Where(ab => Data.GameDataTagHelper.IsGrantableOnHeroGearByName(ab.Name))
                .ToList();

            if (actionBonusesToShow != null && actionBonusesToShow.Count > 0 && lines.Count < maxLines)
            {
                AddBlank(lines);
                AddLine(lines, SectionHeader("Action bonuses"));
                foreach (var ab in actionBonusesToShow)
                {
                    if (lines.Count >= maxLines) break;
                    var b = new ColoredTextBuilder();
                    b.Add(string.IsNullOrEmpty(ab.Name) ? "Bonus" : ab.Name, ColorPalette.Success.GetColor());
                    if (!string.IsNullOrEmpty(ab.Description))
                    {
                        b.Add(": ", Colors.Gray);
                        b.Add(ab.Description, Colors.White);
                    }
                    AddLine(lines, b.Build());
                }
            }

            if (item.ArmorStatuses != null && item.ArmorStatuses.Count > 0 && lines.Count < maxLines)
            {
                AddBlank(lines);
                AddLine(lines, SectionHeader("On hit"));
                foreach (var st in item.ArmorStatuses)
                {
                    if (lines.Count >= maxLines) break;
                    var b = new ColoredTextBuilder();
                    b.Add(string.IsNullOrEmpty(st.Name) ? "Effect" : st.Name, ColorPalette.Warning.GetColor());
                    if (!string.IsNullOrEmpty(st.Description))
                    {
                        b.Add(": ", Colors.Gray);
                        b.Add(st.Description, Colors.White);
                    }
                    AddLine(lines, b.Build());
                }
            }

            return Trim(lines, maxLines);
        }

        private static List<ColoredText> BuildSlotLine(string slotLabel)
        {
            var b = new ColoredTextBuilder();
            b.Add(slotLabel, Colors.DarkGray);
            b.Add(" slot", Colors.Gray);
            return b.Build();
        }

        private static List<ColoredText> BuildMetaLine(Item item)
        {
            var b = new ColoredTextBuilder();
            string rarity = (item.Rarity ?? "Common").Trim();
            b.Add(rarity, ItemThemeProvider.GetRarityColor(rarity));
            b.Add($" · Tier {item.Tier}", ColorPalette.Warning.GetColor());
            b.Add($" · Lv {item.Level}", Colors.Gray);
            return b.Build();
        }

        private static List<ColoredText> BuildRequirementsLine(string reqSummary, bool blocked)
        {
            var b = new ColoredTextBuilder();
            b.Add("Requires ", ColorPalette.Info.GetColor());
            string text = reqSummary.StartsWith("Requires:", StringComparison.Ordinal)
                ? reqSummary.Substring("Requires:".Length).TrimStart()
                : reqSummary;
            b.Add(text, blocked ? ColorPalette.Error.GetColor() : ColorPalette.Warning.GetColor());
            if (blocked)
            {
                b.Add(" (not met)", ColorPalette.Error.GetColor());
            }
            return b.Build();
        }

        private static List<ColoredText> BuildModificationLine(Item item, Modification mod)
        {
            var b = new ColoredTextBuilder();
            b.Add("  ", Colors.Gray);
            b.AddRange(ItemStatsFormatter.FormatModification(mod, item.Rarity ?? "Common"));
            b.Add(" — ", Colors.DarkGray);
            string effect = ItemDisplayFormatter.GetModificationEffectDescription(mod);
            bool debuff = string.Equals(mod.Effect, "gearPrimaryStatMultiplier", StringComparison.OrdinalIgnoreCase) &&
                            mod.RolledValue > 0 && mod.RolledValue < 1.0;
            ItemBuffHighlightFormatting.AppendHighlightedDescription(b, effect, debuff);
            if (!string.IsNullOrEmpty(mod.Description))
            {
                b.Add(" · ", Colors.DarkGray);
                b.Add(mod.Description, Colors.Gray);
            }
            return b.Build();
        }

        private static string? BuildArmorBreakdownLine(Item item)
        {
            int? baseArmor = item switch
            {
                HeadItem h => h.Armor,
                ChestItem c => c.Armor,
                LegsItem l => l.Armor,
                FeetItem f => f.Armor,
                _ => null
            };
            if (!baseArmor.HasValue)
                return null;

            int affixArmor = GetAffixOnlyArmorBonus(item);
            double quality = ItemPrefixHelper.GetGearPrimaryStatMultiplier(item);
            int total = item switch
            {
                HeadItem h => h.GetTotalArmor(),
                ChestItem c => c.GetTotalArmor(),
                LegsItem l => l.GetTotalArmor(),
                FeetItem f => f.GetTotalArmor(),
                _ => 0
            };

            var parts = new List<string> { $"base {baseArmor.Value}" };
            if (affixArmor != 0)
                parts.Add($"affixes +{affixArmor}");
            if (Math.Abs(quality - 1.0) > 0.001)
                parts.Add($"quality ×{quality.ToString("0.##", CultureInfo.InvariantCulture)}");
            return $"{total} ({string.Join(", ", parts)})";
        }

        private static int GetAffixOnlyArmorBonus(Item item)
        {
            int fromZero = 0;
            if (item.StatBonuses != null)
            {
                foreach (var statBonus in item.StatBonuses)
                {
                    if (statBonus != null)
                        fromZero += statBonus.SumContributionValuesForStatType("Armor");
                }
            }
            if (item.Modifications != null)
            {
                foreach (var modification in item.Modifications)
                {
                    if (modification?.Effect != null &&
                        modification.Effect.Contains("armor", StringComparison.OrdinalIgnoreCase))
                        fromZero += (int)modification.RolledValue;
                }
            }
            return fromZero;
        }

        private static List<ColoredText> SectionHeader(string title)
        {
            var b = new ColoredTextBuilder();
            b.Add(title, ColorPalette.Info.GetColor());
            return b.Build();
        }

        private static List<ColoredText> LabelValueLine(string indent, string label, string value, ColorPalette labelColor, Color valueColor)
        {
            var b = new ColoredTextBuilder();
            b.Add(indent, Colors.Gray);
            b.Add(label + ": ", labelColor.GetColor());
            b.Add(value, valueColor);
            return b.Build();
        }

        private static void TrimLeadingIndent(List<ColoredText> segments)
        {
            if (segments.Count > 0 && segments[0].Text == "    ")
                segments.RemoveAt(0);
        }

        private static List<ColoredText> RecolorAll(List<ColoredText> segments, Color color)
        {
            return segments.Select(s => new ColoredText(s.Text, color, s.SourceTemplate, s.ColorReadyForCanvas)).ToList();
        }

        private static void AddLine(List<List<ColoredText>> lines, List<ColoredText> line)
        {
            if (line != null)
                lines.Add(line);
        }

        private static void AddBlank(List<List<ColoredText>> lines) => lines.Add(new List<ColoredText>());

        private static List<List<ColoredText>> Trim(List<List<ColoredText>> lines, int maxLines) =>
            lines.Count <= maxLines ? lines : lines.GetRange(0, maxLines);
    }
}
