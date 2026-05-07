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
        /// <summary>Footer text + button + prompt (anchored to bottom of content rect).</summary>
        public const int FooterReservedRows = 4;

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
        public static List<List<ColoredText>> BuildDeathSummaryLines(string defeatSummary)
        {
            var lines = new List<List<ColoredText>>();

            lines.Add(new List<ColoredText> { new ColoredText("═══ YOU DIED ═══", AsciiArtAssets.Colors.Red) });
            lines.Add(new List<ColoredText>());

            string[] summaryLines = (defeatSummary ?? "").Split('\n');
            bool inSection = false;
            bool skipNextEmpty = false;

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
                
                // Skip empty lines (but allow one between sections)
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    if (inSection && !skipNextEmpty)
                    {
                        skipNextEmpty = true;
                        continue; // Skip this empty line
                    }
                    skipNextEmpty = false;
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
                
                skipNextEmpty = false;
                
                // Detect and render section headers (compact, no separator lines)
                if (trimmedLine.Contains("CHARACTER PROGRESSION"))
                {
                    inSection = true;
                    lines.Add(new List<ColoredText> { new ColoredText($"  {trimmedLine}", AsciiArtAssets.Colors.Cyan) });
                    continue;
                }
                else if (trimmedLine.Contains("COMBAT PERFORMANCE"))
                {
                    inSection = true;
                    lines.Add(new List<ColoredText> { new ColoredText($"  {trimmedLine}", AsciiArtAssets.Colors.Red) });
                    continue;
                }
                else if (trimmedLine.Contains("DAMAGE RECORDS"))
                {
                    inSection = true;
                    continue; // Skip the header, just show the stats
                }
                else if (trimmedLine.Contains("COMBO MASTERY"))
                {
                    inSection = true;
                    lines.Add(new List<ColoredText> { new ColoredText($"  {trimmedLine}", AsciiArtAssets.Colors.Yellow) });
                    continue;
                }
                else if (trimmedLine.Contains("ITEM COLLECTION"))
                {
                    inSection = true;
                    lines.Add(new List<ColoredText> { new ColoredText($"  {trimmedLine}", AsciiArtAssets.Colors.Magenta) });
                    continue;
                }
                else if (trimmedLine.Contains("EXPLORATION"))
                {
                    // Skip exploration if all zeros
                    inSection = false;
                    continue;
                }
                else if (trimmedLine.Contains("ACHIEVEMENTS UNLOCKED"))
                {
                    inSection = true;
                    lines.Add(new List<ColoredText> { new ColoredText($"  {trimmedLine}", AsciiArtAssets.Colors.Gold) });
                    continue;
                }
                
                // Render stat lines
                var lineColor = trimmedLine.Contains("✓", StringComparison.Ordinal) ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.White;
                
                // Compact formatting - remove extra indentation
                string formattedLine = trimmedLine;
                if (formattedLine.StartsWith("   "))
                {
                    formattedLine = "    " + formattedLine.TrimStart();
                }
                
                lines.Add(new List<ColoredText> { new ColoredText(formattedLine, lineColor) });
            }

            lines.Add(new List<ColoredText>());
            lines.Add(new List<ColoredText> { new ColoredText("Better luck next time!", AsciiArtAssets.Colors.Yellow) });

            return lines;
        }

        /// <summary>
        /// Draws the bottom footer (fixed; not part of the scrollable log).
        /// </summary>
        public void RenderFooterOnly(int x, int y, int width, int height)
        {
            int footerTextY = y + height - FooterReservedRows;
            int buttonY = footerTextY + 2;

            string footerText = "Better luck next time!";
            int footerX = x + (width / 2) - (footerText.Length / 2);
            canvas.AddText(footerX, footerTextY, footerText, AsciiArtAssets.Colors.Yellow);

            string continueText = UIConstants.MenuOptions.ReturnToMainMenu;
            string continueDisplayText = MenuOptionFormatter.Format(0, continueText);
            string buttonText = $"[ {continueDisplayText} ]";
            int buttonX = x + (width / 2) - (buttonText.Length / 2);

            var continueButton = new ClickableElement
            {
                X = buttonX,
                Y = buttonY,
                Width = buttonText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "enter",
                DisplayText = continueDisplayText
            };

            clickableElements.Add(continueButton);
            canvas.AddText(buttonX, buttonY, buttonText, AsciiArtAssets.Colors.Yellow);

            string promptText = "Press any key to continue...";
            int promptX = x + (width / 2) - (promptText.Length / 2);
            canvas.AddText(promptX, buttonY + 1, promptText, AsciiArtAssets.Colors.Gray);
        }
    }
}
