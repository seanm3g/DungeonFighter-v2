using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Death screen: summary lines for the scrollable combat log plus a fixed footer menu.
    /// </summary>
    public class DeathScreenRenderer
    {
        /// <summary>Footer choice text anchored to the bottom of the content rect.</summary>
        public const int FooterReservedRows = 7;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        
        public DeathScreenRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }
        
        /// <summary>
        /// Builds colored lines appended to the center display buffer so the death summary sits after combat log text.
        /// Metric rows form one block centered in <paramref name="summaryWidth"/> with labels right-aligned on the colon.
        /// Alignment uses leading indent only: the log wrapper collapses runs of inner spaces to one.
        /// </summary>
        public static List<List<ColoredText>> BuildDeathSummaryLines(string defeatSummary, int summaryWidth = 0)
        {
            var rows = ParseSummaryRows(defeatSummary);

            int labelWidth = 0;
            int valueWidth = 0;
            foreach (var row in rows)
            {
                if (row.Kind != SummaryRowKind.Metric)
                    continue;
                labelWidth = Math.Max(labelWidth, row.Label.Length);
                valueWidth = Math.Max(valueWidth, row.Value.Length);
            }
            int blockLeft = Math.Max(0, (summaryWidth - (labelWidth + 1 + valueWidth)) / 2);

            var lines = new List<List<ColoredText>>();
            AddCenteredLine(lines, "═══ YOU DIED ═══", AsciiArtAssets.Colors.Red, summaryWidth);

            foreach (var row in rows)
            {
                if (row.Kind == SummaryRowKind.Header)
                {
                    lines.Add(new List<ColoredText>());
                    AddCenteredLine(lines, row.Label, row.Color, summaryWidth);
                }
                else if (row.Kind == SummaryRowKind.Metric)
                {
                    int indent = summaryWidth > 0 ? blockLeft + (labelWidth - row.Label.Length) : 0;
                    var line = new List<ColoredText>();
                    if (indent > 0)
                        line.Add(new ColoredText(new string(' ', indent), AsciiArtAssets.Colors.White));
                    line.Add(new ColoredText(row.Label + " ", row.Color));
                    line.Add(new ColoredText(row.Value, row.Color));
                    lines.Add(line);
                }
                else
                {
                    AddCenteredLine(lines, row.Label, row.Color, summaryWidth);
                }
            }

            lines.Add(new List<ColoredText>());
            AddCenteredLine(lines, "Better luck next time!", AsciiArtAssets.Colors.Yellow, summaryWidth);

            return lines;
        }

        private enum SummaryRowKind { Header, Metric, Text }

        private readonly record struct SummaryRow(SummaryRowKind Kind, string Label, string Value, Color Color);

        private static readonly (string Key, Color Color)[] SectionHeaders =
        {
            ("CHARACTER PROGRESSION", AsciiArtAssets.Colors.Cyan),
            ("COMBAT PERFORMANCE", AsciiArtAssets.Colors.Red),
            ("COMBO MASTERY", AsciiArtAssets.Colors.Yellow),
            ("ITEM COLLECTION", AsciiArtAssets.Colors.Magenta),
            ("ACHIEVEMENTS UNLOCKED", AsciiArtAssets.Colors.Gold),
        };

        // Headers dropped from the summary; their stats (if any survive the skip list) stay.
        private static readonly string[] HiddenHeaders = { "DAMAGE RECORDS", "EXPLORATION" };

        private static readonly string[] StatsToSkip =
        {
            "Total Healing Received: 0",
            "Combo Success Rate: ∞%",
            "Combo Success Rate: NaN%",
            "Rare Items: 0",
            "Epic Items: 0",
            "Legendary Items: 0",
            "Dungeons Completed: 0",
            "Rooms Explored: 0",
            "Encounters Survived: 0"
        };

        private static List<SummaryRow> ParseSummaryRows(string defeatSummary)
        {
            var rows = new List<SummaryRow>();
            foreach (string line in (defeatSummary ?? "").Split('\n'))
            {
                string trimmedLine = line.Trim();

                // Old header/footer lines and blank spacing; this renderer owns its own spacing.
                if (string.IsNullOrWhiteSpace(trimmedLine) ||
                    trimmedLine.Trim('═').Length == 0 ||
                    trimmedLine == "DEFEAT STATISTICS" ||
                    trimmedLine == "YOU DIED" ||
                    trimmedLine == "Better luck next time!")
                {
                    continue;
                }

                if (Array.Exists(StatsToSkip, skip => trimmedLine.Contains(skip, StringComparison.Ordinal)))
                    continue;
                if (Array.Exists(HiddenHeaders, h => trimmedLine.Contains(h, StringComparison.Ordinal)))
                    continue;

                int headerIndex = Array.FindIndex(SectionHeaders, h => trimmedLine.Contains(h.Key, StringComparison.Ordinal));
                if (headerIndex >= 0)
                {
                    // Drop the emoji prefix: wide glyphs overlap the header text on the cell grid.
                    rows.Add(new SummaryRow(SummaryRowKind.Header, StripSymbolPrefix(trimmedLine), "", SectionHeaders[headerIndex].Color));
                    continue;
                }

                if (trimmedLine.Contains("✓", StringComparison.Ordinal))
                {
                    rows.Add(new SummaryRow(SummaryRowKind.Text, trimmedLine, "", AsciiArtAssets.Colors.Green));
                    continue;
                }

                int separatorIndex = trimmedLine.IndexOf(':', StringComparison.Ordinal);
                if (separatorIndex <= 0 || separatorIndex == trimmedLine.Length - 1)
                {
                    rows.Add(new SummaryRow(SummaryRowKind.Text, trimmedLine, "", AsciiArtAssets.Colors.White));
                    continue;
                }

                rows.Add(new SummaryRow(
                    SummaryRowKind.Metric,
                    trimmedLine.Substring(0, separatorIndex + 1),
                    trimmedLine.Substring(separatorIndex + 1).Trim(),
                    AsciiArtAssets.Colors.White));
            }

            return rows;
        }

        private static string StripSymbolPrefix(string text)
        {
            int i = 0;
            while (i < text.Length && !char.IsLetterOrDigit(text[i]))
                i++;
            return i < text.Length ? text.Substring(i) : text;
        }

        private static void AddCenteredLine(List<List<ColoredText>> lines, string text, Color color, int summaryWidth)
        {
            int leftPadding = summaryWidth > 0 ? (summaryWidth - text.Length) / 2 : 0;
            var line = new List<ColoredText>();
            if (leftPadding > 0)
                line.Add(new ColoredText(new string(' ', leftPadding), AsciiArtAssets.Colors.White));
            line.Add(new ColoredText(text, color));
            lines.Add(line);
        }

        /// <summary>
        /// Draws the bottom footer (fixed; not part of the scrollable log).
        /// </summary>
        public void RenderFooterOnly(int x, int y, int width, int height)
        {
            int footerTextY = y + height - FooterReservedRows;
            int cloneButtonY = footerTextY + 1;
            int resurrectButtonY = footerTextY + 2;
            int returnButtonY = footerTextY + 3;

            string footerText = "Choose your fate:";
            int footerX = x + (width / 2) - (footerText.Length / 2);
            canvas.AddText(footerX, footerTextY, footerText, AsciiArtAssets.Colors.Yellow);

            string cloneText = "Clone this hero";
            string cloneDisplayText = MenuOptionFormatter.Format(1, cloneText);
            string cloneButtonText = $"[ {cloneDisplayText} ]";
            int cloneButtonX = x + (width / 2) - (cloneButtonText.Length / 2);

            var cloneButton = new ClickableElement
            {
                X = cloneButtonX,
                Y = cloneButtonY,
                Width = cloneButtonText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = cloneDisplayText
            };

            clickableElements.Add(cloneButton);
            canvas.AddText(cloneButtonX, cloneButtonY, cloneButtonText, AsciiArtAssets.Colors.Green);

            string resurrectText = "Resurrect (Dev — no penalty)";
            string resurrectDisplayText = MenuOptionFormatter.Format(2, resurrectText);
            string resurrectButtonText = $"[ {resurrectDisplayText} ]";
            int resurrectButtonX = x + (width / 2) - (resurrectButtonText.Length / 2);

            var resurrectButton = new ClickableElement
            {
                X = resurrectButtonX,
                Y = resurrectButtonY,
                Width = resurrectButtonText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = resurrectDisplayText
            };

            clickableElements.Add(resurrectButton);
            canvas.AddText(resurrectButtonX, resurrectButtonY, resurrectButtonText, AsciiArtAssets.Colors.Cyan);

            string continueText = UIConstants.MenuOptions.ReturnToMainMenu;
            string continueDisplayText = MenuOptionFormatter.Format(0, continueText);
            string returnButtonText = $"[ {continueDisplayText} ]";
            int returnButtonX = x + (width / 2) - (returnButtonText.Length / 2);

            var returnButton = new ClickableElement
            {
                X = returnButtonX,
                Y = returnButtonY,
                Width = returnButtonText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = continueDisplayText
            };

            clickableElements.Add(returnButton);
            canvas.AddText(returnButtonX, returnButtonY, returnButtonText, AsciiArtAssets.Colors.Yellow);

            string cloneNote = "Clone keeps progress and bag, but loses equipped gear.";
            int noteX = x + (width / 2) - (cloneNote.Length / 2);
            canvas.AddText(noteX, returnButtonY + 1, cloneNote, AsciiArtAssets.Colors.Gray);

            string promptText = "Press 1 to clone, 2 to resurrect [Dev], or 0 to leave a tombstone.";
            int promptX = x + (width / 2) - (promptText.Length / 2);
            canvas.AddText(promptX, returnButtonY + 2, promptText, AsciiArtAssets.Colors.Gray);
        }
    }
}
