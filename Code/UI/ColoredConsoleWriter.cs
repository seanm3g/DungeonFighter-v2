using System;
using System.Collections.Generic;

namespace RPGGame.UI
{
    /// <summary>
    /// Writes colored text to console using parsed color segments
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
            var segments = ColorParser.Parse(text);
            WriteSegments(segments);
        }

        /// <summary>
        /// Writes colored text to console with newline
        /// </summary>
        public static void WriteLine(string text)
        {
            Write(text);
            Console.WriteLine();
        }

        /// <summary>
        /// Writes colored segments to console
        /// </summary>
        public static void WriteSegments(List<ColorDefinitions.ColoredSegment> segments)
        {
            foreach (var segment in segments)
            {
                if (segment.Foreground.HasValue)
                {
                    Console.ForegroundColor = segment.Foreground.Value.ToConsoleColor();
                    currentForeground = segment.Foreground.Value.ToConsoleColor();
                }

                if (segment.Background.HasValue)
                {
                    Console.BackgroundColor = segment.Background.Value.ToConsoleColor();
                    currentBackground = segment.Background.Value.ToConsoleColor();
                }

                Console.Write(segment.Text);
            }
        }

        /// <summary>
        /// Resets console colors to defaults
        /// </summary>
        public static void ResetColors()
        {
            Console.ResetColor();
            currentForeground = ConsoleColor.Gray;
            currentBackground = ConsoleColor.Black;
        }

        /// <summary>
        /// Gets the current foreground color
        /// </summary>
        public static ConsoleColor GetCurrentForeground() => currentForeground;

        /// <summary>
        /// Gets the current background color
        /// </summary>
        public static ConsoleColor GetCurrentBackground() => currentBackground;
    }
}

