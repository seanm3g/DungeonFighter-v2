using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for dungeon selection screen
    /// Uses centralized animation state for brightness mask and undulation effects
    /// </summary>
    public class DungeonSelectionRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private readonly DungeonSelectionAnimationState animationState;
        
        // Cache template segments per dungeon (no animation state stored here)
        private readonly Dictionary<string, List<ColoredText>> dungeonTemplateCache = new();
        
        public DungeonSelectionRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            this.animationState = DungeonSelectionAnimationState.Instance;
        }
        
        /// <summary>
        /// Renders the dungeon selection screen
        /// Applies animation effects (brightness mask and undulation) during rendering using centralized state
        /// </summary>
        public int RenderDungeonSelection(int x, int y, int width, int height, List<Dungeon> dungeons)
        {
            int currentLineCount = 0;
            
            // Validate input
            if (dungeons == null)
            {
                throw new ArgumentNullException(nameof(dungeons), "Dungeon list cannot be null");
            }
            
            // Clear the dungeon list area before rendering to ensure animations are visible
            int dungeonListStartY = y + 2;
            int dungeonListEndY = dungeonListStartY + dungeons.Count;
            int dungeonListX = x + 4;
            int dungeonListWidth = width - 8;
            canvas.ClearTextInArea(dungeonListX, dungeonListStartY, dungeonListWidth, dungeonListEndY - dungeonListStartY + 1);
            
            // Available dungeons header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.AvailableDungeons), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            for (int i = 0; i < dungeons.Count; i++)
            {
                var dungeon = dungeons[i];
                
                if (dungeon == null)
                {
                    throw new InvalidOperationException($"Dungeon at index {i} is null");
                }
                
                string displayText = MenuOptionFormatter.FormatDungeon(i + 1, dungeon.Name, dungeon.MinLevel);
                var option = new ClickableElement
                {
                    X = x + 4,
                    Y = y,
                    Width = width - 8,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = (i + 1).ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                
                // Get or create template segments for this dungeon (cached, no animation state)
                List<ColoredText>? templateSegments;
                string cacheKey = $"{dungeon.Theme}_{dungeon.Name}";
                if (!dungeonTemplateCache.TryGetValue(cacheKey, out templateSegments) || templateSegments == null)
                {
                    templateSegments = ColoredText.FromTemplate(
                        GetDungeonThemeTemplate(dungeon.Theme),
                        dungeon.Name
                    );
                    if (templateSegments.Count == 0)
                    {
                        templateSegments = new List<ColoredText> { new ColoredText(dungeon.Name, Colors.White) };
                    }
                    dungeonTemplateCache[cacheKey] = templateSegments;
                }
                
                // Render the dungeon option
                if (option.IsHovered)
                {
                    // When hovered, use yellow color
                    var hoveredText = new ColoredText(displayText, ColorPalette.Yellow.GetColor());
                    textWriter.WriteLineColored(hoveredText, x + 4, y);
                }
                else
                {
                    // Build segments with animation effects applied during rendering
                    var segments = new List<ColoredText>();
                    
                    // Add bracket and number (no animation)
                    segments.Add(new ColoredText($"[{i + 1}] ", ColorPalette.Gray.GetColor()));
                    
                    // Generate a small random offset for this animated element based on line position
                    // This makes each animated element look different from others
                    int elementOffset = GetElementRandomOffset(y);
                    
                    // Dungeon names should always animate (undulation effect)
                    // This creates the shimmering/wave effect across the dungeon name text
                    bool shouldUndulate = true;
                    
                    // Apply animation effects to dungeon name character-by-character
                    int charPosition = $"[{i + 1}] ".Length;
                    foreach (var templateSegment in templateSegments)
                    {
                        foreach (char c in templateSegment.Text)
                        {
                            // Add element offset to position to make this element's animation unique
                            int adjustedPosition = charPosition + elementOffset;
                            
                            // Get brightness mask adjustment from centralized state
                            float brightnessAdjustment = animationState.GetBrightnessAt(adjustedPosition, y);
                            double brightnessFactor = 1.0 + (brightnessAdjustment / 100.0) * 2.0;
                            brightnessFactor = Math.Max(0.3, Math.Min(2.0, brightnessFactor));
                            
                            // Get position-based undulation brightness (creates sine wave across text)
                            // Only apply if the template has undulation enabled
                            if (shouldUndulate)
                            {
                                double undulationBrightness = animationState.GetUndulationBrightnessAt(adjustedPosition, y);
                                brightnessFactor += undulationBrightness * 3.0;
                                brightnessFactor = Math.Max(0.3, Math.Min(2.0, brightnessFactor));
                            }
                            
                            // Apply brightness adjustments to color
                            Color adjustedColor = AdjustColorBrightness(templateSegment.Color, brightnessFactor);
                            segments.Add(new ColoredText(c.ToString(), adjustedColor, templateSegment.SourceTemplate));
                            
                            charPosition++;
                        }
                    }
                    
                    // Add level info (no animation)
                    segments.Add(new ColoredText($" (lvl {dungeon.MinLevel})", ColorPalette.Gray.GetColor()));
                    
                    // Render all segments
                    textWriter.RenderSegments(segments, x + 4, y);
                }
                
                y++;
                currentLineCount++;
            }
            
            // Options section
            y += 1;
            currentLineCount += 1;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Options), AsciiArtAssets.Colors.Gold);
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
                DisplayText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.ReturnToMenu)
            };
            clickableElements.Add(returnOption);
            
            canvas.AddMenuOption(x + 4, y, 0, UIConstants.MenuOptions.ReturnToMenu, AsciiArtAssets.Colors.White, returnOption.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Adjusts the brightness of a color by a factor
        /// </summary>
        private Color AdjustColorBrightness(Color color, double factor)
        {
            factor = Math.Max(0.0, Math.Min(2.0, factor)); // Clamp between 0 and 2
            
            byte r = (byte)Math.Min(255, (int)(color.R * factor));
            byte g = (byte)Math.Min(255, (int)(color.G * factor));
            byte b = (byte)Math.Min(255, (int)(color.B * factor));
            
            return Color.FromRgb(r, g, b);
        }
        
        /// <summary>
        /// Generates a small random offset for an animated element based on its line position
        /// Uses a simple hash to ensure the same line always gets the same offset
        /// Returns a value between -50 and +50 to create visual variation
        /// </summary>
        private int GetElementRandomOffset(int lineOffset)
        {
            // Use a simple hash function to generate a consistent but varied offset
            // Multiply by a prime number and use modulo to get a pseudo-random value
            int hash = (lineOffset * 7919 + 12345) % 101; // Prime number 7919, offset 12345, modulo 101 gives -50 to +50 range
            return hash - 50; // Shift to -50 to +50 range
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
