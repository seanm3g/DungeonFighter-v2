using System;
using System.Collections.Generic;
using Avalonia.Media;
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
        /// <param name="fighterTemplate">Template name for FIGHTERS text (e.g., "fiery")</param>
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
        /// <param name="fighterColor">Color code for FIGHTERS text</param>
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
            // Use title-specific templates that use yellow and orange (no white stripes)
            if (progress >= 1.0f)
            {
                return BuildTemplateFrame("title_dungeon_yellow_orange", "title_fighter_yellow_orange");
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
        /// Prepends spaces to a list of ColoredText segments
        /// </summary>
        private List<ColoredText> PrependSpaces(List<ColoredText> segments, int spaceCount)
        {
            var result = new List<ColoredText>();
            
            // Add spaces at the beginning (using white color)
            if (spaceCount > 0)
            {
                result.Add(new ColoredText(new string(' ', spaceCount), Colors.White));
            }
            
            // Add all existing segments
            if (segments != null)
            {
                result.AddRange(segments);
            }
            
            return result;
        }

        /// <summary>
        /// Ensures exactly one space between each segment in a list
        /// This fixes spacing issues where segments are rendered without spaces between them
        /// </summary>
        private List<ColoredText> EnsureSpacesBetweenSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return segments ?? new List<ColoredText>();
            
            var result = new List<ColoredText>();
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                // Add the segment
                result.Add(segment);
                
                // If this is not the last segment, check if we need to add a space
                if (i < segments.Count - 1)
                {
                    var nextSegment = segments[i + 1];
                    
                    // Check if current segment ends with space or next starts with space
                    bool currentEndsWithSpace = !string.IsNullOrEmpty(segment.Text) && 
                                                segment.Text.Length > 0 && 
                                                char.IsWhiteSpace(segment.Text[segment.Text.Length - 1]);
                    bool nextStartsWithSpace = !string.IsNullOrEmpty(nextSegment.Text) && 
                                               nextSegment.Text.Length > 0 && 
                                               char.IsWhiteSpace(nextSegment.Text[0]);
                    
                    // If neither segment has a space at the boundary, add one
                    if (!currentEndsWithSpace && !nextStartsWithSpace)
                    {
                        result.Add(new ColoredText(" ", Colors.White));
                    }
                }
            }
            
            return result;
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
                // Ensure spaces are preserved between segments
                var lineWithSpaces = EnsureSpacesBetweenSegments(line);
                frameList.Add(lineWithSpaces);
            }

            // Spacing and decorator
            frameList.Add(new List<ColoredText>());
            // Decorator line - use white color
            var decoratorSegments = TitleColorApplicator.ApplySolidColor(TitleArtAssets.DecoratorLine, "Y");
            // Ensure spaces are preserved between segments
            var decoratorWithSpaces = EnsureSpacesBetweenSegments(decoratorSegments);
            frameList.Add(decoratorWithSpaces);
            frameList.Add(new List<ColoredText>());

            // FIGHTERS title lines
            foreach (var line in fighterLines)
            {
                // Ensure spaces are preserved between segments
                var lineWithSpaces = EnsureSpacesBetweenSegments(line);
                frameList.Add(lineWithSpaces);
            }

            // Bottom spacing
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            // Tagline - use white color
            var taglineSegments = TitleColorApplicator.ApplySolidColor(TitleArtAssets.Tagline, "Y");
            // Ensure spaces are preserved between segments
            var taglineWithSpaces = EnsureSpacesBetweenSegments(taglineSegments);
            // Add 6 spaces to the right to move the tagline
            var taglineWithIndent = PrependSpaces(taglineWithSpaces, 6);
            frameList.Add(taglineWithIndent);

            // Final spacing
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());
            frameList.Add(new List<ColoredText>());

            return frameList.ToArray();
        }
    }
}

