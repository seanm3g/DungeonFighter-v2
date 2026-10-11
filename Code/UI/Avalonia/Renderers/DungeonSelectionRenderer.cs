using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.TextAnimation;
using System;
using System.Collections.Generic;

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
        public int RenderDungeonSelection(
            int x,
            int y,
            int width,
            int height,
            List<Dungeon> dungeons,
            string? customDungeonLevelEntryBuffer = null,
            string? currentRegionDisplayName = null)
        {
            int currentLineCount = 0;
            
            // Validate input
            if (dungeons == null)
            {
                throw new ArgumentNullException(nameof(dungeons), "Dungeon list cannot be null");
            }

            int contentWidth = Math.Max(20, width - 4);
            int dungeonListX = x + 4;
            int dungeonListWidth = width - 8;
            
            // Available dungeons header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.AvailableDungeons), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;

            if (!string.IsNullOrWhiteSpace(currentRegionDisplayName))
            {
                string regionLine = "Region: " + currentRegionDisplayName.Trim();
                canvas.AddText(x + 2, y, Truncate(regionLine, contentWidth), AsciiArtAssets.Colors.Cyan);
                y++;
                currentLineCount++;
            }

            // Clear the dungeon option rows (after header and optional region line) before drawing the list
            int dungeonListStartY = y;
            int clearHeight = Math.Max(1, dungeons.Count);
            canvas.ClearTextInArea(dungeonListX, dungeonListStartY, dungeonListWidth, clearHeight);
            
            for (int i = 0; i < dungeons.Count; i++)
            {
                var dungeon = dungeons[i];
                
                if (dungeon == null)
                {
                    throw new InvalidOperationException($"Dungeon at index {i} is null");
                }
                
                string displayText = dungeon.Name == RPGGame.GameConstants.DungeonCustomLevelMenuName
                    ? MenuOptionFormatter.FormatDisplayText(i + 1, dungeon.Name, $" (levels {RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL}-{RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL})")
                    : MenuOptionFormatter.FormatDungeon(i + 1, dungeon.Name, dungeon.MinLevel);
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
                
                // Same segment layout idle and hovered — only color changes on hover.
                // A single yellow FormattedText string used a different glyph advance under
                // center-column zoom, so letter spacing jumped when the pointer entered the row.
                var segments = BuildDungeonOptionSegments(
                    optionNumber: i + 1,
                    dungeon: dungeon,
                    templateSegments: templateSegments,
                    lineY: y,
                    animationState: animationState,
                    customDungeonLevelEntryBuffer: customDungeonLevelEntryBuffer);

                if (option.IsHovered)
                    RecolorSegments(segments, ColorPalette.Yellow.GetColor());

                textWriter.RenderSegments(segments, x + 4, y);
                
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
        /// Builds the dungeon-row segments (prefix + undulating name + level suffix).
        /// Prefix/suffix are one glyph per segment so paint stays on the character grid —
        /// matching the animated name and avoiding hover/idle advance mismatches.
        /// </summary>
        internal static List<ColoredText> BuildDungeonOptionSegments(
            int optionNumber,
            Dungeon dungeon,
            List<ColoredText> templateSegments,
            int lineY,
            DungeonSelectionAnimationState animationState,
            string? customDungeonLevelEntryBuffer = null)
        {
            var segments = new List<ColoredText>();

            string prefix = $"[{optionNumber}] ";
            segments.AddRange(PerCharSegments(prefix, ColorPalette.Gray.GetColor()));

            int elementOffset = GetElementRandomOffset(lineY);
            var animatedName = TextAnimationCompositor.Compose(
                "dungeonSelection",
                baseSegments: templateSegments,
                charStartIndex: prefix.Length,
                lineOffset: lineY,
                animationState: animationState,
                elementOffset: elementOffset);
            segments.AddRange(animatedName);

            string levelSuffix = dungeon.Name == RPGGame.GameConstants.DungeonCustomLevelMenuName
                ? $" (levels {RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL}-{RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL})"
                : $" (lvl {dungeon.MinLevel})";
            segments.AddRange(PerCharSegments(levelSuffix, ColorPalette.Gray.GetColor()));

            if (dungeon.Name == RPGGame.GameConstants.DungeonCustomLevelMenuName && customDungeonLevelEntryBuffer != null)
            {
                segments.AddRange(PerCharSegments("  Level: ", ColorPalette.Gray.GetColor()));
                string lev = string.IsNullOrEmpty(customDungeonLevelEntryBuffer)
                    ? "_"
                    : customDungeonLevelEntryBuffer + "_";
                segments.AddRange(PerCharSegments(lev, ColorPalette.Yellow.GetColor()));
            }

            return segments;
        }

        /// <summary>One <see cref="ColoredText"/> per character so layout matches the undulating name.</summary>
        internal static List<ColoredText> PerCharSegments(string text, Color color)
        {
            var list = new List<ColoredText>(text.Length);
            foreach (char ch in text)
                list.Add(new ColoredText(ch.ToString(), color));
            return list;
        }

        /// <summary>Hover highlight: same glyphs/positions, yellow only.</summary>
        internal static void RecolorSegments(List<ColoredText> segments, Color color)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                segments[i] = new ColoredText(s.Text, color, s.SourceTemplate, colorReadyForCanvas: true);
            }
        }

        /// <summary>
        /// Concatenated display text for a segment list (idle and hover must match).
        /// </summary>
        internal static string JoinSegmentText(IReadOnlyList<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < segments.Count; i++)
                sb.Append(segments[i].Text);
            return sb.ToString();
        }

        /// <summary>
        /// Generates a small random offset for an animated element based on its line position
        /// Uses a simple hash to ensure the same line always gets the same offset
        /// Returns a value between -50 and +50 to create visual variation
        /// </summary>
        private static int GetElementRandomOffset(int lineOffset)
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

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            if (maxLength <= 3)
                return text.Substring(0, maxLength);
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
