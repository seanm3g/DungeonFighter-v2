using Avalonia.Media;
using RPGGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of dungeon-related screens (selection, entry, room entry, completion)
    /// </summary>
    public class DungeonRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private int currentLineCount;
        
        // Store dungeon name texts for animation
        private readonly List<ColoredText> dungeonNameTexts = new();
        private List<Dungeon>? lastDungeonList = null;
        private readonly BrightnessMask? sharedBrightnessMask;
        private static readonly Random random = new Random();
        
        public DungeonRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            this.currentLineCount = 0;
            
            // Initialize brightness mask from configuration
            var maskConfig = UIManager.UIConfig.BrightnessMask;
            if (maskConfig.Enabled)
            {
                sharedBrightnessMask = new BrightnessMask(maskConfig.Intensity, maskConfig.WaveLength);
            }
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
            dungeonNameTexts.Clear();
            currentLineCount = 0;
        }
        
        /// <summary>
        /// Updates the undulation animation for dungeon names
        /// Call this each frame to create a shimmering effect
        /// </summary>
        public void UpdateUndulation()
        {
            foreach (var text in dungeonNameTexts)
            {
                if (text.Undulate)
                {
                    text.AdvanceUndulation();
                }
            }
        }
        
        /// <summary>
        /// Updates the brightness mask animation (separate from undulation)
        /// </summary>
        public void UpdateBrightnessMask()
        {
            sharedBrightnessMask?.Advance();
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
            currentLineCount = 0;
            
            // Only recreate ColoredText objects if the dungeon list has changed
            bool dungeonListChanged = lastDungeonList == null || 
                                      lastDungeonList.Count != dungeons.Count ||
                                      !lastDungeonList.SequenceEqual(dungeons);
            
            if (dungeonListChanged)
            {
                dungeonNameTexts.Clear();
                lastDungeonList = new List<Dungeon>(dungeons);
            }
            
            // Available dungeons
            canvas.AddText(x + 2, y, "═══ AVAILABLE DUNGEONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            for (int i = 0; i < dungeons.Count; i++)
            {
                var dungeon = dungeons[i];
                var option = new ClickableElement
                {
                    X = x + 4,
                    Y = y,
                    Width = width - 8,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = (i + 1).ToString(),
                    DisplayText = $"[{i + 1}] {dungeon.Name} (lvl {dungeon.MinLevel})"
                };
                clickableElements.Add(option);
                
                // Create or reuse the dungeon name ColoredText
                ColoredText dungeonNameText;
                if (dungeonListChanged)
                {
                    dungeonNameText = ColoredText.FromTemplate(
                        dungeon.Name, 
                        GetDungeonThemeTemplate(dungeon.Theme), 
                        undulate: false
                    );
                    // Apply shared brightness mask for cloud-like lighting effect
                    dungeonNameText.BrightnessMask = sharedBrightnessMask;
                    // Set random line offset to make each line independent
                    dungeonNameText.BrightnessMaskLineOffset = random.Next(0, 1000);
                    dungeonNameTexts.Add(dungeonNameText);
                }
                else
                {
                    dungeonNameText = dungeonNameTexts[i];
                }
                
                // Build the dungeon display text using ColoredText pattern approach
                if (option.IsHovered)
                {
                    // When hovered, use yellow color for everything
                    var hoveredText = ColoredText.FromColor($"[{i + 1}] {dungeon.Name} (lvl {dungeon.MinLevel})", 'Y');
                    textWriter.WriteLineColored(hoveredText, x + 4, y);
                }
                else
                {
                    // Build using ColoredTextBuilder - keeps text and patterns separate
                    var builder = new ColoredTextBuilder()
                        .Add($"[{i + 1}] ", 'y')  // Grey bracket and number
                        .Add(dungeonNameText)  // Use the stored ColoredText (will animate!)
                        .Add($" (lvl {dungeon.MinLevel})", 'Y');  // White level info
                    
                    // Render segments directly - no parsing, no corruption
                    var segments = builder.Build();
                    textWriter.RenderSegments(segments, x + 4, y);
                }
                y++;
                currentLineCount++;
            }
            
            // Return option
            y += 2;
            currentLineCount += 2;
            canvas.AddText(x + 2, y, "═══ OPTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            var returnOption = new ClickableElement
            {
                X = x + 4,
                Y = y,
                Width = 25,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = "[0] Return to Menu"
            };
            clickableElements.Add(returnOption);
            
            canvas.AddMenuOption(x + 4, y, 0, "Return to Menu", AsciiArtAssets.Colors.White, returnOption.IsHovered);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders the dungeon start screen
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
        /// Renders the room entry screen
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
        /// Renders the room completion screen
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
        /// Renders the dungeon completion screen
        /// </summary>
        public void RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon)
        {
            currentLineCount = 0;
            int centerY = y + (height / 2) - 8;
            
            // Victory message
            canvas.AddText(x + (width / 2) - 15, centerY, "═══ VICTORY! ═══", AsciiArtAssets.Colors.Gold);
            centerY += 3;
            currentLineCount += 3;
            
            canvas.AddText(x + 4, centerY, "Congratulations! You have successfully completed the dungeon!", AsciiArtAssets.Colors.White);
            centerY += 2;
            currentLineCount += 2;
            canvas.AddText(x + 4, centerY, "You have gained experience and loot.", AsciiArtAssets.Colors.White);
            centerY += 3;
            currentLineCount += 3;
            
            // Use theme color for dungeon name
            var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
            canvas.AddText(x + 4, centerY, "Dungeon: ", AsciiArtAssets.Colors.White);
            canvas.AddText(x + 14, centerY, dungeon.Name, themeColor);
            centerY++;
            currentLineCount++;
            canvas.AddText(x + 4, centerY, $"Rooms Cleared: {dungeon.Rooms.Count}", AsciiArtAssets.Colors.White);
            centerY += 3;
            currentLineCount += 3;
            
            // Status message
            canvas.AddText(x + (width / 2) - 18, centerY, "Returning to the main menu...", AsciiArtAssets.Colors.White);
            currentLineCount++;
        }
        
        /// <summary>
        /// Maps dungeon themes to their corresponding color template names
        /// </summary>
        private string GetDungeonThemeTemplate(string theme)
        {
            // Convert theme name to lowercase for template lookup
            string templateName = theme.ToLower();
            
            // Check if the template exists, otherwise fall back to a generic one
            if (ColorTemplateLibrary.HasTemplate(templateName))
            {
                return templateName;
            }
            
            // Fallback for themes without specific templates
            return "y"; // Default to grey color code
        }
    }
}

