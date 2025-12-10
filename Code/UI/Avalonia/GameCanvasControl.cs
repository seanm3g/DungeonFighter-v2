using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Canvas control for rendering game UI elements.
    /// Facade coordinator that delegates to specialized canvas components.
    /// </summary>
    public class GameCanvasControl : Control
    {
        // Specialized canvas components using composition pattern
        private readonly CanvasElementManager elementManager;
        private readonly CanvasCoordinateConverter coordinateConverter;
        private readonly CanvasRenderer renderer;
        
        // Base grid dimensions (original design size)
        private const int BASE_GRID_WIDTH = 210;
        private const int BASE_GRID_HEIGHT = 52;
        
        // Grid properties (now calculated dynamically)
        private int _gridWidth = BASE_GRID_WIDTH;
        private int _gridHeight = BASE_GRID_HEIGHT;
        
        public int GridWidth 
        { 
            get => _gridWidth;
            private set
            {
                if (_gridWidth != value)
                {
                    _gridWidth = value;
                    UpdateLayoutConstants();
                }
            }
        }
        
        public int GridHeight 
        { 
            get => _gridHeight;
            private set
            {
                if (_gridHeight != value)
                {
                    _gridHeight = value;
                    UpdateLayoutConstants();
                }
            }
        }
        
        public int ViewportWidth => GridWidth;
        public int ViewportHeight => GridHeight;
        
        // Center point (half of GridWidth)
        public int CenterX => GridWidth / 2;

        public GameCanvasControl()
        {
            // Enable focus for keyboard input
            Focusable = true;
            
            // Initialize specialized components
            this.coordinateConverter = new CanvasCoordinateConverter();
            this.elementManager = new CanvasElementManager();
            this.renderer = new CanvasRenderer(coordinateConverter);
            
            // Initialize LayoutConstants with default dimensions
            UpdateLayoutConstants();
        }
        
        /// <summary>
        /// Updates LayoutConstants with current grid dimensions
        /// </summary>
        private void UpdateLayoutConstants()
        {
            LayoutConstants.UpdateGridDimensions(GridWidth, GridHeight);
        }

        /// <summary>
        /// Calculates scale factor and grid dimensions based on available space
        /// Keeps grid at base size but scales character pixels to fit available space
        /// </summary>
        private void CalculateScaleAndGrid(double availableWidth, double availableHeight)
        {
            // Always use base grid dimensions
            GridWidth = BASE_GRID_WIDTH;
            GridHeight = BASE_GRID_HEIGHT;
            
            // Calculate scale factor based on available space
            if (availableWidth > 0 && availableHeight > 0 && 
                availableWidth != double.PositiveInfinity && availableHeight != double.PositiveInfinity)
            {
                // Temporarily set scale to 1.0 to measure base character size
                coordinateConverter.SetScaleFactor(1.0);
                coordinateConverter.EnsureCharWidthMeasured();
                double baseCharWidth = coordinateConverter.GetCharWidth();
                double baseCharHeight = coordinateConverter.GetCharHeight();
                
                // Calculate required size for base grid at base scale
                double requiredWidth = BASE_GRID_WIDTH * baseCharWidth;
                double requiredHeight = BASE_GRID_HEIGHT * baseCharHeight;
                
                // Calculate scale factors for width and height
                double scaleX = availableWidth / requiredWidth;
                double scaleY = availableHeight / requiredHeight;
                
                // Use the smaller scale to ensure everything fits (maintains aspect ratio)
                double scaleFactor = Math.Min(scaleX, scaleY);
                
                // Apply minimum scale of 1.0 and maximum reasonable scale
                scaleFactor = Math.Max(1.0, Math.Min(scaleFactor, 10.0));
                
                // Update the coordinate converter with the calculated scale factor
                // This will reset charWidth/charHeight so they'll be remeasured with new scale
                coordinateConverter.SetScaleFactor(scaleFactor);
            }
            else
            {
                // Use base scale if no size available
                coordinateConverter.SetScaleFactor(1.0);
            }
        }

        /// <summary>
        /// Measures the control size and calculates scale factor based on available space
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            CalculateScaleAndGrid(availableSize.Width, availableSize.Height);
            
            coordinateConverter.EnsureCharWidthMeasured();
            
            // Calculate size based on grid dimensions and scaled character size
            double width = GridWidth * coordinateConverter.GetCharWidth();
            double height = GridHeight * coordinateConverter.GetCharHeight();
            
            return new Size(width, height);
        }
        
        /// <summary>
        /// Called when the control is arranged - recalculates scale if size changed
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Calculate scale based on available space
            CalculateScaleAndGrid(finalSize.Width, finalSize.Height);
            
            coordinateConverter.EnsureCharWidthMeasured();
            
            // Calculate actual canvas size we need
            double canvasWidth = GridWidth * coordinateConverter.GetCharWidth();
            double canvasHeight = GridHeight * coordinateConverter.GetCharHeight();
            
            // Return the size we actually use (parent will center via alignment)
            return new Size(canvasWidth, canvasHeight);
        }

        /// <summary>
        /// Exposes character width for coordinate conversion
        /// </summary>
        public double GetCharWidth()
        {
            return coordinateConverter.GetCharWidth();
        }

        /// <summary>
        /// Exposes character height for coordinate conversion
        /// </summary>
        public double GetCharHeight()
        {
            return coordinateConverter.GetCharHeight();
        }

        public override void Render(DrawingContext context)
        {
            // Render all elements using renderer
            renderer.Render(
                context,
                Bounds.Width,
                Bounds.Height,
                elementManager.TextElements.ToList(),
                elementManager.BoxElements.ToList(),
                elementManager.ProgressBars.ToList());
        }

        // Public methods for adding elements
        public void Clear()
        {
            elementManager.Clear();
        }
        
        /// <summary>
        /// Clears text elements within a specific Y range (inclusive)
        /// Clears ALL text in the Y range across the entire canvas width
        /// This is the standard method for clearing panels/areas - always clears full width
        /// </summary>
        public void ClearTextInRange(int startY, int endY)
        {
            elementManager.ClearTextInRange(startY, endY);
        }
        
        /// <summary>
        /// Clears text elements within a specific rectangular area (inclusive)
        /// Use this only when you need to clear a specific rectangular region (e.g., center panel only)
        /// For clearing full-width panels, use ClearTextInRange() instead
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearTextInArea(startX, startY, width, height);
        }
        
        /// <summary>
        /// Clears progress bars within a specific rectangular area (inclusive)
        /// Used to clear specific areas of the canvas without affecting other panels
        /// </summary>
        public void ClearProgressBarsInArea(int startX, int startY, int width, int height)
        {
            elementManager.ClearProgressBarsInArea(startX, startY, width, height);
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
            elementManager.RemoveText(t => t.X == x && t.Y == y);
            
            // Check for adjacent text elements on the same line that can be merged (same color)
            // This prevents gaps between adjacent segments
            var adjacentText = elementManager.GetFirstText(t => 
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
            var nearbyText = elementManager.GetFirstText(t => 
                t.Y == y && 
                Math.Abs(t.X - x) <= 1 && 
                t.X != x);
            
            if (nearbyText != null)
            {
                // If same color, we could merge, but for now just skip
            }
            
            elementManager.AddText(new CanvasText { X = x, Y = y, Content = text, Color = color });
        }
        
        /// <summary>
        /// Adds text to the canvas at the specified position without removing existing text
        /// This is used for rendering multi-segment colored text where segments may round to the same position
        /// </summary>
        public void AppendText(int x, int y, string text, Color color)
        {
            // Find existing text at this position and append to it, or create new
            var existing = elementManager.GetFirstText(t => t.X == x && t.Y == y);
            if (existing != null && existing.Color == color)
            {
                // Append to existing text with same color
                existing.Content += text;
            }
            else
            {
                // Create new text element
                elementManager.AddText(new CanvasText { X = x, Y = y, Content = text, Color = color });
            }
        }
        
        /// <summary>
        /// Measures the actual pixel width of text using FormattedText
        /// Returns the width in character units (pixels / charWidth)
        /// </summary>
        public double MeasureTextWidth(string text)
        {
            return coordinateConverter.MeasureTextWidth(text);
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
            elementManager.AddBox(new CanvasBox 
            { 
                X = x, Y = y, Width = width, Height = height, 
                BorderColor = borderColor, BackgroundColor = backgroundColor 
            });
        }

        public void AddProgressBar(int x, int y, int width, double progress, Color foregroundColor, Color backgroundColor, Color borderColor)
        {
            elementManager.AddProgressBar(new CanvasProgressBar
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
