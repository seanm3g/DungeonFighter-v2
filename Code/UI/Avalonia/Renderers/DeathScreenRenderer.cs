using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Renders the death screen with run statistics.
    /// Extracted from DungeonRenderer to reduce size and improve Single Responsibility Principle compliance.
    /// </summary>
    public class DeathScreenRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        
        public DeathScreenRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }
        
        /// <summary>
        /// Renders the death screen with run statistics (condensed version)
        /// </summary>
        public void RenderDeathScreen(int x, int y, int width, int height, Character player, string defeatSummary)
        {
            int currentY = y + 1;
            int startX = x + 2;
            
            // ===== COMPACT HEADER =====
            string headerText = "═══ YOU DIED ═══";
            int headerX = x + (width / 2) - (headerText.Length / 2);
            canvas.AddText(headerX, currentY, headerText, AsciiArtAssets.Colors.Red);
            currentY += 2;
            
            // ===== PARSE AND DISPLAY STATISTICS (CONDENSED) =====
            string[] summaryLines = defeatSummary.Split('\n');
            Color sectionColor = AsciiArtAssets.Colors.White;
            bool inSection = false;
            bool skipNextEmpty = false;
            
            // Stats to skip (less relevant - only skip if zero or meaningless)
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
                    sectionColor = AsciiArtAssets.Colors.Cyan;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("COMBAT PERFORMANCE"))
                {
                    sectionColor = AsciiArtAssets.Colors.Red;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("DAMAGE RECORDS"))
                {
                    // Merge into combat section - skip header, just show stats
                    sectionColor = AsciiArtAssets.Colors.Orange;
                    inSection = true;
                    continue; // Skip the header, just show the stats
                }
                else if (trimmedLine.Contains("COMBO MASTERY"))
                {
                    sectionColor = AsciiArtAssets.Colors.Yellow;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("ITEM COLLECTION"))
                {
                    sectionColor = AsciiArtAssets.Colors.Magenta;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("EXPLORATION"))
                {
                    // Skip exploration if all zeros
                    inSection = false;
                    sectionColor = AsciiArtAssets.Colors.White;
                    continue;
                }
                else if (trimmedLine.Contains("ACHIEVEMENTS UNLOCKED"))
                {
                    sectionColor = AsciiArtAssets.Colors.Gold;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                
                // Render stat lines
                Color lineColor = AsciiArtAssets.Colors.White;
                if (trimmedLine.Contains("✓"))
                {
                    lineColor = AsciiArtAssets.Colors.Green;
                }
                else if (inSection && sectionColor != AsciiArtAssets.Colors.White)
                {
                    lineColor = AsciiArtAssets.Colors.White;
                }
                
                // Compact formatting - remove extra indentation
                string formattedLine = trimmedLine;
                if (formattedLine.StartsWith("   "))
                {
                    formattedLine = "    " + formattedLine.TrimStart();
                }
                
                canvas.AddText(startX, currentY, formattedLine, lineColor);
                currentY++;
            }
            
            currentY += 1;
            
            // ===== COMPACT FOOTER =====
            string footerText = "Better luck next time!";
            int footerX = x + (width / 2) - (footerText.Length / 2);
            canvas.AddText(footerX, currentY, footerText, AsciiArtAssets.Colors.Yellow);
            currentY += 2;
            
            // ===== CONTINUE BUTTON =====
            string continueText = UIConstants.MenuOptions.ReturnToMainMenu;
            string continueDisplayText = MenuOptionFormatter.Format(0, continueText);
            string buttonText = $"[ {continueDisplayText} ]";
            int buttonX = x + (width / 2) - (buttonText.Length / 2);
            
            var continueButton = new ClickableElement
            {
                X = buttonX,
                Y = currentY,
                Width = buttonText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "enter",
                DisplayText = continueDisplayText
            };
            
            clickableElements.Add(continueButton);
            canvas.AddText(buttonX, currentY, buttonText, AsciiArtAssets.Colors.Yellow);
            
            // Compact prompt
            currentY += 1;
            string promptText = "Press any key to continue...";
            int promptX = x + (width / 2) - (promptText.Length / 2);
            canvas.AddText(promptX, currentY, promptText, AsciiArtAssets.Colors.Gray);
        }
    }
}
