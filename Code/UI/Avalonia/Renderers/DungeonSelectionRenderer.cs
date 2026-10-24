using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.TextAnimation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for dungeon selection screen: the dungeon list on the left and, beside it, the
    /// travel map zoomed in on the current region with each dungeon marked as a numbered level tile.
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

        // Narrower screens keep the plain full-width list.
        private const int MinWidthForMap = 90;
        private static readonly Color MapEdge = Color.FromRgb(150, 118, 80);
        private static readonly Color MapSea = Color.FromRgb(12, 34, 40);

        private readonly TravelRegionCatalog regionCatalog = new();
        private readonly TravelMapPainter mapPainter = new();
        private TravelMapGeographyResult? mapGeography;
        private string mapKey = "";

        /// <summary>Dungeon (list number) under the pointer, on its map tile or its list row; -1 for none.</summary>
        private int hoveredDungeon = -1;

        private const string SiteTargetPrefix = "dungeon:";
        
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
            string? currentRegionDisplayName = null,
            string? currentRegionTheme = null)
        {
            int currentLineCount = 0;
            
            // Validate input
            if (dungeons == null)
            {
                throw new ArgumentNullException(nameof(dungeons), "Dungeon list cannot be null");
            }

            int mapLeft = x;
            bool showMap = width >= MinWidthForMap;
            hoveredDungeon = -1;
            if (showMap)
            {
                // The list keeps a column on the left; the region map takes the rest of the panel.
                int listWidth = Math.Clamp(width * 36 / 100, 36, 56);
                mapLeft = x + listWidth;
                RenderRegionMap(mapLeft, y, x + width - mapLeft - 1, height - 1, dungeons, currentRegionDisplayName);
                width = listWidth;
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
                const string regionPrefix = "Region: ";
                string regionName = Truncate(currentRegionDisplayName.Trim(), Math.Max(1, contentWidth - regionPrefix.Length));
                string? regionTemplate = DungeonThemeColors.GetRegionThemeTemplate(currentRegionTheme);
                canvas.ClearTextInArea(x + 2, y, contentWidth, 1);
                if (regionTemplate == null)
                {
                    canvas.AddText(x + 2, y, regionPrefix + regionName, AsciiArtAssets.Colors.Cyan);
                }
                else
                {
                    var regionSegments = PerCharSegments(regionPrefix, AsciiArtAssets.Colors.Cyan);
                    regionSegments.AddRange(TextAnimationCompositor.Compose(
                        "dungeonSelection",
                        baseSegments: ColoredText.FromTemplate(regionTemplate, regionName),
                        charStartIndex: regionPrefix.Length,
                        lineOffset: y,
                        animationState: animationState,
                        elementOffset: GetElementRandomOffset(y)));
                    textWriter.RenderSegments(regionSegments, x + 2, y);
                }
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
                    ? MenuOptionFormatter.FormatDisplayText(i + 1, dungeon.Name, $" (levels {RPGGame.GameConstants.MIN_DUNGEON_LEVEL}-{RPGGame.GameConstants.MAX_DUNGEON_LEVEL})")
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
                    templateSegments = DungeonThemeColors.BuildThemedName(dungeon.Name, dungeon.Theme);
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

                if (option.IsHovered || hoveredDungeon == i + 1)
                    RecolorSegments(segments, ColorPalette.Yellow.GetColor());
                if (showMap && dungeon.Name != RPGGame.GameConstants.DungeonCustomLevelMenuName)
                    TravelMapHoverState.AddTarget(option.X, option.Y, option.Width, 1, SiteTargetPrefix + (i + 1));

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
        /// The world map zoomed in on the current region, with the region's dungeons as numbered tiles on its land.
        /// The region arrives as its display label, so it is matched back to the catalog by that label.
        /// </summary>
        private void RenderRegionMap(int left, int top, int width, int height, List<Dungeon> dungeons, string? regionLabel)
        {
            if (width < 20 || height < 8)
                return;

            var regions = RegionTravelRenderer.OrderRegions(regionCatalog.GetAllRegions());
            var region = regions.FirstOrDefault(r => string.Equals(r.DisplayNameWithLevels.Trim(), regionLabel?.Trim(), StringComparison.Ordinal))
                ?? regions.FirstOrDefault(r => string.Equals(r.DisplayName.Trim(), regionLabel?.Trim(), StringComparison.Ordinal));
            if (region == null)
                return;

            TravelMapHoverState.BeginTargets();
            string? hovered = TravelMapHoverState.HoveredRegionId;
            if (hovered != null && hovered.StartsWith(SiteTargetPrefix, StringComparison.Ordinal)
                && int.TryParse(hovered.AsSpan(SiteTargetPrefix.Length), out int hoveredNumber))
                hoveredDungeon = hoveredNumber;

            // Dungeon markers follow list order; the custom-level entry is not a place on the map.
            var placed = dungeons
                .Select((d, i) => (Dungeon: d, Number: i + 1))
                .Where(e => e.Dungeon.Name != RPGGame.GameConstants.DungeonCustomLevelMenuName)
                .ToList();

            string key = $"{width}x{height}:{region.Id}:{placed.Count}:" + string.Join(";", regions.Select(r => $"{r.Id}@{r.MapX},{r.MapY}"));
            if (mapGeography == null || key != mapKey)
            {
                mapGeography = TravelMapGeography.BuildRegionView(width * 2, height * 2, regions, region.Id, placed.Count);
                mapKey = key;
            }

            // Sites are numbered 1..n in placement order; relabel them with each dungeon's list number.
            var sites = mapGeography.Sites
                .Select(s => s with { Number = s.Number - 1 < placed.Count ? placed[s.Number - 1].Number : s.Number })
                .ToList();

            canvas.AddBox(left, top, width, height, MapEdge, MapSea);
            var hoveredEntry = placed.FirstOrDefault(e => e.Number == hoveredDungeon);
            var frame = new TravelMapFrame
            {
                Pins = mapGeography.Pins,
                CurrentRegionId = region.Id,
                Sites = sites,
                HoveredSite = hoveredDungeon,
                HoveredSiteLabel = hoveredEntry.Dungeon != null ? $"{hoveredEntry.Dungeon.Name} (lvl {hoveredEntry.Dungeon.MinLevel})" : null
            };
            double scaling = global::Avalonia.Controls.TopLevel.GetTopLevel(canvas)?.RenderScaling ?? 1.0;
            double cellWidth = canvas.GetCharWidth();
            double cellHeight = canvas.GetCharHeight();
            var image = mapPainter.Paint(mapGeography, mapKey, width, height, cellWidth, cellHeight, scaling, (0, 0, 0, 0));
            canvas.AddImage(left, top, width, height, image, new global::Avalonia.Rect(image.Size), paddingPixels: 0, fillFrame: true,
                pixelAligned: true, overlay: mapPainter.Overlay(frame, width, height, cellWidth, cellHeight));

            // Each tile is clickable, the same as its row in the list, and hovering it names the dungeon.
            foreach (var site in sites)
            {
                var entry = placed.First(e => e.Number == site.Number);
                TravelMapHoverState.AddTarget(left + site.FineX / 2 - 1, top + site.FineY / 2, 3, 1, SiteTargetPrefix + site.Number);
                clickableElements.Add(new ClickableElement
                {
                    X = left + site.FineX / 2 - 1,
                    Y = top + site.FineY / 2,
                    Width = 3,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = site.Number.ToString(),
                    DisplayText = MenuOptionFormatter.FormatDungeon(site.Number, entry.Dungeon.Name, entry.Dungeon.MinLevel)
                });
            }

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
                ? $" (levels {RPGGame.GameConstants.MIN_DUNGEON_LEVEL}-{RPGGame.GameConstants.MAX_DUNGEON_LEVEL})"
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
