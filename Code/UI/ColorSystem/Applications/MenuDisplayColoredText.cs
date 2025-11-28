using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Formats menu displays using the new ColoredText system
    /// Provides clean, maintainable menu formatting with proper color separation
    /// </summary>
    public static class MenuDisplayColoredText
    {
        /// <summary>
        /// Formats a menu title/header
        /// </summary>
        public static List<ColoredText> FormatMenuTitle(string title, bool centered = false)
        {
            var builder = new ColoredTextBuilder();
            
            if (centered)
            {
                builder.Add("=== ", Colors.Gray);
            }
            
            builder.Add(title, ColorPalette.Gold);
            
            if (centered)
            {
                builder.Add(" ===", Colors.Gray);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a menu option with index
        /// </summary>
        public static List<ColoredText> FormatMenuOption(int index, string text, bool selected = false)
        {
            var builder = new ColoredTextBuilder();
            
            // Index
            builder.Add($"{index}. ", selected ? ColorPalette.Highlight.GetColor() : Colors.Gray);
            
            // Option text
            builder.Add(text, selected ? ColorPalette.Highlight.GetColor() : Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a menu option with custom key
        /// </summary>
        public static List<ColoredText> FormatMenuOptionWithKey(string key, string text, bool selected = false)
        {
            var builder = new ColoredTextBuilder();
            
            // Key in brackets
            builder.Add("[", Colors.Gray);
            builder.Add(key, selected ? ColorPalette.Highlight.GetColor() : ColorPalette.Info.GetColor());
            builder.Add("] ", Colors.Gray);
            
            // Option text
            builder.Add(text, selected ? ColorPalette.Highlight.GetColor() : Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a menu section header
        /// </summary>
        public static List<ColoredText> FormatSectionHeader(string header)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("--- ", Colors.Gray);
            builder.Add(header, ColorPalette.Cyan);
            builder.Add(" ---", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a menu divider
        /// </summary>
        public static List<ColoredText> FormatDivider(int length = 50, char character = '-')
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(new string(character, length), Colors.DarkGray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a key-value pair for menus
        /// </summary>
        public static List<ColoredText> FormatKeyValue(string key, string value, bool highlight = false)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(key, ColorPalette.Info);
            builder.Add(": ", ColorPalette.Gray);
            builder.Add(value, highlight ? ColorPalette.Highlight : ColorPalette.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a prompt message
        /// </summary>
        public static List<ColoredText> FormatPrompt(string message)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("> ", ColorPalette.Cyan);
            builder.Add(message, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats an error message for menus
        /// </summary>
        public static List<ColoredText> FormatError(string message)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("ERROR: ", ColorPalette.Error);
            builder.Add(message, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a success message for menus
        /// </summary>
        public static List<ColoredText> FormatSuccess(string message)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("✓ ", ColorPalette.Success);
            builder.Add(message, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a warning message for menus
        /// </summary>
        public static List<ColoredText> FormatWarning(string message)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚠ ", ColorPalette.Warning);
            builder.Add(message, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats an info message for menus
        /// </summary>
        public static List<ColoredText> FormatInfo(string message)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("ℹ ", ColorPalette.Info);
            builder.Add(message, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a menu with title and options
        /// </summary>
        public static List<List<ColoredText>> FormatMenu(string title, List<string> options, int? selectedIndex = null)
        {
            var lines = new List<List<ColoredText>>();
            
            // Title
            lines.Add(FormatMenuTitle(title, centered: true));
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Options
            for (int i = 0; i < options.Count; i++)
            {
                bool isSelected = selectedIndex.HasValue && selectedIndex.Value == i;
                lines.Add(FormatMenuOption(i + 1, options[i], isSelected));
            }
            
            return lines;
        }
        
        /// <summary>
        /// Formats a confirmation prompt
        /// </summary>
        public static List<List<ColoredText>> FormatConfirmation(string message, string yesText = "Yes", string noText = "No")
        {
            var lines = new List<List<ColoredText>>();
            
            // Message
            var messageLine = new ColoredTextBuilder();
            messageLine.Add(message, Colors.White);
            lines.Add(messageLine.Build());
            
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Options
            lines.Add(FormatMenuOption(1, yesText));
            lines.Add(FormatMenuOption(2, noText));
            
            return lines;
        }
        
        /// <summary>
        /// Formats character selection menu
        /// </summary>
        public static List<List<ColoredText>> FormatCharacterSelection(List<string> characterClasses)
        {
            var lines = new List<List<ColoredText>>();
            
            // Title
            lines.Add(FormatMenuTitle("Choose Your Class", centered: true));
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Classes
            for (int i = 0; i < characterClasses.Count; i++)
            {
                lines.Add(FormatMenuOption(i + 1, characterClasses[i]));
            }
            
            return lines;
        }
        
        /// <summary>
        /// Formats dungeon selection menu
        /// </summary>
        public static List<ColoredText> FormatDungeonOption(int index, string dungeonName, int level, string difficulty)
        {
            var builder = new ColoredTextBuilder();
            
            // Index
            builder.Add($"{index}. ", Colors.Gray);
            
            // Dungeon name
            builder.Add(dungeonName, ColorPalette.Warning.GetColor());
            
            // Level
            builder.Add(" [", Colors.Gray);
            builder.Add($"Lvl {level}", ColorPalette.Info.GetColor());
            builder.Add("] ", Colors.Gray);
            
            // Difficulty
            var diffColor = difficulty.ToLower() switch
            {
                "easy" => ColorPalette.Success.GetColor(),
                "normal" => ColorPalette.Warning.GetColor(),
                "hard" => ColorPalette.Error.GetColor(),
                _ => Colors.White
            };
            builder.Add($"({difficulty})", diffColor);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a status bar
        /// </summary>
        public static List<ColoredText> FormatStatusBar(string label, int current, int max, int barWidth = 20)
        {
            var builder = new ColoredTextBuilder();
            
            // Label
            builder.Add(label, ColorPalette.Info);
            builder.Add(": ", Colors.White);
            
            // Calculate filled portion
            float percentage = (float)current / max;
            int filled = (int)(barWidth * percentage);
            
            // Bar
            builder.Add("[", Colors.Gray);
            
            // Filled portion (color based on percentage)
            var fillColor = percentage > 0.5f ? ColorPalette.Success :
                           percentage > 0.25f ? ColorPalette.Warning :
                           ColorPalette.Error;
            
            if (filled > 0)
            {
                builder.Add(new string('█', filled), fillColor);
            }
            
            // Empty portion
            if (filled < barWidth)
            {
                builder.Add(new string('░', barWidth - filled), Colors.DarkGray);
            }
            
            builder.Add("] ", Colors.Gray);
            
            // Numbers
            builder.Add($"{current}/{max}", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a button/action prompt
        /// </summary>
        public static List<ColoredText> FormatButton(string text, bool active = true)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("[", Colors.Gray);
            builder.Add(text, active ? ColorPalette.Cyan : ColorPalette.Disabled);
            builder.Add("]", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a breadcrumb navigation path
        /// </summary>
        public static List<ColoredText> FormatBreadcrumb(List<string> path)
        {
            var builder = new ColoredTextBuilder();
            
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    builder.Add(" > ", Colors.Gray);
                }
                
                bool isLast = i == path.Count - 1;
                builder.Add(path[i], isLast ? ColorPalette.Highlight : ColorPalette.Info);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a progress indicator
        /// </summary>
        public static List<ColoredText> FormatProgress(string label, int current, int total)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(label, ColorPalette.Info);
            builder.Add(": ", Colors.White);
            builder.Add(current.ToString(), ColorPalette.Success);
            builder.Add(" / ", Colors.Gray);
            builder.Add(total.ToString(), Colors.White);
            
            float percentage = (float)current / total * 100;
            builder.Add($" ({percentage:F0}%)", Colors.Gray);
            
            return builder.Build();
        }
    }
}
