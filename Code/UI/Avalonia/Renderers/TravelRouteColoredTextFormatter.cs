using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Builds <see cref="ColoredText"/> segments for region travel logs, matching combat log conventions
    /// (gray structure, <see cref="ColorPalette.Info"/> for roll labels, white numerals, outcome-colored keywords).
    /// </summary>
    public static class TravelRouteColoredTextFormatter
    {
        public static List<ColoredText> FormatEventCountHeader(TravelRouteResult route)
        {
            var builder = new ColoredTextBuilder();
            if (route.EventCountDice != null && route.EventCountDice.Length > 0)
            {
                builder.Add("Travel events (", ColorPalette.Gray);
                builder.Add("4d4", ColorPalette.Info);
                builder.Add("): ", ColorPalette.Gray);
                builder.Add(string.Join("+", route.EventCountDice), ColorPalette.White);
                builder.Add(" = ", ColorPalette.Gray);
                builder.Add(route.EventCount.ToString(), ColorPalette.White);
            }
            else
            {
                builder.Add("Travel events: ", ColorPalette.Gray);
                builder.Add(route.EventCount.ToString(), ColorPalette.White);
            }

            return builder.Build();
        }

        public static List<ColoredText> FormatTravelStep(TravelStepResult step)
        {
            var builder = new ColoredTextBuilder();
            builder.Add($"{step.StepNumber}. ", ColorPalette.Gray);
            builder.Add("d20 ", ColorPalette.Info);
            builder.Add(step.Roll.ToString(), ColorPalette.White);
            builder.Add(" ", ColorPalette.White);
            builder.Add(FormatOutcomeLabel(step.Outcome), GetOutcomeWordPalette(step.Outcome));
            builder.Add(" (", ColorPalette.Gray);
            builder.Add(TravelPacing.FormatTravelTime(step.TravelMinutes), ColorPalette.White);
            builder.Add("): ", ColorPalette.Gray);
            builder.Add(step.Event.Title ?? "", ColorPalette.White);
            builder.Add(" - ", ColorPalette.Gray);
            builder.Add(step.Event.Narrative ?? "", ColorPalette.Gray);
            return builder.Build();
        }

        public static List<ColoredText> FormatRouteSummary(TravelRouteResult route)
        {
            var builder = new ColoredTextBuilder();
            bool hasClause = false;

            void Comma()
            {
                if (hasClause)
                    builder.Add(", ", ColorPalette.Gray);
                hasClause = true;
            }

            Comma();
            builder.Add("Progress ", ColorPalette.Gray);
            builder.Add(Signed(route.TotalProgressDelta), ColorPalette.Success);

            Comma();
            builder.Add("time ", ColorPalette.Gray);
            builder.Add(TravelPacing.FormatTravelTime(route.TotalTravelMinutes), ColorPalette.White);

            if (route.TotalDamageTaken > 0)
            {
                Comma();
                builder.Add("damage ", ColorPalette.Gray);
                builder.Add(route.TotalDamageTaken.ToString(), ColorPalette.Error);
            }

            if (route.TotalHealingReceived > 0)
            {
                Comma();
                builder.Add("healed ", ColorPalette.Gray);
                builder.Add(route.TotalHealingReceived.ToString(), ColorPalette.Healing);
            }

            if (route.TotalXpGained > 0)
            {
                Comma();
                builder.Add("XP ", ColorPalette.Gray);
                builder.Add($"+{route.TotalXpGained}", ColorPalette.Info);
            }

            if (route.LootFound.Count > 0)
            {
                Comma();
                builder.Add("loot ", ColorPalette.Gray);
                builder.Add(route.LootFound.Count.ToString(), ColorPalette.Warning);
            }

            return builder.Build();
        }

        public static List<ColoredText> FormatArrivalStatus(TravelRouteResult route)
        {
            var builder = new ColoredTextBuilder();
            if (route.IsComplete)
            {
                builder.Add("Arrived in ", ColorPalette.Gray);
                builder.Add(route.ToRegion.DisplayName, ColorPalette.Gold);
                builder.Add(".", ColorPalette.Gray);
            }
            else
            {
                builder.Add("Traveling to ", ColorPalette.Gray);
                builder.Add(route.ToRegion.DisplayName, ColorPalette.Gold);
                builder.Add("...", ColorPalette.Gray);
            }

            return builder.Build();
        }

        public static string FormatOutcomeLabel(TravelRollOutcome outcome) => outcome switch
        {
            TravelRollOutcome.CriticalMiss => "Critical miss",
            TravelRollOutcome.Miss => "Miss",
            TravelRollOutcome.Hit => "Hit",
            TravelRollOutcome.Combo => "Combo",
            TravelRollOutcome.Critical => "Critical",
            _ => outcome.ToString()
        };

        private static ColorPalette GetOutcomeWordPalette(TravelRollOutcome outcome) => outcome switch
        {
            TravelRollOutcome.CriticalMiss => ColorPalette.Error,
            TravelRollOutcome.Miss => ColorPalette.Miss,
            TravelRollOutcome.Hit => ColorPalette.White,
            TravelRollOutcome.Combo => ColorPalette.Cyan,
            TravelRollOutcome.Critical => ColorPalette.Critical,
            _ => ColorPalette.White
        };

        private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();
    }
}
