using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for dungeon selection screen
    /// </summary>
    public class DungeonSelectionRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        
        // Store dungeon name texts for animation
        private readonly List<ColoredText> dungeonNameTexts = new();
        private List<Dungeon>? lastDungeonList = null;
        private readonly BrightnessMask? sharedBrightnessMask;
        private static readonly Random random = new Random();
        
        public DungeonSelectionRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            
            // Initialize brightness mask from configuration
            var maskConfig = UIManager.UIConfig.BrightnessMask;
            if (maskConfig.Enabled)
            {
                sharedBrightnessMask = new BrightnessMask(maskConfig.Intensity, maskConfig.WaveLength);
            }
        }
        
        /// <summary>
        /// Updates the undulation animation for dungeon names
        /// </summary>
        public void UpdateUndulation()
        {
            foreach (var text in dungeonNameTexts)
            {
                if (text.IsUndulating)
                {
                    text.AdvanceUndulation();
                }
            }
        }
        
        /// <summary>
        /// Updates the brightness mask animation
        /// </summary>
        public void UpdateBrightnessMask()
        {
            sharedBrightnessMask?.Advance();
        }
        
        /// <summary>
        /// Renders the dungeon selection screen
        /// </summary>
        public int RenderDungeonSelection(int x, int y, int width, int height, List<Dungeon> dungeons)
        {
            int currentLineCount = 0;
            
            // Validate input - dungeons list must not be null
            if (dungeons == null)
            {
                throw new ArgumentNullException(nameof(dungeons), "Dungeon list cannot be null");
            }
            
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
                
                // Validate dungeon object is not null
                if (dungeon == null)
                {
                    throw new InvalidOperationException($"Dungeon at index {i} is null");
                }
                
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
                    var coloredTexts = ColoredText.FromTemplate(
                        GetDungeonThemeTemplate(dungeon.Theme),
                        dungeon.Name
                    );
                    // Use the first segment as the dungeon name text
                    dungeonNameText = coloredTexts.FirstOrDefault() ?? new ColoredText(dungeon.Name);
                    // Note: BrightnessMask feature requires API updates to work with BrightnessMask class
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
                    var hoveredText = new ColoredText($"[{i + 1}] {dungeon.Name} (lvl {dungeon.MinLevel})", ColorPalette.Yellow.GetColor());
                    textWriter.WriteLineColored(hoveredText, x + 4, y);
                }
                else
                {
                    // Build using ColoredTextBuilder - keeps text and patterns separate
                    var builder = new ColoredTextBuilder()
                        .Add($"[{i + 1}] ", ColorPalette.Gray)  // Grey bracket and number
                        .Add(dungeonNameText)  // Use the stored ColoredText (will animate!)
                        .Add($" (lvl {dungeon.MinLevel})", ColorPalette.White);  // White level info
                    
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
            
            return currentLineCount;
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
