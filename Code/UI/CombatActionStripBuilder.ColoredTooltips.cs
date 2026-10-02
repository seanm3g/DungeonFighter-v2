using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame
{
    public static partial class CombatActionStripBuilder
    {
        /// <summary>
        /// Colored, spaced action tooltip lines (Name / Rarity / Stats / Triggers; Alt for extended).
        /// Used by F7 narrative prose hover; mirrors item tip structure.
        /// </summary>
        public static List<List<ColoredText>> BuildColoredActionTooltipLines(
            Character? character,
            Action? action,
            int maxLines = 28,
            ActionStripDamageLineMode swingLineMode = ActionStripDamageLineMode.EffectiveWithComboAmp,
            bool includeExtendedDetails = false)
        {
            var lines = new List<List<ColoredText>>();
            if (character == null || action == null || maxLines < 1)
                return lines;

            int panelIndex = -1;
            var combo = character.GetComboActions();
            for (int i = 0; i < combo.Count; i++)
            {
                if (ReferenceEquals(combo[i], action)
                    || (!string.IsNullOrWhiteSpace(action.Name)
                        && string.Equals(combo[i]?.Name, action.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    panelIndex = i;
                    break;
                }
            }

            return BuildColoredActionTooltipLinesCore(
                character, action, panelIndex, maxLines, swingLineMode, includeExtendedDetails);
        }

        /// <summary>
        /// Resolves an action by catalog name (combo match first, then <see cref="ActionLoader"/>).
        /// </summary>
        public static Action? ResolveActionForTooltip(Character? character, string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return null;

            string name = actionName.Trim();
            if (character != null)
            {
                foreach (var a in character.GetComboActions())
                {
                    if (a != null && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
                        return a;
                }
                foreach (var a in character.GetActionPool())
                {
                    if (a != null && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
                        return a;
                }
            }

            return ActionLoader.GetAction(name);
        }

        /// <summary>
        /// Colored tip for a catalog action name (narrative log hover).
        /// </summary>
        public static List<List<ColoredText>> BuildColoredActionTooltipLinesForName(
            Character? character,
            string? actionName,
            int maxLines = 28,
            ActionStripDamageLineMode swingLineMode = ActionStripDamageLineMode.EffectiveWithComboAmp,
            bool includeExtendedDetails = false)
        {
            var action = ResolveActionForTooltip(character, actionName);
            return BuildColoredActionTooltipLines(character, action, maxLines, swingLineMode, includeExtendedDetails);
        }

        private static List<List<ColoredText>> BuildColoredActionTooltipLinesCore(
            Character? character,
            Action action,
            int panelIndex,
            int maxLines,
            ActionStripDamageLineMode swingLineMode,
            bool includeExtendedDetails)
        {
            var lines = new List<List<ColoredText>>();

            AddColoredLine(lines, BuildActionTitleLine(action.Name));
            if (lines.Count >= maxLines) return TrimColored(lines, maxLines);

            string rarity = ResolveActionRarityLabel(action);
            if (!string.IsNullOrEmpty(rarity))
                AddColoredLine(lines, BuildActionRarityLine(rarity));
            if (lines.Count >= maxLines) return TrimColored(lines, maxLines);

            var stats = BuildPrimaryStatSegments(character, action, panelIndex, swingLineMode);
            if (stats.Count > 0)
            {
                AddColoredBlank(lines);
                AddColoredLine(lines, ActionSectionHeader("Stats"));
                foreach (string segment in stats)
                {
                    if (lines.Count >= maxLines) break;
                    AddColoredLine(lines, BuildActionBodyLine(segment, character, action, panelIndex));
                }
            }

            var triggers = BuildPrimaryTriggerSegments(action);
            if (triggers.Count > 0 && lines.Count < maxLines)
            {
                AddColoredBlank(lines);
                AddColoredLine(lines, ActionSectionHeader("Triggers"));
                foreach (string segment in triggers)
                {
                    if (lines.Count >= maxLines) break;
                    AddColoredLine(lines, PlainGrayLine(segment));
                }
            }

            if (!includeExtendedDetails)
            {
                var extended = BuildExtendedDetailSegments(action);
                if (extended.Count > 0 && lines.Count < maxLines)
                {
                    AddColoredBlank(lines);
                    AddColoredLine(lines, BuildActionAltHintLine());
                }
                return TrimColored(lines, maxLines);
            }

            var extendedLines = BuildExtendedDetailSegments(action);
            if (extendedLines.Count > 0 && lines.Count < maxLines)
            {
                AddColoredBlank(lines);
                AddColoredLine(lines, ActionSectionHeader("Details"));
                foreach (string segment in extendedLines)
                {
                    if (lines.Count >= maxLines) break;
                    AddColoredLine(lines, PlainGrayLine(segment));
                }
            }

            return TrimColored(lines, maxLines);
        }

        private static List<ColoredText> BuildActionTitleLine(string? name)
        {
            var b = new ColoredTextBuilder();
            b.Add(FormatTooltipActionName(name), Colors.White);
            return b.Build();
        }

        private static List<ColoredText> BuildActionRarityLine(string rarity)
        {
            var b = new ColoredTextBuilder();
            b.Add(rarity, ItemThemeProvider.GetRarityColor(rarity));
            return b.Build();
        }

        private static List<ColoredText> ActionSectionHeader(string title)
        {
            var b = new ColoredTextBuilder();
            b.Add(title, ColorPalette.Info.GetColor());
            return b.Build();
        }

        private static List<ColoredText> BuildActionAltHintLine()
        {
            var b = new ColoredTextBuilder();
            b.Add("Hold Alt for more", Colors.DarkGray);
            return b.Build();
        }

        private static List<ColoredText> PlainGrayLine(string text)
        {
            var b = new ColoredTextBuilder();
            b.Add(text ?? "", Colors.LightGray);
            return b.Build();
        }

        private static List<ColoredText> BuildActionBodyLine(
            string segment,
            Character? character,
            Action action,
            int panelIndex)
        {
            if (character != null)
            {
                foreach (var bonus in ActionCardExternalBonusCollector.BuildLines(character, action, panelIndex))
                {
                    if (string.Equals(bonus.Text, segment, StringComparison.Ordinal))
                    {
                        var tinted = new ColoredTextBuilder();
                        tinted.Add(
                            segment,
                            bonus.Beneficial ? ColorPalette.Success.GetColor() : ColorPalette.Error.GetColor());
                        return tinted.Build();
                    }
                }
            }

            var b = new ColoredTextBuilder();
            b.Add(segment ?? "", Colors.White);
            return b.Build();
        }

        private static void AddColoredLine(List<List<ColoredText>> lines, List<ColoredText> line)
        {
            if (lines == null || line == null)
                return;
            lines.Add(line);
        }

        private static void AddColoredBlank(List<List<ColoredText>> lines)
        {
            if (lines == null || lines.Count == 0)
                return;
            if (lines[lines.Count - 1].Count == 0)
                return;
            lines.Add(new List<ColoredText>());
        }

        private static List<List<ColoredText>> TrimColored(List<List<ColoredText>> lines, int maxLines)
        {
            if (lines.Count <= maxLines)
                return lines;
            return lines.GetRange(0, maxLines);
        }
    }
}
