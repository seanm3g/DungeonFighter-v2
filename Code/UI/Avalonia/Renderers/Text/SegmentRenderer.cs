using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.SegmentRenderers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Renderers.Text
{

    /// <summary>
    /// Handles segment rendering with Strategy pattern for template vs standard rendering.
    /// Applies animation to critical hit lines similar to dungeon selection screen.
    /// </summary>
    public class SegmentRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ISegmentRenderer templateRenderer;
        private readonly ISegmentRenderer standardRenderer;
        private readonly CritAnimationState critAnimationState;
        
        public SegmentRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.templateRenderer = new TemplateSegmentRenderer(canvas);
            this.standardRenderer = new StandardSegmentRenderer(canvas);
            this.critAnimationState = CritAnimationState.Instance;
        }
        
        /// <summary>
        /// Renders a list of colored text segments to the canvas.
        /// Uses Strategy pattern to select appropriate renderer (template vs standard).
        /// Applies animation to critical hit lines.
        /// </summary>
        /// <param name="legacyColorConverter">Unused; segment colors use <see cref="ColorConverter.ConvertSegmentToCanvasColor"/>.</param>
        public void RenderSegments(List<ColoredText> segments, int x, int y, Func<Color, Color> legacyColorConverter)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            _ = legacyColorConverter;
            // Check if this is a critical hit ACTION line (not a status effect line)
            // Must contain "CRITICAL" AND "hits" AND "damage" to be an action line
            // This excludes status effect lines like "affected by poison" which should not be animated
            string fullText = string.Join("", segments.Where(s => s.Text != null).Select(s => s.Text));
            bool hasCritical = fullText.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase);
            bool hasHits = fullText.Contains("hits", StringComparison.OrdinalIgnoreCase);
            bool hasDamage = fullText.Contains("damage", StringComparison.OrdinalIgnoreCase);
            bool isCritActionLine = hasCritical && hasHits && hasDamage;
            
            if (isCritActionLine)
            {
                // Render with animation for crit action lines only
                RenderCritLineWithAnimation(segments, x, y);
            }
            else
            {
                // Normal rendering for non-crit lines and status effect lines
                RenderSegmentsNormal(segments, x, y);
            }
        }
        
        /// <summary>
        /// Renders a critical hit line with animation effects
        /// Only applies undulation animation to the "CRITICAL *ACTION NAME*" segment, not the full line
        /// </summary>
        private void RenderCritLineWithAnimation(List<ColoredText> segments, int x, int y)
        {
            int currentX = x;
            int charPosition = 0;
            
            // Generate a small random offset for this animated element based on line position
            // This makes each animated element look different from others
            int elementOffset = GetElementRandomOffset(y);
            
            // Select appropriate renderer for non-animated segments
            ISegmentRenderer standardRenderer = templateRenderer.ShouldUseRenderer(segments) 
                ? templateRenderer 
                : this.standardRenderer;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Check if this segment contains "CRITICAL" - only animate the action name segment
                bool isCriticalSegment = segment.Text.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase);
                
                if (isCriticalSegment)
                {
                    var baseColor = ColorConverter.ConvertSegmentToCanvasColor(segment);
                    var critSegment = new ColoredText(segment.Text, baseColor, segment.SourceTemplate);
                    var animated = TextAnimationCompositor.Compose(
                        "critLine",
                        baseSegments: new List<ColoredText> { critSegment },
                        charStartIndex: charPosition,
                        lineOffset: y,
                        animationState: critAnimationState,
                        elementOffset: elementOffset);

                    foreach (var animatedChar in animated)
                    {
                        canvas.AddText(currentX, y, animatedChar.Text, animatedChar.Color);
                        currentX++;
                        charPosition++;
                    }
                }
                else
                {
                    // Render non-critical segments normally (attacker name, "hits", target name, damage, etc.)
                    var canvasColor = ColorConverter.ConvertSegmentToCanvasColor(segment);
                    int renderedX = int.MinValue;
                    
                    double newXDouble = standardRenderer.RenderSegment(segment, canvasColor, currentX, 
                        int.MinValue, null, y, ref renderedX);
                    
                    currentX = (int)System.Math.Round(newXDouble);
                    
                    // Update character position based on segment length
                    charPosition += segment.Text.Length;
                }
            }
        }
        
        /// <summary>
        /// Generates a small random offset for an animated element based on its line position
        /// Uses a simple hash to ensure the same line always gets the same offset
        /// Returns a value between -50 and +50 to create visual variation
        /// </summary>
        private int GetElementRandomOffset(int lineOffset)
        {
            // Use a simple hash function to generate a consistent but varied offset
            // Multiply by a prime number and use modulo to get a pseudo-random value
            int hash = (lineOffset * 7919 + 12345) % 101; // Prime number 7919, offset 12345, modulo 101 gives -50 to +50 range
            return hash - 50; // Shift to -50 to +50 range
        }
        
        /// <summary>
        /// Renders segments normally without animation
        /// </summary>
        private void RenderSegmentsNormal(List<ColoredText> segments, int x, int y)
        {
            // Select appropriate renderer using Strategy pattern
            ISegmentRenderer renderer = templateRenderer.ShouldUseRenderer(segments) 
                ? templateRenderer 
                : standardRenderer;
            
            // Use integer X positions throughout (character grid system)
            // No floating point needed - monospace font means 1 char = 1 position
            int currentX = x;
            int lastRenderedX = int.MinValue;
            Color? lastColor = null;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                var canvasColor = ColorConverter.ConvertSegmentToCanvasColor(segment);
                int renderedX = int.MinValue;
                
                // Use selected renderer to render segment
                // Renderer returns double for interface compatibility, but value is always integer
                double newXDouble = renderer.RenderSegment(segment, canvasColor, currentX, 
                    lastRenderedX, lastColor, y, ref renderedX);
                
                // Convert back to int (should always be exact since renderer uses character count)
                currentX = (int)System.Math.Round(newXDouble);
                
                if (renderedX != int.MinValue)
                {
                    lastRenderedX = renderedX;
                }
                lastColor = canvasColor;
            }
        }
    }
}

