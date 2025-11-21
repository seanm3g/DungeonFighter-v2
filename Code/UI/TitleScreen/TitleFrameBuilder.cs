using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Represents a single frame of the title screen animation
    /// Contains all the lines to be displayed at a specific moment
    /// </summary>
    public class TitleFrame
    {
        public List<ColoredText>[] Lines { get; set; }

        public TitleFrame(List<ColoredText>[] lines)
        {
            Lines = lines ?? Array.Empty<List<ColoredText>>();
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
        /// Builds a frame with both words using templates
        /// </summary>
        /// <param name="dungeonTemplate">Template name for DUNGEON text (e.g., "golden")</param>
        /// <param name="fighterTemplate">Template name for FIGHTER text (e.g., "fiery")</param>
        /// <returns>Complete title frame</returns>
        public TitleFrame BuildTemplateFrame(string dungeonTemplate, string fighterTemplate)
        {
            // Apply templates to each line
            var dungeonColoredLines = ApplyTemplateToLines(TitleArtAssets.DungeonLines, dungeonTemplate);
            var fighterColoredLines = ApplyTemplateToLines(TitleArtAssets.FighterLines, fighterTemplate);

            // Build complete frame with layout
            var frameLines = BuildFrameLayout(dungeonColoredLines, fighterColoredLines);

            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Builds a frame with both words using solid colors (for transitions)
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
            var frameLines = BuildFrameLayout(dungeonColoredLines, fighterColoredLines);

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

            // If progress is 1.0 (final state), use templates instead of solid colors
            if (progress >= 1.0f)
            {
                return BuildTemplateFrame("golden", "fiery");
            }

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
            var frameLines = BuildFrameLayout(dungeonTransitionLines, fighterTransitionLines);

            return new TitleFrame(frameLines);
        }

        /// <summary>
        /// Applies a template to multiple lines
        /// </summary>
        private List<ColoredText>[] ApplyTemplateToLines(string[] lines, string templateName)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplyTemplate(lines[i], templateName);
            }
            return coloredLines;
        }

        /// <summary>
        /// Applies a solid color to multiple lines
        /// </summary>
        private List<ColoredText>[] ColorizeLines(string[] lines, string colorCode)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                coloredLines[i] = TitleColorApplicator.ApplySolidColor(lines[i], colorCode);
            }
            return coloredLines;
        }

        /// <summary>
        /// Applies transition colors to multiple lines
        /// </summary>
        private List<ColoredText>[] ColorizeTransitionLines(string[] lines, string sourceColor, string targetColor, float progress)
        {
            var coloredLines = new List<ColoredText>[lines.Length];
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
        private List<ColoredText>[] BuildFrameLayout(List<ColoredText>[] dungeonLines, List<ColoredText>[] fighterLines)
        {
            var frameList = new List<List<ColoredText>>();

            // Top padding - empty lines
            for (int i = 0; i < 15; i++)
            {
                frameList.Add(new List<ColoredText>());
            }

            // Add blank lines before title
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            // DUNGEON title lines
            foreach (var line in dungeonLines)
            {
                frameList.Add(line);
            }

            // Spacing and decorator
            frameList.Add(new List<ColoredText>());
            // Decorator line - use white color
            var decoratorSegments = TitleColorApplicator.ApplySolidColor(TitleArtAssets.DecoratorLine, "Y");
            frameList.Add(decoratorSegments);
            frameList.Add(new List<ColoredText>());

            // FIGHTER title lines
            foreach (var line in fighterLines)
            {
                frameList.Add(line);
            }

            // Bottom spacing
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            // Tagline - use white color
            var taglineSegments = TitleColorApplicator.ApplySolidColor(TitleArtAssets.Tagline, "Y");
            frameList.Add(taglineSegments);

            // Final spacing
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            return frameList.ToArray();
        }
    }
}

