using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.Animations
{
    /// <summary>
    /// Handles text fade animations with color patterns
    /// Supports multiple fade patterns including alternating letter dimming
    /// </summary>
    public static class TextFadeAnimator
    {
        /// <summary>
        /// Fade pattern type for animation
        /// </summary>
        public enum FadePattern
        {
            /// <summary>Every other letter fades in alternating waves</summary>
            Alternating,
            /// <summary>Letters fade from left to right</summary>
            Sequential,
            /// <summary>Letters fade from outside to center</summary>
            CenterCollapse,
            /// <summary>Letters fade from center to outside</summary>
            CenterExpand,
            /// <summary>All letters fade uniformly</summary>
            Uniform,
            /// <summary>Random letters fade one by one</summary>
            Random
        }

        /// <summary>
        /// Fade direction (for color progression)
        /// </summary>
        public enum FadeDirection
        {
            /// <summary>Fade from bright to dark</summary>
            ToDark,
            /// <summary>Fade from dark to bright</summary>
            ToBright,
            /// <summary>Fade through a color template pattern</summary>
            TemplateProgression
        }

        /// <summary>
        /// Configuration for fade animation
        /// </summary>
        public class FadeConfig
        {
            public FadePattern Pattern { get; set; } = FadePattern.Alternating;
            public FadeDirection Direction { get; set; } = FadeDirection.ToDark;
            public string? ColorTemplate { get; set; } = null; // Optional color template to use
            public int FrameDelayMs { get; set; } = 100; // Delay between animation frames
            public int TotalFrames { get; set; } = 5; // Total number of animation frames
            public bool ClearAfterFade { get; set; } = true; // Clear line after fade completes
        }

        /// <summary>
        /// Animates text fading from the screen
        /// </summary>
        public static void FadeOut(string text, FadeConfig? config = null)
        {
            config ??= new FadeConfig();
            
            // Strip any existing color markup to work with plain text
            var segments = ColoredTextParser.Parse(text);
            string plainText = ColoredTextRenderer.RenderAsPlainText(segments);
            
            // Generate animation frames
            var frames = GenerateFadeFrames(plainText, config);
            
            // Display animation
            DisplayFadeAnimation(frames, config);
        }

        /// <summary>
        /// Generates all animation frames for the fade effect
        /// </summary>
        private static List<string> GenerateFadeFrames(string text, FadeConfig config)
        {
            var frames = new List<string>();
            
            // Generate brightness/color values for each frame
            var colorSteps = GenerateColorSteps(config);
            
            // Create letter indices based on pattern
            var letterSequence = GenerateLetterSequence(text, config.Pattern);
            
            // Build each frame
            for (int frame = 0; frame < config.TotalFrames; frame++)
            {
                var frameText = BuildFrame(text, letterSequence, frame, colorSteps, config);
                frames.Add(frameText);
            }
            
            return frames;
        }

        /// <summary>
        /// Generates color steps for each frame
        /// </summary>
        private static List<char> GenerateColorSteps(FadeConfig config)
        {
            if (config.Direction == FadeDirection.TemplateProgression && config.ColorTemplate != null)
            {
                // Use default color progression for template mode
                // TODO: Extract colors from ColorTemplateLibrary if needed
                return new List<char> { 'Y', 'y', 'K', 'k' }; // White -> Grey -> Dark Grey -> Very Dark
            }
            
            // Default brightness progression (bright to dark)
            if (config.Direction == FadeDirection.ToDark)
            {
                return new List<char> { 'Y', 'y', 'K', 'k' }; // White -> Grey -> Dark Grey -> Very Dark
            }
            else if (config.Direction == FadeDirection.ToBright)
            {
                return new List<char> { 'k', 'K', 'y', 'Y' }; // Very Dark -> Dark Grey -> Grey -> White
            }
            
            return new List<char> { 'y' }; // Default grey
        }

        /// <summary>
        /// Generates the sequence in which letters fade based on pattern
        /// </summary>
        private static List<int> GenerateLetterSequence(string text, FadePattern pattern)
        {
            var indices = new List<int>();
            var letterPositions = new List<int>();
            
            // Get positions of non-whitespace characters
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    letterPositions.Add(i);
                }
            }
            
            switch (pattern)
            {
                case FadePattern.Alternating:
                    // Even indices first, then odd indices
                    indices.AddRange(letterPositions.Where((_, i) => i % 2 == 0));
                    indices.AddRange(letterPositions.Where((_, i) => i % 2 == 1));
                    break;
                    
                case FadePattern.Sequential:
                    // Left to right
                    indices.AddRange(letterPositions);
                    break;
                    
                case FadePattern.CenterCollapse:
                    // Outside to center
                    int left = 0, right = letterPositions.Count - 1;
                    while (left <= right)
                    {
                        if (left == right)
                        {
                            indices.Add(letterPositions[left]);
                        }
                        else
                        {
                            indices.Add(letterPositions[left]);
                            indices.Add(letterPositions[right]);
                        }
                        left++;
                        right--;
                    }
                    break;
                    
                case FadePattern.CenterExpand:
                    // Center to outside
                    int mid = letterPositions.Count / 2;
                    indices.Add(letterPositions[mid]);
                    for (int offset = 1; offset <= mid; offset++)
                    {
                        if (mid - offset >= 0)
                            indices.Add(letterPositions[mid - offset]);
                        if (mid + offset < letterPositions.Count)
                            indices.Add(letterPositions[mid + offset]);
                    }
                    break;
                    
                case FadePattern.Random:
                    // Random order
                    var random = new Random();
                    indices = letterPositions.OrderBy(_ => random.Next()).ToList();
                    break;
                    
                case FadePattern.Uniform:
                    // All at once
                    indices.AddRange(letterPositions);
                    break;
            }
            
            return indices;
        }

        /// <summary>
        /// Builds a single animation frame
        /// </summary>
        private static string BuildFrame(string text, List<int> letterSequence, int frame, List<char> colorSteps, FadeConfig config)
        {
            // Calculate how many letters should be affected in this frame
            int lettersPerFrame = Math.Max(1, letterSequence.Count / config.TotalFrames);
            int affectedLetters = Math.Min((frame + 1) * lettersPerFrame, letterSequence.Count);
            
            // Create character array for this frame
            var frameChars = new char[text.Length];
            var frameColors = new char?[text.Length];
            
            for (int i = 0; i < text.Length; i++)
            {
                frameChars[i] = text[i];
                frameColors[i] = null;
            }
            
            // Apply fade effect to affected letters
            for (int i = 0; i < affectedLetters; i++)
            {
                int charIndex = letterSequence[i];
                
                // Calculate fade stage for this letter
                int fadeStage = frame - (i / lettersPerFrame);
                if (fadeStage < 0) fadeStage = 0;
                if (fadeStage >= colorSteps.Count) fadeStage = colorSteps.Count - 1;
                
                frameColors[charIndex] = colorSteps[fadeStage];
            }
            
            // Build frame string with color markup
            return BuildColoredString(frameChars, frameColors);
        }

        /// <summary>
        /// Builds a colored string from character and color arrays
        /// </summary>
        private static string BuildColoredString(char[] chars, char?[] colors)
        {
            var result = new System.Text.StringBuilder();
            char? currentColor = null;
            
            for (int i = 0; i < chars.Length; i++)
            {
                // Change color if needed
                if (colors[i] != currentColor && colors[i].HasValue)
                {
                    result.Append($"&{colors[i]}");
                    currentColor = colors[i];
                }
                
                result.Append(chars[i]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Displays the fade animation
        /// </summary>
        private static void DisplayFadeAnimation(List<string> frames, FadeConfig config)
        {
            int cursorTop = Console.CursorTop;
            int cursorLeft = Console.CursorLeft;
            
            foreach (var frame in frames)
            {
                // Move cursor to start of animation
                Console.SetCursorPosition(cursorLeft, cursorTop);
                
                // Clear the line
                Console.Write(new string(' ', Console.WindowWidth - cursorLeft));
                Console.SetCursorPosition(cursorLeft, cursorTop);
                
                // Write frame
                ColoredConsoleWriter.Write(frame);
                
                // Delay before next frame
                Thread.Sleep(config.FrameDelayMs);
            }
            
            // Clear line after animation if configured
            if (config.ClearAfterFade)
            {
                Console.SetCursorPosition(cursorLeft, cursorTop);
                Console.Write(new string(' ', Console.WindowWidth - cursorLeft));
                Console.SetCursorPosition(cursorLeft, cursorTop);
            }
            else
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Quick fade with default alternating pattern
        /// </summary>
        public static void FadeOutAlternating(string text, int frames = 5, int delayMs = 100)
        {
            FadeOut(text, new FadeConfig
            {
                Pattern = FadePattern.Alternating,
                Direction = FadeDirection.ToDark,
                TotalFrames = frames,
                FrameDelayMs = delayMs
            });
        }

        /// <summary>
        /// Fade using a color template
        /// </summary>
        public static void FadeOutWithTemplate(string text, string templateName, FadePattern pattern = FadePattern.Sequential)
        {
            FadeOut(text, new FadeConfig
            {
                Pattern = pattern,
                Direction = FadeDirection.TemplateProgression,
                ColorTemplate = templateName,
                TotalFrames = 6,
                FrameDelayMs = 100
            });
        }

        /// <summary>
        /// Fade with custom color progression
        /// </summary>
        public static void FadeOutCustom(string text, List<char> colorProgression, FadePattern pattern = FadePattern.Alternating)
        {
            var config = new FadeConfig
            {
                Pattern = pattern,
                Direction = FadeDirection.TemplateProgression,
                TotalFrames = colorProgression.Count,
                FrameDelayMs = 100
            };
            
            // Generate frames using custom color progression
            var frames = new List<string>();
            var segments = ColoredTextParser.Parse(text);
            string plainText = ColoredTextRenderer.RenderAsPlainText(segments);
            var letterSequence = GenerateLetterSequence(plainText, pattern);
            
            for (int frame = 0; frame < config.TotalFrames; frame++)
            {
                var frameText = BuildFrame(plainText, letterSequence, frame, colorProgression, config);
                frames.Add(frameText);
            }
            
            DisplayFadeAnimation(frames, config);
        }
    }
}
