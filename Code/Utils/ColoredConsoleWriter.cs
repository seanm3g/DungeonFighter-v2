using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;
using RPGGame;

namespace RPGGame.Utils
{
    /// <summary>
    /// Writes colored text to console using the new color system
    /// </summary>
    public static class ColoredConsoleWriter
    {
        private static ConsoleColor currentForeground = ConsoleColor.Gray;
        private static ConsoleColor currentBackground = ConsoleColor.Black;

        /// <summary>
        /// Writes colored text to console
        /// </summary>
        public static void Write(string text)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            var segments = ColoredTextParser.Parse(text);
            WriteSegments(segments);
        }

        /// <summary>
        /// Writes colored text to console with newline
        /// </summary>
        public static void WriteLine(string text)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            Write(text);
            Console.WriteLine();
        }

        /// <summary>
        /// Writes colored segments to console
        /// </summary>
        public static void WriteSegments(List<ColoredText> segments)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            foreach (var segment in segments)
            {
                var consoleColor = ConvertToConsoleColor(segment.Color);
                Console.ForegroundColor = consoleColor;
                currentForeground = consoleColor;

                Console.Write(segment.Text);
            }
        }

        /// <summary>
        /// Writes colored text using the builder pattern
        /// </summary>
        public static void WriteColoredText(ColoredTextBuilder builder)
        {
            var segments = builder.Build();
            WriteSegments(segments);
        }

        /// <summary>
        /// Writes colored text using the builder pattern with newline
        /// </summary>
        public static void WriteLineColoredText(ColoredTextBuilder builder)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            WriteColoredText(builder);
            Console.WriteLine();
        }

        /// <summary>
        /// Writes text with a specific color
        /// </summary>
        public static void Write(string text, ColorPalette color)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            var consoleColor = ConvertToConsoleColor(color.GetColor());
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
        }

        /// <summary>
        /// Writes text with a specific color and newline
        /// </summary>
        public static void WriteLine(string text, ColorPalette color)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            Write(text, color);
            Console.WriteLine();
        }

        /// <summary>
        /// Writes text with a pattern
        /// </summary>
        public static void WriteWithPattern(string text, string pattern)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            var color = ColorPatterns.GetColorForPattern(pattern);
            var consoleColor = ConvertToConsoleColor(color);
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
        }

        /// <summary>
        /// Writes text with a pattern and newline
        /// </summary>
        public static void WriteLineWithPattern(string text, string pattern)
        {
            // Skip writing if UI output is disabled (e.g., during tests)
            if (UIManager.DisableAllUIOutput)
            {
                return;
            }

            WriteWithPattern(text, pattern);
            Console.WriteLine();
        }

        /// <summary>
        /// Resets console colors to default
        /// </summary>
        public static void ResetColors()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            currentForeground = ConsoleColor.Gray;
            currentBackground = ConsoleColor.Black;
        }

        /// <summary>
        /// Gets the current foreground color
        /// </summary>
        public static ConsoleColor GetCurrentForegroundColor()
        {
            return currentForeground;
        }

        /// <summary>
        /// Gets the current background color
        /// </summary>
        public static ConsoleColor GetCurrentBackgroundColor()
        {
            return currentBackground;
        }

        /// <summary>
        /// Converts Avalonia color to console color
        /// </summary>
        private static ConsoleColor ConvertToConsoleColor(Color color)
        {
            // Simple color mapping - you may need to adjust this based on your needs
            if (color == Colors.Red) return ConsoleColor.Red;
            if (color == Colors.Green) return ConsoleColor.Green;
            if (color == Colors.Blue) return ConsoleColor.Blue;
            if (color == Colors.Yellow) return ConsoleColor.Yellow;
            if (color == Colors.Cyan) return ConsoleColor.Cyan;
            if (color == Colors.Magenta) return ConsoleColor.Magenta;
            if (color == Colors.White) return ConsoleColor.White;
            if (color == Colors.Black) return ConsoleColor.Black;
            if (color == Colors.Gray) return ConsoleColor.Gray;
            if (color == Colors.DarkRed) return ConsoleColor.DarkRed;
            if (color == Colors.DarkGreen) return ConsoleColor.DarkGreen;
            if (color == Colors.DarkBlue) return ConsoleColor.DarkBlue;
            if (color == Color.FromRgb(128, 128, 0)) return ConsoleColor.DarkYellow;
            if (color == Colors.DarkCyan) return ConsoleColor.DarkCyan;
            if (color == Colors.DarkMagenta) return ConsoleColor.DarkMagenta;
            if (color == Colors.DarkGray) return ConsoleColor.DarkGray;
            if (color == Colors.LightGray) return ConsoleColor.Gray;
            
            // Default to gray for unknown colors
            return ConsoleColor.Gray;
        }
    }
}
