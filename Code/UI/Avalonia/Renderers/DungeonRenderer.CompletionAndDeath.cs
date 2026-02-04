using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Room completion, dungeon completion, and death screen rendering.
    /// </summary>
    public partial class DungeonRenderer
    {
        /// <summary>
        /// Renders the room completion screen.
        /// </summary>
        public void RenderRoomCompletion(int x, int y, int width, int height, Environment room, Character currentCharacter)
        {
            currentLineCount = 0;
            if (currentCharacter != null)
            {
                canvas.AddText(x + 2, y, string.Format(AsciiArtAssets.UIText.RemainingHealth,
                    currentCharacter.CurrentHealth, currentCharacter.GetEffectiveMaxHealth()),
                    AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
            }
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.RoomClearedMessage, AsciiArtAssets.Colors.Green);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.Divider, AsciiArtAssets.Colors.Green);
            currentLineCount++;
        }

        /// <summary>
        /// Renders the dungeon completion screen with detailed statistics and menu choices.
        /// </summary>
        public void RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, List<string>? dungeonContext = null)
        {
            currentLineCount = dungeonCompletionRenderer.RenderDungeonCompletion(x, y, width, height, dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun ?? new List<Item>(), dungeonContext);
        }

        /// <summary>
        /// Renders the death screen with run statistics (condensed version).
        /// </summary>
        public void RenderDeathScreen(int x, int y, int width, int height, Character player, string defeatSummary)
        {
            int currentY = y + 1;
            int startX = x + 2;

            string headerText = "═══ YOU DIED ═══";
            int headerX = x + (width / 2) - (headerText.Length / 2);
            canvas.AddText(headerX, currentY, headerText, AsciiArtAssets.Colors.Red);
            currentY += 2;

            string[] summaryLines = defeatSummary.Split('\n');
            Color sectionColor = AsciiArtAssets.Colors.White;
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

                if (trimmedLine == "═══════════════════════════════════════" ||
                    trimmedLine == "DEFEAT STATISTICS" ||
                    trimmedLine == "YOU DIED" ||
                    trimmedLine == "Better luck next time!")
                    continue;

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    if (inSection && !skipNextEmpty) { skipNextEmpty = true; continue; }
                    skipNextEmpty = false;
                    continue;
                }

                bool shouldSkip = false;
                foreach (var skipPattern in statsToSkip)
                {
                    if (trimmedLine.Contains(skipPattern)) { shouldSkip = true; break; }
                }
                if (shouldSkip) continue;
                skipNextEmpty = false;

                if (trimmedLine.Contains("CHARACTER PROGRESSION"))
                { sectionColor = AsciiArtAssets.Colors.Cyan; inSection = true; canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor); currentY++; continue; }
                if (trimmedLine.Contains("COMBAT PERFORMANCE"))
                { sectionColor = AsciiArtAssets.Colors.Red; inSection = true; canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor); currentY++; continue; }
                if (trimmedLine.Contains("DAMAGE RECORDS"))
                { sectionColor = AsciiArtAssets.Colors.Orange; inSection = true; continue; }
                if (trimmedLine.Contains("COMBO MASTERY"))
                { sectionColor = AsciiArtAssets.Colors.Yellow; inSection = true; canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor); currentY++; continue; }
                if (trimmedLine.Contains("ITEM COLLECTION"))
                { sectionColor = AsciiArtAssets.Colors.Magenta; inSection = true; canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor); currentY++; continue; }
                if (trimmedLine.Contains("EXPLORATION"))
                { inSection = false; sectionColor = AsciiArtAssets.Colors.White; continue; }
                if (trimmedLine.Contains("ACHIEVEMENTS UNLOCKED"))
                { sectionColor = AsciiArtAssets.Colors.Gold; inSection = true; canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor); currentY++; continue; }

                Color lineColor = trimmedLine.Contains("✓") ? AsciiArtAssets.Colors.Green : (inSection && sectionColor != AsciiArtAssets.Colors.White ? AsciiArtAssets.Colors.White : AsciiArtAssets.Colors.White);
                string formattedLine = trimmedLine.StartsWith("   ") ? "    " + trimmedLine.TrimStart() : trimmedLine;
                canvas.AddText(startX, currentY, formattedLine, lineColor);
                currentY++;
            }

            currentY += 1;
            string footerText = "Better luck next time!";
            int footerX = x + (width / 2) - (footerText.Length / 2);
            canvas.AddText(footerX, currentY, footerText, AsciiArtAssets.Colors.Yellow);
            currentY += 2;

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

            currentY += 1;
            string promptText = "Press any key to continue...";
            int promptX = x + (width / 2) - (promptText.Length / 2);
            canvas.AddText(promptX, currentY, promptText, AsciiArtAssets.Colors.Gray);
        }
    }
}
