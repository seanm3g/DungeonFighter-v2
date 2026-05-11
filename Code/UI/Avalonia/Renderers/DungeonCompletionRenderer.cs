using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Dungeon completion: summary lines for the scrollable combat log plus a fixed footer menu.
    /// </summary>
    public class DungeonCompletionRenderer
    {
        /// <summary>Prompt + gap + 3 menu rows (anchored to bottom of content rect).</summary>
        public const int FooterReservedRows = 5;
        private const int SummaryMetricLabelWidth = 24;

        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;

        public DungeonCompletionRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
        }

        /// <summary>
        /// Builds colored lines appended to the center display buffer so the victory summary sits after combat log text.
        /// </summary>
        public static List<List<ColoredText>> BuildCompletionSummaryLines(
            Dungeon dungeon,
            Character player,
            int xpGained,
            Item? lootReceived,
            List<LevelUpInfo> levelUpInfos,
            List<Item> itemsFoundDuringRun,
            int summaryWidth = 0)
        {
            var lines = new List<List<ColoredText>>();

            string victoryHeader = AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Victory);
            AddCenteredLine(lines, new List<ColoredText> { new ColoredText(victoryHeader, AsciiArtAssets.Colors.Gold) }, summaryWidth);
            lines.Add(new List<ColoredText>());

            const string congrats = "Congratulations! You have successfully completed the dungeon!";
            AddCenteredLine(lines, new List<ColoredText> { new ColoredText(congrats, AsciiArtAssets.Colors.Green) }, summaryWidth);

            lines.Add(new List<ColoredText>());
            AddCenteredLine(lines, new List<ColoredText>
            {
                new ColoredText(AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.DungeonStatistics), AsciiArtAssets.Colors.Green)
            }, summaryWidth);

            var sessionStats = player.SessionStats;
            AddCenteredLine(lines, BuildMetricLine("Rooms Cleared:", dungeon.Rooms.Count.ToString("N0")), summaryWidth);
            AddCenteredLine(lines, BuildMetricLine("Enemies Defeated:", sessionStats.EnemiesDefeated.ToString("N0")), summaryWidth);
            AddCenteredLine(lines, BuildMetricLine("Total Damage Dealt:", sessionStats.TotalDamageDealt.ToString("N0")), summaryWidth);
            AddCenteredLine(lines, BuildMetricLine("Total Damage Received:", sessionStats.TotalDamageReceived.ToString("N0")), summaryWidth);

            lines.Add(new List<ColoredText>());
            AddCenteredLine(lines, new List<ColoredText>
            {
                new ColoredText(AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.RewardsEarned), AsciiArtAssets.Colors.Yellow)
            }, summaryWidth);

            AddCenteredLine(lines, BuildMetricLine("Experience Gained:", $"{xpGained:N0} XP", AsciiArtAssets.Colors.Gold, AsciiArtAssets.Colors.White), summaryWidth);

            if (levelUpInfos != null && levelUpInfos.Count > 0)
            {
                lines.Add(new List<ColoredText>());
                foreach (var levelUpInfo in levelUpInfos)
                {
                    if (!levelUpInfo.IsValid) continue;
                    foreach (var line in LevelUpDisplayColoredText.BuildDisplayLines(levelUpInfo))
                        AddCenteredLine(lines, line, summaryWidth);
                    lines.Add(new List<ColoredText>());
                }
            }

            List<Item> allLoot = new List<Item>();
            if (itemsFoundDuringRun != null)
                allLoot.AddRange(itemsFoundDuringRun);
            if (lootReceived != null)
                allLoot.Add(lootReceived);

            if (allLoot.Count > 0)
            {
                AddCenteredLine(lines, new List<ColoredText> { new ColoredText("Loot Received:", AsciiArtAssets.Colors.Yellow) }, summaryWidth);
                foreach (var item in allLoot)
                    AddCenteredLine(lines, ItemDisplayColoredText.FormatLootForCompletion(item), summaryWidth);
            }
            else
            {
                AddCenteredLine(lines, new List<ColoredText> { new ColoredText("Loot Received: None", AsciiArtAssets.Colors.Gray) }, summaryWidth);
            }

            return lines;
        }

        private static List<ColoredText> BuildMetricLine(string label, string value)
        {
            return BuildMetricLine(label, value, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.White);
        }

        private static List<ColoredText> BuildMetricLine(string label, string value, Color labelColor, Color valueColor)
        {
            return new List<ColoredText>
            {
                new ColoredText(label.PadRight(SummaryMetricLabelWidth), labelColor),
                new ColoredText(value, valueColor)
            };
        }

        private static void AddCenteredLine(List<List<ColoredText>> lines, List<ColoredText> segments, int summaryWidth)
        {
            lines.Add(CenterLine(segments, summaryWidth));
        }

        private static List<ColoredText> CenterLine(List<ColoredText> segments, int summaryWidth)
        {
            var normalized = TrimLeadingWhitespace(segments);
            if (summaryWidth <= 0 || normalized.Count == 0)
                return normalized;

            int textLength = 0;
            foreach (var segment in normalized)
                textLength += segment.Text?.Length ?? 0;

            int leftPadding = (summaryWidth - textLength) / 2;
            if (leftPadding <= 0)
                return normalized;

            var centered = new List<ColoredText>
            {
                new ColoredText(new string(' ', leftPadding), AsciiArtAssets.Colors.White)
            };
            centered.AddRange(normalized);
            return centered;
        }

        private static List<ColoredText> TrimLeadingWhitespace(List<ColoredText> segments)
        {
            var normalized = new List<ColoredText>();
            bool trimming = true;

            foreach (var segment in segments)
            {
                string text = segment.Text ?? string.Empty;
                if (trimming)
                {
                    string trimmed = text.TrimStart();
                    if (trimmed.Length == 0)
                        continue;

                    text = trimmed;
                    trimming = false;
                }

                normalized.Add(new ColoredText(text, segment.Color, segment.SourceTemplate, segment.ColorReadyForCanvas));
            }

            return normalized;
        }

        /// <summary>
        /// Draws the bottom prompt and menu options (fixed; not part of the scrollable log).
        /// </summary>
        public void RenderFooterOnly(int x, int y, int width, int height)
        {
            int footerPromptY = y + height - FooterReservedRows;
            int menuStartY = footerPromptY + 2;

            int menuX = x + Math.Max(2, (width / 2) - 10);
            const int menuWidth = 20;

            // Prompt (plain white, centered above options)
            const string promptText = "What would you like to do?";
            int promptX = menuX + Math.Max(0, (menuWidth - promptText.Length) / 2);
            canvas.AddText(promptX, footerPromptY, promptText, AsciiArtAssets.Colors.White);

            var option1 = new ClickableElement
            {
                X = menuX,
                Y = menuStartY,
                Width = menuWidth,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.GoToDungeon)
            };

            var option2 = new ClickableElement
            {
                X = menuX,
                Y = menuStartY + 1,
                Width = menuWidth,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = MenuOptionFormatter.Format(2, UIConstants.MenuOptions.ShowInventory)
            };

            var option3 = new ClickableElement
            {
                X = menuX,
                Y = menuStartY + 2,
                Width = menuWidth,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.SaveAndExit)
            };

            clickableElements.AddRange(new[] { option1, option2, option3 });

            canvas.AddMenuOption(menuX, menuStartY, 1, UIConstants.MenuOptions.GoToDungeon, AsciiArtAssets.Colors.White, option1.IsHovered);
            canvas.AddMenuOption(menuX, menuStartY + 1, 2, UIConstants.MenuOptions.ShowInventory, AsciiArtAssets.Colors.White, option2.IsHovered);
            canvas.AddMenuOption(menuX, menuStartY + 2, 0, UIConstants.MenuOptions.SaveAndExit, AsciiArtAssets.Colors.White, option3.IsHovered);
        }
    }
}
