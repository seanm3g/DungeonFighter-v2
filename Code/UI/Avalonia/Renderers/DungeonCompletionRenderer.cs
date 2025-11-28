using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for dungeon completion screen
    /// </summary>
    public class DungeonCompletionRenderer
    {
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
        /// Renders the dungeon completion screen with detailed statistics and menu choices
        /// </summary>
        public int RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon, Character player, int xpGained, Item? lootReceived)
        {
            int currentLineCount = 0;
            int startY = y + 2;
            int currentY = startY;
            
            // Victory message
            canvas.AddText(x + (width / 2) - 15, currentY, "═══ VICTORY! ═══", AsciiArtAssets.Colors.Gold);
            currentY += 3;
            currentLineCount += 3;
            
            canvas.AddText(x + 4, currentY, "Congratulations! You have successfully completed the dungeon!", AsciiArtAssets.Colors.White);
            currentY += 2;
            currentLineCount += 2;
            
            // Health restoration message
            int maxHealth = player.GetEffectiveMaxHealth();
            if (player.CurrentHealth == maxHealth)
            {
                canvas.AddText(x + 4, currentY, "Health Fully Restored", AsciiArtAssets.Colors.Green);
                currentY += 2;
                currentLineCount += 2;
            }
            
            // Use theme color for dungeon name - render as segments to ensure proper vertical alignment
            var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
            var segments = new List<ColoredText>
            {
                new ColoredText("Dungeon: ", AsciiArtAssets.Colors.White),
                new ColoredText(dungeon.Name, themeColor)
            };
            textWriter.RenderSegments(segments, x + 4, currentY);
            currentY += 2;
            currentLineCount += 2;
            
            // Dungeon Statistics Section
            canvas.AddText(x + 4, currentY, "═══ DUNGEON STATISTICS ═══", AsciiArtAssets.Colors.Green);
            currentY += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 6, currentY, $"Rooms Cleared: {dungeon.Rooms.Count}", AsciiArtAssets.Colors.White);
            currentY++;
            currentLineCount++;
            
            // Get session statistics for this dungeon
            var sessionStats = player.SessionStats;
            canvas.AddText(x + 6, currentY, $"Enemies Defeated: {sessionStats.EnemiesDefeated}", AsciiArtAssets.Colors.White);
            currentY++;
            currentLineCount++;
            
            canvas.AddText(x + 6, currentY, $"Total Damage Dealt: {sessionStats.TotalDamageDealt:N0}", AsciiArtAssets.Colors.White);
            currentY++;
            currentLineCount++;
            
            canvas.AddText(x + 6, currentY, $"Total Damage Received: {sessionStats.TotalDamageReceived:N0}", AsciiArtAssets.Colors.White);
            currentY++;
            currentLineCount++;
            
            // Rewards Section
            canvas.AddText(x + 4, currentY, "═══ REWARDS EARNED ═══", AsciiArtAssets.Colors.Yellow);
            currentY += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 6, currentY, $"Experience Gained: {xpGained:N0} XP", AsciiArtAssets.Colors.White);
            currentY += 2;
            currentLineCount += 2;
            
            if (lootReceived != null)
            {
                canvas.AddText(x + 6, currentY, "Loot Received:", AsciiArtAssets.Colors.White);
                currentY++;
                currentLineCount++;
                
                // Format item name with colored elements: [Rarity] ItemName (each element colored)
                var lootSegments = ItemDisplayColoredText.FormatLootForCompletion(lootReceived);
                textWriter.RenderSegments(lootSegments, x + 6, currentY);
                currentY++;
                currentLineCount++;
            }
            else
            {
                canvas.AddText(x + 6, currentY, "Loot Received: None", AsciiArtAssets.Colors.Gray);
                currentY++;
                currentLineCount++;
            }
            
            currentY += 2;
            currentLineCount += 2;
            
            // Menu choices
            canvas.AddText(x + 4, currentY, "═══ WHAT WOULD YOU LIKE TO DO? ═══", AsciiArtAssets.Colors.Gold);
            currentY += 3;
            currentLineCount += 3;
            
            // Menu options
            int menuX = x + (width / 2) - 10;
            
            var option1 = new ClickableElement
            {
                X = menuX,
                Y = currentY,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = "[1] Go to Dungeon"
            };
            
            var option2 = new ClickableElement
            {
                X = menuX,
                Y = currentY + 1,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = "[2] Show Inventory"
            };
            
            var option3 = new ClickableElement
            {
                X = menuX,
                Y = currentY + 2,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = "[0] Save & Exit"
            };
            
            clickableElements.AddRange(new[] { option1, option2, option3 });
            
            canvas.AddMenuOption(menuX, currentY, 1, "Go to Dungeon", AsciiArtAssets.Colors.White, option1.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, currentY + 1, 2, "Show Inventory", AsciiArtAssets.Colors.White, option2.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, currentY + 2, 0, "Save & Exit", AsciiArtAssets.Colors.White, option3.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Gets the color for item rarity display
        /// </summary>
        private Color GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "Common" => AsciiArtAssets.Colors.White,
                "Rare" => AsciiArtAssets.Colors.Blue,
                "Epic" => AsciiArtAssets.Colors.Purple,
                "Legendary" => AsciiArtAssets.Colors.Gold,
                _ => AsciiArtAssets.Colors.White
            };
        }
    }
}
