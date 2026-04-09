using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for dungeon completion screen
    /// </summary>
    public class DungeonCompletionRenderer
    {
        private const int TwoColumnMinWidth = 80;
        private const int ColumnGap = 2;
        /// <summary>Prompt + gap + 3 menu rows (anchored to bottom of content rect).</summary>
        private const int FooterReservedRows = 5;

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
        public int RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, List<string>? dungeonContext = null)
        {
            int currentLineCount = 0;
            int bodyMaxY = y + height - FooterReservedRows - 1;
            int startY = y + 1;
            int currentY = startY;

            string victoryHeader = AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Victory);
            int headerX = x + Math.Max(0, (width - victoryHeader.Length) / 2);
            canvas.AddText(headerX, currentY, victoryHeader, AsciiArtAssets.Colors.Gold);
            currentY += 2;
            currentLineCount += 2;

            const string congrats = "Congratulations! You have successfully completed the dungeon!";
            int fullWidth = Math.Max(8, width - 4);
            int lines = textWriter.WriteLineColoredWrapped(congrats, x + 2, currentY, fullWidth);
            currentY += lines;
            currentLineCount += lines;

            var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
            var dungeonNameSegments = new List<ColoredText>
            {
                new ColoredText("Dungeon: ", AsciiArtAssets.Colors.White),
                new ColoredText(dungeon.Name, themeColor)
            };

            List<Item> allLoot = new List<Item>();
            if (itemsFoundDuringRun != null)
                allLoot.AddRange(itemsFoundDuringRun);
            if (lootReceived != null)
                allLoot.Add(lootReceived);

            bool useTwoColumns = width >= TwoColumnMinWidth;
            if (useTwoColumns)
            {
                int innerPad = 2;
                int usable = width - innerPad * 2 - ColumnGap;
                int leftColW = Math.Max(12, usable / 2);
                int rightColW = Math.Max(12, usable - leftColW);
                int leftX = x + innerPad;
                int rightX = leftX + leftColW + ColumnGap;

                int splitY = currentY;
                int leftY = splitY;
                int rightY = splitY;

                RenderLeftColumnStats(leftX, leftY, leftColW, bodyMaxY, dungeon, player, xpGained, dungeonNameSegments, allLoot, ref currentLineCount);
                RenderRightColumnLevelUp(rightX, rightY, rightColW, bodyMaxY, levelUpInfos, ref currentLineCount);
            }
            else
            {
                int maxHealth = player.GetEffectiveMaxHealth();
                if (player.CurrentHealth == maxHealth)
                {
                    if (currentY <= bodyMaxY)
                    {
                        canvas.AddText(x + 2, currentY, "Health Fully Restored", AsciiArtAssets.Colors.Green);
                        currentY++;
                        currentLineCount++;
                    }
                }

                if (currentY <= bodyMaxY)
                {
                    int n = textWriter.WriteLineColoredWrapped(dungeonNameSegments, x + 2, currentY, fullWidth);
                    currentY += n;
                    currentLineCount += n;
                }

                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 2, currentY, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.DungeonStatistics), AsciiArtAssets.Colors.Green);
                    currentY++;
                    currentLineCount++;
                }

                var sessionStats = player.SessionStats;
                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 4, currentY, $"Rooms Cleared: {dungeon.Rooms.Count}", AsciiArtAssets.Colors.White);
                    currentY++;
                    currentLineCount++;
                }
                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 4, currentY, $"Enemies Defeated: {sessionStats.EnemiesDefeated}", AsciiArtAssets.Colors.White);
                    currentY++;
                    currentLineCount++;
                }
                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 4, currentY, $"Total Damage Dealt: {sessionStats.TotalDamageDealt:N0}", AsciiArtAssets.Colors.White);
                    currentY++;
                    currentLineCount++;
                }
                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 4, currentY, $"Total Damage Received: {sessionStats.TotalDamageReceived:N0}", AsciiArtAssets.Colors.White);
                    currentY++;
                    currentLineCount++;
                }

                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 2, currentY, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.RewardsEarned), AsciiArtAssets.Colors.Yellow);
                    currentY++;
                    currentLineCount++;
                }
                if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 4, currentY, $"Experience Gained: {xpGained:N0} XP", AsciiArtAssets.Colors.White);
                    currentY++;
                    currentLineCount++;
                }

                if (levelUpInfos != null && levelUpInfos.Count > 0)
                {
                    if (currentY <= bodyMaxY)
                    {
                        currentY++;
                        currentLineCount++;
                    }
                    foreach (var levelUpInfo in levelUpInfos)
                    {
                        if (!levelUpInfo.IsValid) continue;
                        foreach (var line in LevelUpDisplayColoredText.BuildDisplayLines(levelUpInfo))
                        {
                            if (currentY > bodyMaxY) break;
                            int m = textWriter.WriteLineColoredWrapped(line, x + 4, currentY, fullWidth - 2);
                            currentY += m;
                            currentLineCount += m;
                        }
                        if (currentY <= bodyMaxY) { currentY++; currentLineCount++; }
                    }
                }

                if (allLoot.Count > 0)
                {
                    if (currentY <= bodyMaxY)
                    {
                        canvas.AddText(x + 4, currentY, "Loot Received:", AsciiArtAssets.Colors.White);
                        currentY++;
                        currentLineCount++;
                    }
                    foreach (var item in allLoot)
                    {
                        if (currentY > bodyMaxY) break;
                        var lootSegments = ItemDisplayColoredText.FormatLootForCompletion(item);
                        int ln = textWriter.WriteLineColoredWrapped(lootSegments, x + 4, currentY, fullWidth - 2);
                        currentY += ln;
                        currentLineCount += ln;
                    }
                }
                else if (currentY <= bodyMaxY)
                {
                    canvas.AddText(x + 4, currentY, "Loot Received: None", AsciiArtAssets.Colors.Gray);
                    currentY++;
                    currentLineCount++;
                }
            }

            int footerPromptY = y + height - FooterReservedRows;
            int menuStartY = footerPromptY + 2;

            canvas.AddText(x + 2, footerPromptY, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.WhatWouldYouLikeToDo), AsciiArtAssets.Colors.Gold);
            currentLineCount += 1;

            int menuX = x + Math.Max(2, (width / 2) - 10);

            var option1 = new ClickableElement
            {
                X = menuX,
                Y = menuStartY,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.GoToDungeon)
            };

            var option2 = new ClickableElement
            {
                X = menuX,
                Y = menuStartY + 1,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = MenuOptionFormatter.Format(2, UIConstants.MenuOptions.ShowInventory)
            };

            var option3 = new ClickableElement
            {
                X = menuX,
                Y = menuStartY + 2,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.SaveAndExit)
            };

            clickableElements.AddRange(new[] { option1, option2, option3 });

            canvas.AddMenuOption(menuX, menuStartY, 1, UIConstants.MenuOptions.GoToDungeon, AsciiArtAssets.Colors.White, option1.IsHovered);
            canvas.AddMenuOption(menuX, menuStartY + 1, 2, UIConstants.MenuOptions.ShowInventory, AsciiArtAssets.Colors.White, option2.IsHovered);
            canvas.AddMenuOption(menuX, menuStartY + 2, 0, UIConstants.MenuOptions.SaveAndExit, AsciiArtAssets.Colors.White, option3.IsHovered);
            currentLineCount += 5;

            return currentLineCount;
        }

        private int RenderLeftColumnStats(int leftX, int leftY, int leftColW, int bodyMaxY, Dungeon dungeon, Character player, int xpGained, List<ColoredText> dungeonNameSegments, List<Item> allLoot, ref int currentLineCount)
        {
            int y = leftY;

            int maxHealth = player.GetEffectiveMaxHealth();
            if (player.CurrentHealth == maxHealth && y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, "Health Fully Restored", AsciiArtAssets.Colors.Green);
                y++;
                currentLineCount++;
            }

            if (y <= bodyMaxY)
            {
                int n = textWriter.WriteLineColoredWrapped(dungeonNameSegments, leftX, y, leftColW);
                y += n;
                currentLineCount += n;
            }

            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.DungeonStatistics), AsciiArtAssets.Colors.Green);
                y++;
                currentLineCount++;
            }

            var sessionStats = player.SessionStats;
            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, $"Rooms Cleared: {dungeon.Rooms.Count}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }
            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, $"Enemies Defeated: {sessionStats.EnemiesDefeated}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }
            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, $"Total Damage Dealt: {sessionStats.TotalDamageDealt:N0}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }
            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, $"Total Damage Received: {sessionStats.TotalDamageReceived:N0}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }

            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.RewardsEarned), AsciiArtAssets.Colors.Yellow);
                y++;
                currentLineCount++;
            }
            if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, $"Experience Gained: {xpGained:N0} XP", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }

            if (allLoot.Count > 0)
            {
                if (y <= bodyMaxY)
                {
                    canvas.AddText(leftX, y, "Loot Received:", AsciiArtAssets.Colors.White);
                    y++;
                    currentLineCount++;
                }
                foreach (var item in allLoot)
                {
                    if (y > bodyMaxY) break;
                    var lootSegments = ItemDisplayColoredText.FormatLootForCompletion(item);
                    int ln = textWriter.WriteLineColoredWrapped(lootSegments, leftX, y, leftColW);
                    y += ln;
                    currentLineCount += ln;
                }
            }
            else if (y <= bodyMaxY)
            {
                canvas.AddText(leftX, y, "Loot Received: None", AsciiArtAssets.Colors.Gray);
                y++;
                currentLineCount++;
            }

            return y;
        }

        private int RenderRightColumnLevelUp(int rightX, int rightY, int rightColW, int bodyMaxY, List<LevelUpInfo>? levelUpInfos, ref int currentLineCount)
        {
            int y = rightY;

            if (levelUpInfos != null && levelUpInfos.Count > 0)
            {
                if (y <= bodyMaxY)
                {
                    y++;
                    currentLineCount++;
                }

                foreach (var levelUpInfo in levelUpInfos)
                {
                    if (!levelUpInfo.IsValid) continue;
                    foreach (var line in LevelUpDisplayColoredText.BuildDisplayLines(levelUpInfo))
                    {
                        if (y > bodyMaxY) break;
                        int m = textWriter.WriteLineColoredWrapped(line, rightX, y, rightColW);
                        y += m;
                        currentLineCount += m;
                    }
                    if (y <= bodyMaxY)
                    {
                        y++;
                        currentLineCount++;
                    }
                }
            }

            return y;
        }
    }
}
