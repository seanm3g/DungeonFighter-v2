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

        /// <summary>Blank rows below the last menu option so the footer is not flush with the panel bottom.</summary>

        public const int FooterBottomPaddingRows = 5;



        /// <summary>Gap + Continue button row + bottom padding (anchored to bottom of content rect).</summary>

        public const int FooterReservedRows = 1 + 1 + FooterBottomPaddingRows;

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

            AddLogLine(lines, BuildMetricLine("Enemies Defeated:", sessionStats.RunEnemiesDefeated.ToString("N0")));

            AddLogLine(lines, BuildMetricLine("Total Damage Dealt:", sessionStats.RunDamageDealt.ToString("N0")));

            AddLogLine(lines, BuildMetricLine("Total Damage Received:", sessionStats.RunDamageReceived.ToString("N0")));



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

            int menuStartY = footerPromptY + 1;



            // Match DisplayRenderer combat log inset: text occupies (contentX + 1) .. width-2.

            int textAreaLeft = x + 1;

            int textAreaWidth = System.Math.Max(1, width - 2);



            // Single Continue button: the inventory is the hub (Dungeon Select, Skill Tree, Travel, Main Menu).
            // Drawn as a boxed button (3 rows tall, centered on the menu row); key 1 still works.
            const string label = "CONTINUE";
            int buttonW = System.Math.Min(textAreaWidth, label.Length + 10);
            const int buttonH = 3;
            int buttonX = textAreaLeft + System.Math.Max(0, (textAreaWidth - buttonW) / 2);
            int buttonY = menuStartY - 1;
            var accent = ColorPaletteExtensions.GetColor(ColorPalette.Success);
            var fill = Color.FromRgb(236, 236, 236);
            canvas.AddBox(buttonX, buttonY, buttonW, buttonH, accent, fill, borderThicknessPixels: 3, aboveBackdrop: true);
            canvas.AddText(buttonX + System.Math.Max(1, (buttonW - label.Length) / 2), buttonY + buttonH / 2, label, Color.FromRgb(20, 20, 20));
            clickableElements.Add(new ClickableElement
            {
                X = buttonX,
                Y = buttonY,
                Width = buttonW,
                Height = buttonH,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = MenuOptionFormatter.Format(1, "Continue")
            });

        }

    }

}


