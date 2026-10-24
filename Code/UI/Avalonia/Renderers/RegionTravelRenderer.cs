using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Region travel as an ASCII world map laid on a wooden table: biome islands packed with small terrain glyphs,
    /// regions as plain dots (no numbers or roads on the map). Hover a dot for its details on the slip, click to travel;
    /// the slip keeps the numbered key and logs the journey while an @ crosses to the destination.
    /// </summary>
    public class RegionTravelRenderer
    {
        private static readonly Color TableWood = Color.FromRgb(62, 38, 20);
        private static readonly Color TableEdge = Color.FromRgb(34, 20, 10);
        private static readonly Color TableGrain = Color.FromRgb(92, 60, 34);
        private static readonly Color MapSea = Color.FromRgb(12, 34, 40);
        private static readonly Color MapEdge = Color.FromRgb(150, 118, 80);
        private static readonly Color LogBackground = Color.FromRgb(16, 13, 10);
        private static readonly Color LogBorder = Color.FromRgb(150, 112, 60);

        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private readonly TravelRegionCatalog regionCatalog = new();
        private readonly TravelMapPainter painter = new();

        private TravelMapGeographyResult? geography;
        private string geographyKey = "";

        public RegionTravelRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
            this.clickableElements = clickableElements;
        }

        /// <summary>All regions in level order; pin numbers and travel input both follow this order.</summary>
        public static IReadOnlyList<TravelRegion> OrderRegions(IEnumerable<TravelRegion> regions) =>
            regions.OrderBy(r => r.HasLevelRange ? r.MinLevel : int.MaxValue).ToList();

        public int RenderRegionTravel(
            int x,
            int y,
            int width,
            int height,
            Character player,
            IReadOnlyList<TravelRegion> destinations,
            TravelRouteResult? routeResult)
        {
            TravelMapHoverState.BeginTargets();

            canvas.AddBox(x, y, width, height, TableEdge, TableWood);
            DrawTableTrim(x, y, width, height);

            int mapLeft = x + 2;
            int mapTop = y + 1;
            int mapWidth = Math.Max(30, width - 4);
            int mapHeight = Math.Max(12, height - 2);
            canvas.AddBox(mapLeft, mapTop, mapWidth, mapHeight, MapEdge, MapSea);

            // The log slip lies on the right of the map; the painted map leaves a hole for it.
            int slipWidth = Math.Clamp(mapWidth * 34 / 100, 32, 52);
            int slipX = mapLeft + mapWidth - slipWidth - 2;
            int slipY = mapTop + 2;
            int slipHeight = Math.Max(8, mapHeight - 4);
            canvas.AddBox(slipX, slipY, slipWidth, slipHeight, LogBorder, LogBackground);

            var regions = OrderRegions(regionCatalog.GetAllRegions());
            var currentRegion = regionCatalog.GetRegionForCharacter(player);
            int landFineWidth = (slipX - mapLeft - 1) * 2;
            string key = $"{mapWidth}x{mapHeight}:{landFineWidth}:" + string.Join(";", regions.Select(r => $"{r.Id}@{r.MapX},{r.MapY}"));
            if (geography == null || key != geographyKey)
            {
                geography = TravelMapGeography.Build(mapWidth * 2, mapHeight * 2, landFineWidth, regions);
                geographyKey = key;
            }

            string? hovered = TravelMapHoverState.HoveredRegionId;
            var pins = geography.Pins;
            TravelMapPin? PinFor(string? id) => pins.FirstOrDefault(p => string.Equals(p.Region.Id, id, StringComparison.OrdinalIgnoreCase)) is var pin && pin.Region != null ? pin : null;

            var frame = new TravelMapFrame
            {
                Pins = pins,
                CurrentRegionId = currentRegion.Id,
                HoveredRegionId = hovered,
                RouteFrom = routeResult != null ? PinFor(routeResult.FromRegion.Id) : null,
                RouteTo = routeResult != null ? PinFor(routeResult.ToRegion.Id) : null,
                WalkProgress = routeResult?.WalkProgress ?? 0,
                RouteComplete = routeResult?.IsComplete ?? false
            };

            double scaling = TopLevel.GetTopLevel(canvas)?.RenderScaling ?? 1.0;
            double cellWidth = canvas.GetCharWidth();
            double cellHeight = canvas.GetCharHeight();
            var image = painter.Paint(geography, geographyKey, mapWidth, mapHeight, cellWidth, cellHeight, scaling,
                (slipX - mapLeft, slipY - mapTop, slipWidth, slipHeight));
            canvas.AddImage(mapLeft, mapTop, mapWidth, mapHeight, image, new global::Avalonia.Rect(image.Size), paddingPixels: 0, fillFrame: true,
                pixelAligned: true, overlay: painter.Overlay(frame, mapWidth, mapHeight, cellWidth, cellHeight));

            string title = " ~ THE KNOWN REALMS ~ ";
            canvas.AddText(mapLeft + (mapWidth - title.Length) / 2, y, title, MapEdge);

            AddPinTargets(mapLeft, mapTop, pins, currentRegion);
            RenderSlip(slipX + 2, slipY + 1, slipWidth - 4, slipHeight - 2, player, regions, currentRegion, hovered, routeResult);
            return height;
        }

        private void DrawTableTrim(int x, int y, int width, int height)
        {
            for (int row = y + 1; row < y + height - 1; row++)
            {
                if (row % 3 == 0)
                {
                    canvas.AddText(x, row, "=", TableGrain);
                    canvas.AddText(x + width - 1, row, "=", TableGrain);
                }
            }
            string hint = " hover a dot for details, click to travel  -  [0] back ";
            if (hint.Length < width - 4)
                canvas.AddText(x + (width - hint.Length) / 2, y + height - 1, hint, MapEdge);
        }

        /// <summary>The painter draws the dots; this registers a hit area a little wider than each one.</summary>
        private void AddPinTargets(int mapLeft, int mapTop, IReadOnlyList<TravelMapPin> pins, TravelRegion currentRegion)
        {
            foreach (var pin in pins)
            {
                int gx = mapLeft + pin.FineX / 2;
                int gy = mapTop + pin.FineY / 2;
                bool isCurrent = string.Equals(pin.Region.Id, currentRegion.Id, StringComparison.OrdinalIgnoreCase);
                AddRegionTarget(gx - 1, gy, 3, pin, isCurrent);
            }
        }

        private void AddRegionTarget(int x, int y, int width, TravelMapPin pin, bool isCurrent)
        {
            TravelMapHoverState.AddTarget(x, y, width, 1, pin.Region.Id);
            if (isCurrent)
                return;
            clickableElements.Add(new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = pin.Number.ToString(CultureInfo.InvariantCulture),
                DisplayText = MenuOptionFormatter.Format(pin.Number, pin.Region.DisplayNameWithLevels)
            });
        }

        private void RenderSlip(
            int x,
            int y,
            int width,
            int height,
            Character player,
            IReadOnlyList<TravelRegion> regions,
            TravelRegion currentRegion,
            string? hovered,
            TravelRouteResult? route)
        {
            int returnY = y + height - 1;
            var returnOption = new ClickableElement
            {
                X = x,
                Y = returnY,
                Width = width,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.ReturnToGameMenu)
            };
            clickableElements.Add(returnOption);
            canvas.AddMenuOption(x, returnY, 0, UIConstants.MenuOptions.ReturnToGameMenu, AsciiArtAssets.Colors.White, returnOption.IsHovered);

            int bottom = returnY - 1;
            if (route != null && !route.IsComplete)
            {
                canvas.AddText(x, y, "TRAVEL LOG", AsciiArtAssets.Colors.Gold);
                WriteBottomAligned(BuildRouteLines(width, route), x, y + 2, bottom);
                return;
            }

            canvas.AddText(x, y, "MAP KEY", AsciiArtAssets.Colors.Gold);
            int rowY = y + 2;
            if (route != null)
            {
                foreach (var line in BuildArrivalLines(width, route))
                {
                    textWriter.RenderSegments(line, x, rowY);
                    rowY++;
                }
                rowY++;
            }

            var focus = regions.FirstOrDefault(r => string.Equals(r.Id, hovered, StringComparison.OrdinalIgnoreCase)) ?? currentRegion;
            var details = BuildRegionDetails(width, player, focus, focus.Id == currentRegion.Id);
            int keyRowsAvailable = Math.Max(0, bottom - rowY - details.Count - 1);
            int firstRow = 0;
            if (regions.Count > keyRowsAvailable)
            {
                // Keep the hovered (or current) row in view when the key has to scroll.
                int focusIndex = IndexOf(regions, focus);
                firstRow = Math.Clamp(focusIndex - keyRowsAvailable / 2, 0, regions.Count - keyRowsAvailable);
            }

            for (int i = firstRow; i < regions.Count && i < firstRow + keyRowsAvailable; i++)
            {
                RenderKeyRow(x, rowY, width, player, regions[i], i + 1, currentRegion, hovered);
                rowY++;
            }

            rowY = Math.Max(rowY + 1, bottom - details.Count + 1);
            foreach (var line in details)
            {
                if (rowY > bottom)
                    break;
                if (line.Count > 0)
                    textWriter.RenderSegments(line, x, rowY);
                rowY++;
            }
        }

        private void RenderKeyRow(int x, int y, int width, Character player, TravelRegion region, int number, TravelRegion currentRegion, string? hovered)
        {
            bool isCurrent = string.Equals(region.Id, currentRegion.Id, StringComparison.OrdinalIgnoreCase);
            bool isHovered = string.Equals(region.Id, hovered, StringComparison.OrdinalIgnoreCase);
            bool inBand = region.ContainsLevel(player.Level);

            string num = number.ToString(CultureInfo.InvariantCulture).PadLeft(2);
            string band = region.LevelRangeLabel;
            int nameWidth = Math.Max(4, width - 4 - band.Length - 1);
            string name = region.DisplayName.Length > nameWidth ? region.DisplayName.Substring(0, nameWidth - 1) + "." : region.DisplayName;

            var nameColor = isCurrent ? ColorPalette.Gold : isHovered ? ColorPalette.White : inBand ? ColorPalette.Green : ColorPalette.Gray;
            var row = new ColoredTextBuilder()
                .Add(isCurrent ? "@" : isHovered ? ">" : " ", isCurrent ? ColorPalette.Gold : ColorPalette.Info)
                .Add(num + " ", isHovered ? ColorPalette.White : ColorPalette.DarkGray)
                .Add(name.PadRight(nameWidth + 1), nameColor)
                .Add(band, inBand ? ColorPalette.Green : ColorPalette.DarkGray)
                .Build();
            textWriter.RenderSegments(row, x, y);

            TravelMapHoverState.AddTarget(x, y, width, 1, region.Id);
            if (!isCurrent)
            {
                clickableElements.Add(new ClickableElement
                {
                    X = x,
                    Y = y,
                    Width = width,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(CultureInfo.InvariantCulture),
                    DisplayText = MenuOptionFormatter.Format(number, region.DisplayNameWithLevels)
                });
            }
        }

        private List<List<ColoredText>> BuildRegionDetails(int width, Character player, TravelRegion region, bool isCurrent)
        {
            var lines = new List<List<ColoredText>>();
            AddWrapped(lines, new ColoredTextBuilder()
                .Add(isCurrent ? "You are in " : "", ColorPalette.Gray)
                .Add(region.DisplayNameWithLevels, region.ContainsLevel(player.Level) ? ColorPalette.Green : ColorPalette.Gold)
                .Build(), width);
            AddWrapped(lines, Plain(region.Description, ColorPalette.Gray), width);
            AddWrapped(lines, Plain("Dungeons: " + string.Join(", ", region.ResolveLinkedDungeonThemePool()), ColorPalette.Cyan), width);
            return lines;
        }

        private List<List<ColoredText>> BuildArrivalLines(int width, TravelRouteResult route)
        {
            var lines = new List<List<ColoredText>>();
            AddWrapped(lines, TravelRouteColoredTextFormatter.FormatArrivalStatus(route), width);
            AddWrapped(lines, TravelRouteColoredTextFormatter.FormatRouteSummary(route), width);
            return lines;
        }

        private List<List<ColoredText>> BuildRouteLines(int width, TravelRouteResult route)
        {
            var lines = new List<List<ColoredText>>();
            AddWrapped(lines, new ColoredTextBuilder()
                .Add(route.FromRegion.DisplayName, ColorPalette.Gold)
                .Add(" -> ", ColorPalette.Gray)
                .Add(route.ToRegion.DisplayName, ColorPalette.Gold)
                .Build(), width);
            if (route.JourneyDungeonThemes.Count > 0)
                AddWrapped(lines, Plain("Dungeon echoes: " + string.Join(", ", route.JourneyDungeonThemes), ColorPalette.Cyan), width);
            lines.Add(new List<ColoredText>());

            foreach (var step in route.Steps)
            {
                AddWrapped(lines, TravelRouteColoredTextFormatter.FormatTravelStep(step), width);
                lines.Add(new List<ColoredText>());
            }

            if (route.Steps.Count == 0)
                AddWrapped(lines, Plain("Setting out...", ColorPalette.Gray), width);
            else
                AddWrapped(lines, TravelRouteColoredTextFormatter.FormatArrivalStatus(route), width);
            return lines;
        }

        /// <summary>Like the combat log: when lines overflow, the newest stay in view at the bottom.</summary>
        private void WriteBottomAligned(List<List<ColoredText>> lines, int x, int top, int bottom)
        {
            int capacity = Math.Max(0, bottom - top + 1);
            int first = Math.Max(0, lines.Count - capacity);
            int rowY = top;
            for (int i = first; i < lines.Count; i++)
            {
                if (lines[i].Count > 0)
                    textWriter.RenderSegments(lines[i], x, rowY);
                rowY++;
            }
        }

        private void AddWrapped(List<List<ColoredText>> lines, List<ColoredText> segments, int width)
        {
            if (segments.Count == 0)
                return;
            lines.AddRange(textWriter.WrapColoredSegments(segments, width));
        }

        private static List<ColoredText> Plain(string? text, ColorPalette color) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<ColoredText>()
                : new ColoredTextBuilder().Add(text, color).Build();

        private static int IndexOf(IReadOnlyList<TravelRegion> regions, TravelRegion region)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                if (string.Equals(regions[i].Id, region.Id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}
