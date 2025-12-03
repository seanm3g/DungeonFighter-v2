using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia
{
    public class GameCanvasControl : Control
    {
        private readonly List<CanvasText> textElements = new();
        private readonly List<CanvasBox> boxElements = new();
        private readonly List<CanvasProgressBar> progressBars = new();
        
        // Grid properties
        public int GridWidth { get; set; } = 210;
        public int GridHeight { get; set; } = 52;  // Reduced from 60 to match panel height adjustment
        public int ViewportWidth { get; set; } = 210;
        public int ViewportHeight { get; set; } = 52;  // Reduced from 60 to match panel height adjustment
        
        // Center point (half of GridWidth)
        public int CenterX => GridWidth / 2;  // = 105 for default 210-wide screen
        
        // Font properties - using explicit monospace font
        // "Courier New" is a guaranteed monospace font available on all systems
        // It's a serif, typewriter-style monospace font
        private readonly Typeface typeface = new("Courier New");
        private const double fontSize = 16; // Increased from 14 for better readability
        private double charWidth; // Actual measured width of a character
        private double charHeight; // Actual measured height of a character
        
        // Measure actual character width and height on first use
        private void EnsureCharWidthMeasured()
        {
            if (charWidth == 0)
            {
                // Measure actual width and height of a single character in the monospace font
                var testText = new FormattedText(
                    "M", // Use 'M' as it's typically the widest character
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.White
                )
                {
                    MaxTextWidth = double.PositiveInfinity,
                    MaxTextHeight = double.PositiveInfinity,
                    Trimming = TextTrimming.None
                };
                charWidth = testText.Width;
                charHeight = testText.Height;
            }
        }

        public GameCanvasControl()
        {
            // Enable focus for keyboard input
            Focusable = true;
        }

        /// <summary>
        /// Measures the control size based on grid dimensions and character size
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            EnsureCharWidthMeasured();
            
            // Calculate size based on grid dimensions and measured character size
            double width = GridWidth * charWidth;
            double height = GridHeight * charHeight;
            
            return new Size(width, height);
        }

        /// <summary>
        /// Exposes character width for coordinate conversion
        /// </summary>
        public double GetCharWidth()
        {
            EnsureCharWidthMeasured();
            return charWidth;
        }

        /// <summary>
        /// Exposes character height for coordinate conversion
        /// </summary>
        public double GetCharHeight()
        {
            EnsureCharWidthMeasured();
            return charHeight;
        }

        public override void Render(DrawingContext context)
        {
            // Clear the canvas
            context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));

            // Render all elements
            RenderBoxes(context);
            RenderProgressBars(context);
            RenderText(context);
        }

        private void RenderBoxes(DrawingContext context)
        {
            foreach (var box in boxElements)
            {
                RenderBox(context, box);
            }
        }

        private void RenderBox(DrawingContext context, CanvasBox box)
        {
            double x = box.X * charWidth;
            double y = box.Y * charHeight;
            double width = box.Width * charWidth;
            double height = box.Height * charHeight;

            // Draw border
            var pen = new Pen(new SolidColorBrush(box.BorderColor), 1);
            context.DrawRectangle(null, pen, new Rect(x, y, width, height));

            // Fill background if specified
            if (box.BackgroundColor != Colors.Transparent)
            {
                context.FillRectangle(new SolidColorBrush(box.BackgroundColor), new Rect(x, y, width, height));
            }
        }

        private void RenderProgressBars(DrawingContext context)
        {
            foreach (var progressBar in progressBars)
            {
                RenderProgressBar(context, progressBar);
            }
        }

        private void RenderProgressBar(DrawingContext context, CanvasProgressBar progressBar)
        {
            double x = progressBar.X * charWidth;
            double y = progressBar.Y * charHeight;
            double width = progressBar.Width * charWidth;
            double height = charHeight;

            // Background
            context.FillRectangle(new SolidColorBrush(progressBar.BackgroundColor), new Rect(x, y, width, height));

            // Progress
            double progressWidth = width * progressBar.Progress;
            context.FillRectangle(new SolidColorBrush(progressBar.ForegroundColor), new Rect(x, y, progressWidth, height));

            // Border
            var pen = new Pen(new SolidColorBrush(progressBar.BorderColor), 1);
            context.DrawRectangle(null, pen, new Rect(x, y, width, height));
        }

        private void RenderText(DrawingContext context)
        {
            foreach (var text in textElements)
            {
                RenderText(context, text);
            }
        }

        private void RenderText(DrawingContext context, CanvasText text)
        {
            EnsureCharWidthMeasured();
            
            double x = text.X * charWidth;
            double y = text.Y * charHeight;

            // Create FormattedText with MaxTextWidth set to prevent letter-spacing issues
            // Using explicit monospace font (Courier New) ensures consistent character widths
            var formatted = new FormattedText(
                text.Content,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                new SolidColorBrush(text.Color)
            )
            {
                // Prevent text wrapping which can add extra spacing
                MaxTextWidth = double.PositiveInfinity,
                MaxTextHeight = double.PositiveInfinity,
                // Ensure no letter-spacing is applied
                Trimming = TextTrimming.None
            };

            context.DrawText(formatted, new Point(x, y));
        }

        // Public methods for adding elements
        public void Clear()
        {
            textElements.Clear();
            boxElements.Clear();
            progressBars.Clear();
        }
        
        /// <summary>
        /// Clears text elements within a specific Y range (inclusive)
        /// Clears ALL text in the Y range across the entire canvas width
        /// This is the standard method for clearing panels/areas - always clears full width
        /// </summary>
        public void ClearTextInRange(int startY, int endY)
        {
            textElements.RemoveAll(text => text.Y >= startY && text.Y <= endY);
        }
        
        /// <summary>
        /// Clears text elements within a specific rectangular area (inclusive)
        /// Use this only when you need to clear a specific rectangular region (e.g., center panel only)
        /// For clearing full-width panels, use ClearTextInRange() instead
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            textElements.RemoveAll(text => 
                text.X >= startX && text.X < endX && 
                text.Y >= startY && text.Y < endY);
        }
        
        /// <summary>
        /// Clears progress bars within a specific rectangular area (inclusive)
        /// Used to clear specific areas of the canvas without affecting other panels
        /// </summary>
        public void ClearProgressBarsInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            progressBars.RemoveAll(bar => 
                bar.X >= startX && bar.X < endX && 
                bar.Y >= startY && bar.Y < endY);
        }

        /// <summary>
        /// Adds text to the canvas at the specified position
        /// Automatically removes any existing text at the exact same position to prevent overlap
        /// Enhanced to detect and handle near-overlaps (adjacent positions)
        /// Merges adjacent text elements on the same line with the same color to prevent gaps
        /// </summary>
        public void AddText(int x, int y, string text, Color color)
        {
            // Remove any existing text at the exact same position to prevent overlap
            // This is especially important for animated content like the title screen
            textElements.RemoveAll(t => t.X == x && t.Y == y);
            
            // Check for adjacent text elements on the same line that can be merged (same color)
            // This prevents gaps between adjacent segments
            var adjacentText = textElements.FirstOrDefault(t => 
                t.Y == y && 
                t.Color == color &&
                (t.X + t.Content.Length == x || x + text.Length == t.X));
            
            if (adjacentText != null)
            {
                // Merge with adjacent text element
                if (adjacentText.X + adjacentText.Content.Length == x)
                {
                    // Current text comes after adjacent text - append to it
                    adjacentText.Content += text;
                    return;
                }
                else if (x + text.Length == adjacentText.X)
                {
                    // Current text comes before adjacent text - prepend to it
                    adjacentText.Content = text + adjacentText.Content;
                    adjacentText.X = x;
                    return;
                }
            }
            
            // Check for potential overlaps at adjacent positions (within 1 character)
            // This helps catch rounding issues that might cause near-overlaps
            var nearbyText = textElements.FirstOrDefault(t => 
                t.Y == y && 
                Math.Abs(t.X - x) <= 1 && 
                t.X != x);
            
            if (nearbyText != null)
            {
                // If same color, we could merge, but for now just skip
            }
            
            textElements.Add(new CanvasText { X = x, Y = y, Content = text, Color = color });
        }
        
        /// <summary>
        /// Adds text to the canvas at the specified position without removing existing text
        /// This is used for rendering multi-segment colored text where segments may round to the same position
        /// </summary>
        public void AppendText(int x, int y, string text, Color color)
        {
            // Find existing text at this position and append to it, or create new
            var existing = textElements.FirstOrDefault(t => t.X == x && t.Y == y);
            if (existing != null && existing.Color == color)
            {
                // Append to existing text with same color
                existing.Content += text;
            }
            else
            {
                // Create new text element
                textElements.Add(new CanvasText { X = x, Y = y, Content = text, Color = color });
            }
        }
        
        /// <summary>
        /// Measures the actual pixel width of text using FormattedText
        /// Returns the width in character units (pixels / charWidth)
        /// </summary>
        public double MeasureTextWidth(string text)
        {
            EnsureCharWidthMeasured();
            
            var formatted = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.White
            )
            {
                MaxTextWidth = double.PositiveInfinity,
                MaxTextHeight = double.PositiveInfinity,
                Trimming = TextTrimming.None
            };
            return formatted.Width / charWidth;
        }

        public void AddText(int x, int y, string text, Color color, bool centered = false)
        {
            if (centered)
            {
                // Use CenterX (105 for 210-wide screen) as the center point
                // Use GetDisplayLength to exclude color markup characters from the length calculation
                var segments = ColoredTextParser.Parse(text);
                int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
                x = CenterX - (displayLength / 2);
            }
            AddText(x, y, text, color);
        }

        public void AddBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor = default)
        {
            if (backgroundColor == default) backgroundColor = Colors.Transparent;
            boxElements.Add(new CanvasBox 
            { 
                X = x, Y = y, Width = width, Height = height, 
                BorderColor = borderColor, BackgroundColor = backgroundColor 
            });
        }

        public void AddProgressBar(int x, int y, int width, double progress, Color foregroundColor, Color backgroundColor, Color borderColor)
        {
            progressBars.Add(new CanvasProgressBar
            {
                X = x, Y = y, Width = width, Progress = progress,
                ForegroundColor = foregroundColor, BackgroundColor = backgroundColor, BorderColor = borderColor
            });
        }

        public void AddBorder(int x, int y, int width, int height, Color color)
        {
            AddBox(x, y, width, height, color);
        }

        public void AddCenteredText(int y, string text, Color color)
        {
            AddText(0, y, text, color, true);
        }

        public void AddTitle(int y, string title, Color color = default)
        {
            if (color == default) color = Colors.White;
            AddCenteredText(y, $" {title} ", color);
        }

        public void AddMenuOption(int x, int y, int number, string option, Color color = default, bool isHovered = false)
        {
            if (color == default) color = Colors.White;
            
            // Change color if hovered
            if (isHovered)
            {
                color = Colors.Yellow; // Highlight hovered items
            }
            
            // Render the bracketed number in the specified color
            AddText(x, y, $"[{number}]", color);
            
            // Render the option text in white (or yellow if hovered)
            Color textColor = isHovered ? Colors.Yellow : Colors.White;
            AddText(x + $"[{number}]".Length + 1, y, option, textColor);
        }

        public void AddItem(int x, int y, int number, string itemName, string stats, Color nameColor = default, Color statsColor = default, bool isHovered = false)
        {
            if (nameColor == default) nameColor = Colors.White;
            if (statsColor == default) statsColor = Colors.Gray;
            
            // Change colors if hovered
            if (isHovered)
            {
                nameColor = Colors.Yellow;
                statsColor = Colors.Yellow;
            }
            
            // Render the bracketed number in the specified color
            AddText(x, y, $"[{number}]", nameColor);
            
            // Render the item name in white (or yellow if hovered)
            Color itemTextColor = isHovered ? Colors.Yellow : Colors.White;
            AddText(x + $"[{number}]".Length + 1, y, itemName, itemTextColor);
            
            if (!string.IsNullOrEmpty(stats))
            {
                AddText(x + 2, y + 1, $"    {stats}", statsColor);
            }
        }

        public void AddCharacterStat(int x, int y, string statName, int value, int maxValue, Color nameColor = default, Color valueColor = default)
        {
            if (nameColor == default) nameColor = Colors.White;
            if (valueColor == default) valueColor = Colors.Yellow;
            
            // Only show maxValue if it's greater than 0
            string statText = maxValue > 0 ? $"{statName}: {value}/{maxValue}" : $"{statName}: {value}";
            AddText(x, y, statText, nameColor);
        }

        public void AddHealthBar(int x, int y, int width, int currentHealth, int maxHealth, Color healthColor = default, Color backgroundColor = default)
        {
            if (healthColor == default) healthColor = Colors.Red;
            if (backgroundColor == default) backgroundColor = Colors.DarkRed;
            
            double progress = (double)currentHealth / maxHealth;
            AddProgressBar(x, y, width, progress, healthColor, backgroundColor, Colors.White);
        }

        public void Refresh()
        {
            InvalidateVisual();
        }
    }

    // Helper classes for canvas elements
    public class CanvasText
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Content { get; set; } = "";
        public Color Color { get; set; } = Colors.White;
    }

    public class CanvasBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Color BorderColor { get; set; } = Colors.White;
        public Color BackgroundColor { get; set; } = Colors.Transparent;
    }

    public class CanvasProgressBar
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public double Progress { get; set; }
        public Color ForegroundColor { get; set; } = Colors.Green;
        public Color BackgroundColor { get; set; } = Colors.DarkGreen;
        public Color BorderColor { get; set; } = Colors.White;
    }
}
