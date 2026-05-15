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

        /// Rows use <see cref="UIMessageType.OutcomeSummary"/> so the center panel center-justifies them within the log column.

        /// </summary>

        public static List<List<ColoredText>> BuildCompletionSummaryLines(

            Dungeon dungeon,

            Character player,

            int xpGained,

            Item? lootReceived,

            List<LevelUpInfo> levelUpInfos,

            List<Item> itemsFoundDuringRun)

        {

            var lines = new List<List<ColoredText>>();



            string victoryHeader = AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Victory);

            AddLogLine(lines, new List<ColoredText> { new ColoredText(victoryHeader, AsciiArtAssets.Colors.Gold) });

            lines.Add(new List<ColoredText>());



            const string congrats = "Congratulations! You have successfully completed the dungeon!";

            AddLogLine(lines, new List<ColoredText> { new ColoredText(congrats, AsciiArtAssets.Colors.Green) });



            lines.Add(new List<ColoredText>());

            AddLogLine(lines, new List<ColoredText>

            {

                new ColoredText(AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.DungeonStatistics), AsciiArtAssets.Colors.Green)

            });



            var sessionStats = player.SessionStats;

            AddLogLine(lines, BuildMetricLine("Rooms Cleared:", dungeon.Rooms.Count.ToString("N0")));

            AddLogLine(lines, BuildMetricLine("Enemies Defeated:", sessionStats.EnemiesDefeated.ToString("N0")));

            AddLogLine(lines, BuildMetricLine("Total Damage Dealt:", sessionStats.TotalDamageDealt.ToString("N0")));

            AddLogLine(lines, BuildMetricLine("Total Damage Received:", sessionStats.TotalDamageReceived.ToString("N0")));



            lines.Add(new List<ColoredText>());

            AddLogLine(lines, new List<ColoredText>

            {

                new ColoredText(AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.RewardsEarned), AsciiArtAssets.Colors.Yellow)

            });



            AddLogLine(lines, BuildMetricLine("Experience Gained:", $"{xpGained:N0} XP", AsciiArtAssets.Colors.Gold, AsciiArtAssets.Colors.White));



            if (levelUpInfos != null && levelUpInfos.Count > 0)

            {

                lines.Add(new List<ColoredText>());

                foreach (var levelUpInfo in levelUpInfos)

                {

                    if (!levelUpInfo.IsValid) continue;

                    foreach (var line in LevelUpDisplayColoredText.BuildDisplayLines(levelUpInfo))

                        AddLogLine(lines, line);

                    lines.Add(new List<ColoredText>());

                }

            }



            List<Item> allLoot = new List<Item>();

            if (itemsFoundDuringRun != null)

                allLoot.AddRange(itemsFoundDuringRun);

            if (lootReceived != null)

                allLoot.Add(lootReceived);



            if (lines.Count == 0 || lines[^1].Count > 0)

                lines.Add(new List<ColoredText>());



            if (allLoot.Count > 0)

            {

                AddLogLine(lines, new List<ColoredText> { new ColoredText("Loot Received:", AsciiArtAssets.Colors.Yellow) });

                foreach (var item in allLoot)

                    AddLogLine(lines, ItemDisplayColoredText.FormatLootForCompletion(item));

            }

            else

            {

                AddLogLine(lines, new List<ColoredText> { new ColoredText("Loot Received: None", AsciiArtAssets.Colors.Gray) });

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



        private static void AddLogLine(List<List<ColoredText>> lines, List<ColoredText> segments)

        {

            lines.Add(TrimLeadingWhitespace(segments));

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



            // Match DisplayRenderer combat log inset: text occupies (contentX + 1) .. width-2.

            int textAreaLeft = x + 1;

            int textAreaWidth = System.Math.Max(1, width - 2);



            const string promptText = "What would you like to do?";

            int promptX = textAreaLeft + System.Math.Max(0, (textAreaWidth - promptText.Length) / 2);

            canvas.AddText(promptX, footerPromptY, promptText, AsciiArtAssets.Colors.White);



            string optLine1 = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.GoToDungeon);

            string optLine2 = MenuOptionFormatter.Format(2, UIConstants.MenuOptions.ShowInventory);

            string optLine3 = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.SaveAndExit);

            int row1X = textAreaLeft + System.Math.Max(0, (textAreaWidth - optLine1.Length) / 2);

            int row2X = textAreaLeft + System.Math.Max(0, (textAreaWidth - optLine2.Length) / 2);

            int row3X = textAreaLeft + System.Math.Max(0, (textAreaWidth - optLine3.Length) / 2);



            var option1 = new ClickableElement

            {

                X = row1X,

                Y = menuStartY,

                Width = optLine1.Length,

                Height = 1,

                Type = ElementType.MenuOption,

                Value = "1",

                DisplayText = optLine1

            };



            var option2 = new ClickableElement

            {

                X = row2X,

                Y = menuStartY + 1,

                Width = optLine2.Length,

                Height = 1,

                Type = ElementType.MenuOption,

                Value = "2",

                DisplayText = optLine2

            };



            var option3 = new ClickableElement

            {

                X = row3X,

                Y = menuStartY + 2,

                Width = optLine3.Length,

                Height = 1,

                Type = ElementType.MenuOption,

                Value = "0",

                DisplayText = optLine3

            };



            clickableElements.AddRange(new[] { option1, option2, option3 });



            canvas.AddMenuOption(row1X, menuStartY, 1, UIConstants.MenuOptions.GoToDungeon, AsciiArtAssets.Colors.White, option1.IsHovered);

            canvas.AddMenuOption(row2X, menuStartY + 1, 2, UIConstants.MenuOptions.ShowInventory, AsciiArtAssets.Colors.White, option2.IsHovered);

            canvas.AddMenuOption(row3X, menuStartY + 2, 0, UIConstants.MenuOptions.SaveAndExit, AsciiArtAssets.Colors.White, option3.IsHovered);

        }

    }

}


