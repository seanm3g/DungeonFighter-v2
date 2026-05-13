using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    public class RegionTravelRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private readonly TravelRegionCatalog regionCatalog = new();

        public RegionTravelRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
            this.clickableElements = clickableElements;
        }

        public int RenderRegionTravel(
            int x,
            int y,
            int width,
            int height,
            Character player,
            IReadOnlyList<TravelRegion> destinations,
            TravelRouteResult? routeResult)
        {
            int currentY = y + 1;
            int left = x + 2;
            int contentWidth = Math.Max(20, width - 4);
            var currentRegion = regionCatalog.GetRegionForCharacter(player);

            canvas.AddText(left, currentY, "REGION TRAVEL", AsciiArtAssets.Colors.Gold);
            currentY += 2;
            canvas.AddText(left, currentY, $"Current region: {currentRegion.DisplayName}", AsciiArtAssets.Colors.White);
            currentY++;
            foreach (var line in TextWrapper.WrapText(currentRegion.Description ?? string.Empty, contentWidth))
            {
                canvas.AddText(left, currentY, line, AsciiArtAssets.Colors.Gray);
                currentY++;
            }

            foreach (var line in TextWrapper.WrapText(
                         "Dungeons: " + string.Join(", ", currentRegion.ResolveLinkedDungeonThemePool()),
                         contentWidth))
            {
                canvas.AddText(left, currentY, line, AsciiArtAssets.Colors.Cyan);
                currentY++;
            }

            currentY++;

            canvas.AddText(left, currentY, "Destinations:", AsciiArtAssets.Colors.Gold);
            currentY += 2;

            for (int i = 0; i < destinations.Count; i++)
            {
                var destination = destinations[i];
                int optionNumber = i + 1;
                int descriptionIndent = $"[{optionNumber}] ".Length;
                int descriptionWidth = Math.Max(1, contentWidth - descriptionIndent);
                var descriptionLines = TextWrapper.WrapText(destination.Description ?? string.Empty, descriptionWidth);
                var themePool = destination.ResolveLinkedDungeonThemePool();
                string dungeonsLine = "Dungeons: " + string.Join(", ", themePool);
                var dungeonLines = TextWrapper.WrapText(dungeonsLine, descriptionWidth);
                int destinationHeight = 1 + descriptionLines.Count + dungeonLines.Count;

                var option = new ClickableElement
                {
                    X = left,
                    Y = currentY,
                    Width = contentWidth,
                    Height = destinationHeight,
                    Type = ElementType.MenuOption,
                    Value = optionNumber.ToString(),
                    DisplayText = MenuOptionFormatter.Format(optionNumber, destination.DisplayName)
                };
                clickableElements.Add(option);
                canvas.AddMenuOption(left, currentY, optionNumber, destination.DisplayName, AsciiArtAssets.Colors.White, option.IsHovered);
                currentY++;

                foreach (var line in descriptionLines)
                {
                    canvas.AddText(left + descriptionIndent, currentY, line, AsciiArtAssets.Colors.Gray);
                    currentY++;
                }

                foreach (var line in dungeonLines)
                {
                    canvas.AddText(left + descriptionIndent, currentY, line, AsciiArtAssets.Colors.Cyan);
                    currentY++;
                }

                currentY++;
            }

            var returnOption = new ClickableElement
            {
                X = left,
                Y = currentY,
                Width = 30,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.ReturnToGameMenu)
            };
            clickableElements.Add(returnOption);
            canvas.AddMenuOption(left, currentY, 0, UIConstants.MenuOptions.ReturnToGameMenu, AsciiArtAssets.Colors.White, returnOption.IsHovered);
            currentY += 2;

            if (routeResult != null)
            {
                canvas.AddText(left, currentY, $"Route: {routeResult.FromRegion.DisplayName} -> {routeResult.ToRegion.DisplayName}", AsciiArtAssets.Colors.Gold);
                currentY += 2;

                if (routeResult.JourneyDungeonThemes.Count > 0)
                {
                    string journeyLine = "This journey's dungeon echoes: " + string.Join(", ", routeResult.JourneyDungeonThemes);
                    canvas.AddText(left, currentY, Truncate(journeyLine, contentWidth), AsciiArtAssets.Colors.Cyan);
                    currentY += 2;
                }

                if (routeResult.Steps.Count > 0)
                {
                    var headerSegments = TravelRouteColoredTextFormatter.FormatEventCountHeader(routeResult);
                    int headerLines = textWriter.WriteLineColoredWrapped(headerSegments, left, currentY, contentWidth);
                    currentY += headerLines;
                }

                foreach (var step in routeResult.Steps)
                {
                    var stepSegments = TravelRouteColoredTextFormatter.FormatTravelStep(step);
                    int stepLines = textWriter.WrapColoredSegments(stepSegments, contentWidth).Count;
                    if (currentY + stepLines > y + height - 5)
                        break;

                    currentY += textWriter.WriteLineColoredWrapped(stepSegments, left, currentY, contentWidth);
                }

                currentY++;
                if (routeResult.Steps.Count == 0)
                {
                    canvas.AddText(left, currentY, "Preparing route...", AsciiArtAssets.Colors.Gray);
                }
                else
                {
                    var summarySegments = TravelRouteColoredTextFormatter.FormatRouteSummary(routeResult);
                    currentY += textWriter.WriteLineColoredWrapped(summarySegments, left, currentY, contentWidth);
                    var statusSegments = TravelRouteColoredTextFormatter.FormatArrivalStatus(routeResult);
                    currentY += textWriter.WriteLineColoredWrapped(statusSegments, left, currentY, contentWidth);
                }
            }

            return currentY - y;
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
