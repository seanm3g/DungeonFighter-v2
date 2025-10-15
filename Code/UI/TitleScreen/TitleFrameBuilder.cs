using System;
using System.Collections.Generic;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Represents a single frame of the title screen animation
    /// Contains all the lines to be displayed at a specific moment
    /// </summary>
    public class TitleFrame
    {
        public string[] Lines { get; set; }

        public TitleFrame(string[] lines)
        {
            Lines = lines ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Builds title screen frames using the Builder pattern
    /// Separates frame construction logic from animation and rendering
    /// </summary>
    public class TitleFrameBuilder
    {
        private readonly TitleAnimationConfig _config;

        public TitleFrameBuilder(TitleAnimationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Builds a frame with both words using solid colors
        /// </summary>
        /// <param name="dungeonColor">Color code for DUNGEON text</param>
        /// <param name="fighterColor">Color code for FIGHTER text</param>
        /// <returns>Complete title frame</returns>
        public TitleFrame BuildSolidColorFrame(string dungeonColor, string fighterColor)
        {
            // Apply colors to each line
            var dungeonColoredLines = ColorizeLines(TitleArtAssets.DungeonLines, dungeonColor);
            var fighterColoredLines = ColorizeLines(TitleArtAssets.FighterLines, fighterColor);

            // Build complete frame with layout
            string[] frameLines = BuildFrameLayout(dungeonColoredLines, fighterColoredLines);

            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Builds a frame with transition colors (for animation effect)
        /// </summary>
        /// <param name="progress">Transition progress from 0.0 to 1.0</param>
        /// <returns>Complete title frame with transition colors</returns>
        public TitleFrame BuildTransitionFrame(float progress)
        {
            var scheme = _config.ColorScheme;

            // Apply transition colors to each line
            var dungeonTransitionLines = ColorizeTransitionLines(
                TitleArtAssets.DungeonLines,
                scheme.TransitionFromColor,
                scheme.DungeonFinalColor,
                progress
            );

            var fighterTransitionLines = ColorizeTransitionLines(
                TitleArtAssets.FighterLines,
                scheme.TransitionFromColor,
                scheme.FighterFinalColor,
                progress
            );

            // Build complete frame with layout
            string[] frameLines = BuildFrameLayout(dungeonTransitionLines, fighterTransitionLines);

            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Applies a solid color to multiple lines
        /// </summary>
        private string[] ColorizeLines(string[] lines, string colorCode)
        {
            var coloredLines = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplySolidColor(lines[i], colorCode);
            }
            return coloredLines;
        }

        /// <summary>
        /// Applies transition colors to multiple lines
        /// </summary>
        private string[] ColorizeTransitionLines(string[] lines, string sourceColor, string targetColor, float progress)
        {
            var coloredLines = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplyTransitionColor(
                    lines[i],
                    sourceColor,
                    targetColor,
                    progress
                );
            }
            return coloredLines;
        }

        /// <summary>
        /// Builds the complete frame layout with padding, decoration, and tagline
        /// </summary>
        private string[] BuildFrameLayout(string[] dungeonLines, string[] fighterLines)
        {
            var frameList = new List<string>();

            // Top padding - empty lines
            for (int i = 0; i < 15; i++)
            {
                frameList.Add("");
            }

            // Add blank lines before title
            frameList.Add("");
            frameList.Add("");

            // DUNGEON title lines
            foreach (var line in dungeonLines)
            {
                frameList.Add(line);
            }

            // Spacing and decorator
            frameList.Add("");
            frameList.Add(TitleArtAssets.DecoratorLine);
            frameList.Add("");

            // FIGHTER title lines
            foreach (var line in fighterLines)
            {
                frameList.Add(line);
            }

            // Bottom spacing
            frameList.Add("");
            frameList.Add("");
            frameList.Add("");

            // Tagline
            frameList.Add(TitleArtAssets.Tagline);

            // Final spacing
            frameList.Add("");
            frameList.Add("");
            frameList.Add("");

            return frameList.ToArray();
        }
    }
}

