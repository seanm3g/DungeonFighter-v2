using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
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
        public int GridHeight { get; set; } = 60;
        public int ViewportWidth { get; set; } = 210;
        public int ViewportHeight { get; set; } = 60;
        
        // Center point (half of GridWidth)
        public int CenterX => GridWidth / 2;  // = 105 for default 210-wide screen
        
        // Font properties
        private readonly Typeface typeface = new("Consolas, Courier New, monospace");
        private const double fontSize = 14;
        private const double charWidth = 9; // Width for monospace
        private const double charHeight = 16; // Approximate height for monospace

        public GameCanvasControl()
        {
            // Enable focus for keyboard input
            Focusable = true;
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
            double x = text.X * charWidth;
            double y = text.Y * charHeight;

            // Create FormattedText with MaxTextWidth set to prevent letter-spacing issues
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

        public void AddText(int x, int y, string text, Color color)
        {
            textElements.Add(new CanvasText { X = x, Y = y, Content = text, Color = color });
        }
        
        /// <summary>
        /// Alias for AddText for compatibility
        /// </summary>
        public void DrawText(string text, int x, int y, Color color)
        {
            AddText(x, y, text, color);
        }
        
        /// <summary>
        /// Measures the actual pixel width of text using FormattedText
        /// Returns the width in character units (pixels / charWidth)
        /// </summary>
        public double MeasureTextWidth(string text)
        {
            var formatted = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.White
            );
            return formatted.Width / charWidth;
        }

        public void AddText(int x, int y, string text, Color color, bool centered = false)
        {
            if (centered)
            {
                // Use CenterX (105 for 210-wide screen) as the center point
                // Use GetDisplayLength to exclude color markup characters from the length calculation
                x = CenterX - (ColorParser.GetDisplayLength(text) / 2);
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
