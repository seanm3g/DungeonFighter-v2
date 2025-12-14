using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat
{
    /// <summary>
    /// Display logic for battle narratives
    /// Handles presentation and output of formatted narratives
    /// </summary>
    public static class BattleNarrativeDisplay
    {
        /// <summary>
        /// Displays a narrative as ColoredText
        /// </summary>
        public static void DisplayNarrative(List<ColoredText> narrative)
        {
            // This can be extended to handle different display methods
            // For now, it's a placeholder for future display logic
            // The actual display is handled by the UI system
        }
        
        /// <summary>
        /// Displays multiple narratives
        /// </summary>
        public static void DisplayNarratives(List<List<ColoredText>> narratives)
        {
            foreach (var narrative in narratives)
            {
                DisplayNarrative(narrative);
            }
        }
        
        /// <summary>
        /// Formats and displays a narrative from text
        /// </summary>
        public static void DisplayNarrativeText(string narrativeText, ColorPalette color = ColorPalette.Info)
        {
            var formatted = BattleNarrativeFormatter.FormatGenericNarrative(narrativeText, color);
            DisplayNarrative(formatted);
        }
    }
}

