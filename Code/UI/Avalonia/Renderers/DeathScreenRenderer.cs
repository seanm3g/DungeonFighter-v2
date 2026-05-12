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
        public const int FooterReservedRows = 5;
        private const int SummaryMetricLabelWidth = 28;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        
        public DeathScreenRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }
        
        /// <summary>
        /// Builds colored lines appended to the center display buffer so the death summary sits after combat log text.
        /// </summary>
        public static List<List<ColoredText>> BuildDeathSummaryLines(string defeatSummary, int summaryWidth = 0)
        {
            var lines = new List<List<ColoredText>>();

            AddCenteredLine(lines, new List<ColoredText> { new ColoredText("═══ YOU DIED ═══", AsciiArtAssets.Colors.Red) }, summaryWidth);
            lines.Add(new List<ColoredText>());

            string[] summaryLines = (defeatSummary ?? "").Split('\n');

            var statsToSkip = new HashSet<string>
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
            
            foreach (string line in summaryLines)
            {
                string trimmedLine = line.Trim();
                
                // Skip old header/footer lines
                if (trimmedLine == "═══════════════════════════════════════" || 
                    trimmedLine == "DEFEAT STATISTICS" ||
                    trimmedLine == "YOU DIED" ||
                    trimmedLine == "Better luck next time!")
                {
                    continue;
                }
                
                // Summary spacing is controlled here so the appended block stays compact.
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }
                
                // Skip less relevant stats
                bool shouldSkip = false;
                foreach (var skipPattern in statsToSkip)
                {
                    if (trimmedLine.Contains(skipPattern))
                    {
                        shouldSkip = true;
                        break;
                    }
                }
                if (shouldSkip) continue;
                
                // Detect and render section headers (compact, no separator lines)
                if (trimmedLine.Contains("CHARACTER PROGRESSION"))
                {
                    AddCenteredLine(lines, new List<ColoredText> { new ColoredText(trimmedLine, AsciiArtAssets.Colors.Cyan) }, summaryWidth);
                    continue;
                }
                else if (trimmedLine.Contains("COMBAT PERFORMANCE"))
                {
                    AddCenteredLine(lines, new List<ColoredText> { new ColoredText(trimmedLine, AsciiArtAssets.Colors.Red) }, summaryWidth);
                    continue;
                }
                else if (trimmedLine.Contains("DAMAGE RECORDS"))
                {
                    continue; // Skip the header, just show the stats
                }
                else if (trimmedLine.Contains("COMBO MASTERY"))
                {
                    AddCenteredLine(lines, new List<ColoredText> { new ColoredText(trimmedLine, AsciiArtAssets.Colors.Yellow) }, summaryWidth);
                    continue;
                }
                else if (trimmedLine.Contains("ITEM COLLECTION"))
                {
                    AddCenteredLine(lines, new List<ColoredText> { new ColoredText(trimmedLine, AsciiArtAssets.Colors.Magenta) }, summaryWidth);
                    continue;
                }
                else if (trimmedLine.Contains("EXPLORATION"))
                {
                    // Skip exploration if all zeros
                    continue;
                }
                else if (trimmedLine.Contains("ACHIEVEMENTS UNLOCKED"))
                {
                    AddCenteredLine(lines, new List<ColoredText> { new ColoredText(trimmedLine, AsciiArtAssets.Colors.Gold) }, summaryWidth);
                    continue;
                }
                
                // Render stat lines
                var lineColor = trimmedLine.Contains("✓", StringComparison.Ordinal) ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.White;
                AddCenteredLine(lines, BuildSummaryLine(trimmedLine, lineColor), summaryWidth);
            }

            lines.Add(new List<ColoredText>());
            AddCenteredLine(lines, new List<ColoredText> { new ColoredText("Better luck next time!", AsciiArtAssets.Colors.Yellow) }, summaryWidth);

            return lines;
        }

        private static List<ColoredText> BuildSummaryLine(string text, Color color)
        {
            int separatorIndex = text.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == text.Length - 1)
                return new List<ColoredText> { new ColoredText(text, color) };

            string label = text.Substring(0, separatorIndex + 1).PadRight(SummaryMetricLabelWidth);
            string value = text.Substring(separatorIndex + 1).TrimStart();
            return new List<ColoredText>
            {
                new ColoredText(label, color),
                new ColoredText(value, color)
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
        /// Draws the bottom footer (fixed; not part of the scrollable log).
        /// </summary>
        public void RenderFooterOnly(int x, int y, int width, int height)
        {
            int footerTextY = y + height - FooterReservedRows;
            int cloneButtonY = footerTextY + 1;
            int returnButtonY = footerTextY + 2;

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

            string promptText = "Press 1 to clone, or 0 to leave a tombstone.";
            int promptX = x + (width / 2) - (promptText.Length / 2);
            canvas.AddText(promptX, returnButtonY + 2, promptText, AsciiArtAssets.Colors.Gray);
        }
    }
}
