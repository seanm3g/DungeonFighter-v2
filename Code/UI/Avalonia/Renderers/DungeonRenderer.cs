using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Consolidated dungeon renderer that handles all dungeon-related screens
    /// Merged from multiple specialized renderers for better maintainability
    /// </summary>
    public class DungeonRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private int currentLineCount;
        
        // Specialized renderers (only keep complex ones)
        private readonly DungeonSelectionRenderer selectionRenderer;
        private readonly DungeonCompletionRenderer dungeonCompletionRenderer;
        
        public DungeonRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            this.currentLineCount = 0;
            
            // Initialize specialized renderers (only complex ones)
            this.selectionRenderer = new DungeonSelectionRenderer(canvas, textWriter, clickableElements);
            this.dungeonCompletionRenderer = new DungeonCompletionRenderer(canvas, clickableElements);
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            clickableElements.Clear();
            currentLineCount = 0;
        }
        
        /// <summary>
        /// Updates the undulation animation for dungeon names
        /// Call this each frame to create a shimmering effect
        /// </summary>
        public void UpdateUndulation()
        {
            selectionRenderer.UpdateUndulation();
        }
        
        /// <summary>
        /// Updates the brightness mask animation (separate from undulation)
        /// </summary>
        public void UpdateBrightnessMask()
        {
            selectionRenderer.UpdateBrightnessMask();
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        // IInteractiveRenderer implementation
        public List<ClickableElement> GetClickableElements()
        {
            return clickableElements;
        }
        
        public void UpdateHoverState(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = element.Contains(x, y);
            }
        }
        
        public bool HandleClick(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                if (element.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Renders the dungeon selection screen
        /// </summary>
        public void RenderDungeonSelection(int x, int y, int width, int height, List<Dungeon> dungeons)
        {
            currentLineCount = selectionRenderer.RenderDungeonSelection(x, y, width, height, dungeons);
        }
        
        /// <summary>
        /// Renders the dungeon start screen (merged from DungeonStartRenderer)
        /// </summary>
        public void RenderDungeonStart(int x, int y, int width, int height, Dungeon dungeon)
        {
            currentLineCount = 0;
            int centerY = y + (height / 2) - 8;
            
            // Dungeon info
            canvas.AddText(x + (width / 2) - 15, centerY, "═══ DUNGEON INFORMATION ═══", AsciiArtAssets.Colors.Gold);
            centerY += 3;
            currentLineCount += 3;
            
            // Use theme color for dungeon name
            var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
            canvas.AddText(x + 4, centerY, "Dungeon: ", AsciiArtAssets.Colors.White);
            canvas.AddText(x + 14, centerY, dungeon.Name, themeColor);
            centerY++;
            currentLineCount++;
            canvas.AddText(x + 4, centerY, $"Level Range: {dungeon.MinLevel} - {dungeon.MaxLevel}", AsciiArtAssets.Colors.White);
            centerY++;
            currentLineCount++;
            canvas.AddText(x + 4, centerY, $"Total Rooms: {dungeon.Rooms.Count}", AsciiArtAssets.Colors.White);
            centerY += 3;
            currentLineCount += 3;
            
            // Status messages
            canvas.AddText(x + (width / 2) - 15, centerY, "Preparing for adventure...", AsciiArtAssets.Colors.White);
            centerY += 2;
            currentLineCount += 2;
            canvas.AddText(x + (width / 2) - 20, centerY, "The dungeon awaits your challenge!", AsciiArtAssets.Colors.White);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders the room entry screen (merged from RoomEntryRenderer)
        /// </summary>
        public void RenderRoomEntry(int x, int y, int width, int height, Environment room)
        {
            currentLineCount = 0;
            
            // Room description
            canvas.AddText(x + 2, y, "═══ ROOM DESCRIPTION ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Split description into multiple lines if needed
            string[] descriptionLines = room.Description.Split('\n');
            foreach (var line in descriptionLines.Take(Math.Min(descriptionLines.Length, 15)))
            {
                // Word wrap long lines (use display length to handle color markup)
                if (ColorParser.GetDisplayLength(line) > width - 8)
                {
                    var wrappedLines = textWriter.WrapText(line, width - 8);
                    foreach (var wrappedLine in wrappedLines)
                    {
                        canvas.AddText(x + 4, y, wrappedLine, AsciiArtAssets.Colors.White);
                        y++;
                        currentLineCount++;
                    }
                }
                else
                {
                    canvas.AddText(x + 4, y, line, AsciiArtAssets.Colors.White);
                    y++;
                    currentLineCount++;
                }
            }
            
            y += 2;
            currentLineCount += 2;
            
            // Show room status and next action
            if (room.HasLivingEnemies())
            {
                canvas.AddText(x + 2, y, "═══ THREATS DETECTED ═══", AsciiArtAssets.Colors.Red);
                y += 2;
                currentLineCount += 2;
                canvas.AddText(x + 4, y, "Enemies detected in this room", AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
                canvas.AddText(x + 4, y, "Press any key to engage in combat...", AsciiArtAssets.Colors.Yellow);
                currentLineCount++;
            }
            else
            {
                canvas.AddText(x + 2, y, "═══ ROOM CLEAR ═══", AsciiArtAssets.Colors.Green);
                y += 2;
                currentLineCount += 2;
                canvas.AddText(x + 4, y, "No enemies remain in this room.", AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
                canvas.AddText(x + 4, y, "Press any key to continue to the next room...", AsciiArtAssets.Colors.Yellow);
                currentLineCount++;
            }
        }
        
        /// <summary>
        /// Renders the room completion screen (merged from RoomCompletionRenderer)
        /// </summary>
        public void RenderRoomCompletion(int x, int y, int width, int height, Environment room, Character currentCharacter)
        {
            currentLineCount = 0;
            
            // Match original console UI pattern for room completion
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
        /// Renders the dungeon completion screen with detailed statistics and menu choices
        /// </summary>
        public void RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon, Character player, int xpGained, Item? lootReceived)
        {
            currentLineCount = dungeonCompletionRenderer.RenderDungeonCompletion(x, y, width, height, dungeon, player, xpGained, lootReceived);
        }
    }
}
