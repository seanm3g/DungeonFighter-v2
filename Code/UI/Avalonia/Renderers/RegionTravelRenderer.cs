using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    public class RegionTravelRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly TravelRegionCatalog regionCatalog = new();

        public RegionTravelRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
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
            canvas.AddText(left, currentY, Truncate(currentRegion.Description, contentWidth), AsciiArtAssets.Colors.Gray);
            currentY += 2;

            canvas.AddText(left, currentY, "Destinations:", AsciiArtAssets.Colors.Gold);
            currentY += 2;

            for (int i = 0; i < destinations.Count; i++)
            {
                var destination = destinations[i];
                int optionNumber = i + 1;
                string label = $"{destination.DisplayName} - {destination.Description}";
                var option = new ClickableElement
                {
                    X = left,
                    Y = currentY,
                    Width = contentWidth,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = optionNumber.ToString(),
                    DisplayText = MenuOptionFormatter.Format(optionNumber, destination.DisplayName)
                };
                clickableElements.Add(option);
                canvas.AddMenuOption(left, currentY, optionNumber, Truncate(label, Math.Max(1, contentWidth - 4)), AsciiArtAssets.Colors.White, option.IsHovered);
                currentY++;
            }

            currentY++;
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

                foreach (var step in routeResult.Steps)
                {
                    if (currentY >= y + height - 5)
                        break;

                    string line = $"{step.StepNumber}. d20 {step.Roll} {step.Outcome}: {step.Event.Title} - {step.Event.Narrative}";
                    canvas.AddText(left, currentY, Truncate(line, contentWidth), GetOutcomeColor(step.Outcome));
                    currentY++;
                }

                currentY++;
                string summary = $"Progress {Signed(routeResult.TotalProgressDelta)}";
                if (routeResult.TotalDamageTaken > 0)
                    summary += $", damage {routeResult.TotalDamageTaken}";
                if (routeResult.TotalHealingReceived > 0)
                    summary += $", healed {routeResult.TotalHealingReceived}";
                if (routeResult.TotalXpGained > 0)
                    summary += $", XP +{routeResult.TotalXpGained}";
                if (routeResult.LootFound.Count > 0)
                    summary += $", loot {routeResult.LootFound.Count}";

                canvas.AddText(left, currentY, Truncate(summary, contentWidth), AsciiArtAssets.Colors.Green);
                currentY++;
                canvas.AddText(left, currentY, $"Arrived in {routeResult.ToRegion.DisplayName}.", AsciiArtAssets.Colors.White);
            }

            return currentY - y;
        }

        private static Color GetOutcomeColor(TravelRollOutcome outcome) => outcome switch
        {
            TravelRollOutcome.CriticalMiss => AsciiArtAssets.Colors.Red,
            TravelRollOutcome.Miss => AsciiArtAssets.Colors.Orange,
            TravelRollOutcome.Hit => AsciiArtAssets.Colors.White,
            TravelRollOutcome.Combo => AsciiArtAssets.Colors.Cyan,
            TravelRollOutcome.Critical => AsciiArtAssets.Colors.Gold,
            _ => AsciiArtAssets.Colors.White
        };

        private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();

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
