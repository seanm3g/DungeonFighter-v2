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
            List<Item> itemsFoundDuringRun)
        {
            var lines = new List<List<ColoredText>>();

            string victoryHeader = AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Victory);
            lines.Add(new List<ColoredText> { new ColoredText(victoryHeader, AsciiArtAssets.Colors.Gold) });
            lines.Add(new List<ColoredText>());

            const string congrats = "Congratulations! You have successfully completed the dungeon!";
            lines.Add(new List<ColoredText> { new ColoredText(congrats, AsciiArtAssets.Colors.Green) });

            int maxHealth = player.GetEffectiveMaxHealth();
            if (player.CurrentHealth == maxHealth)
                lines.Add(new List<ColoredText> { new ColoredText("Health Fully Restored", AsciiArtAssets.Colors.Green) });

            var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
            lines.Add(new List<ColoredText>
            {
                new ColoredText("Dungeon: ", AsciiArtAssets.Colors.White),
                new ColoredText(dungeon.Name, themeColor)
            });

            lines.Add(new List<ColoredText>
            {
                new ColoredText(AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.DungeonStatistics), AsciiArtAssets.Colors.Green)
            });

            var sessionStats = player.SessionStats;
            lines.Add(new List<ColoredText> { new ColoredText($"  Rooms Cleared: {dungeon.Rooms.Count}", AsciiArtAssets.Colors.White) });
            lines.Add(new List<ColoredText> { new ColoredText($"  Enemies Defeated: {sessionStats.EnemiesDefeated}", AsciiArtAssets.Colors.White) });
            lines.Add(new List<ColoredText> { new ColoredText($"  Total Damage Dealt: {sessionStats.TotalDamageDealt:N0}", AsciiArtAssets.Colors.White) });
            lines.Add(new List<ColoredText> { new ColoredText($"  Total Damage Received: {sessionStats.TotalDamageReceived:N0}", AsciiArtAssets.Colors.White) });

            lines.Add(new List<ColoredText>
            {
                new ColoredText(AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.RewardsEarned), AsciiArtAssets.Colors.Yellow)
            });

            lines.Add(new List<ColoredText>
            {
                new ColoredText("Experience Gained: ", AsciiArtAssets.Colors.Gold),
                new ColoredText($"{xpGained:N0} XP", AsciiArtAssets.Colors.White)
            });

            if (levelUpInfos != null && levelUpInfos.Count > 0)
            {
                lines.Add(new List<ColoredText>());
                foreach (var levelUpInfo in levelUpInfos)
                {
                    if (!levelUpInfo.IsValid) continue;
                    foreach (var line in LevelUpDisplayColoredText.BuildDisplayLines(levelUpInfo))
                        lines.Add(line);
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
                lines.Add(new List<ColoredText> { new ColoredText("  Loot Received:", AsciiArtAssets.Colors.Yellow) });
                foreach (var item in allLoot)
                    lines.Add(ItemDisplayColoredText.FormatLootForCompletion(item));
            }
            else
            {
                lines.Add(new List<ColoredText> { new ColoredText("  Loot Received: None", AsciiArtAssets.Colors.Gray) });
            }

            return lines;
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
