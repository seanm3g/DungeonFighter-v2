using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
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
    /// Refactored to separate control lifecycle from rendering logic.
    /// </summary>
    public class GameCanvasControl : Control
    {
        // Specialized canvas components using composition pattern
        private readonly CanvasElementManager elementManager;
        private readonly CanvasCoordinateConverter coordinateConverter;
        private readonly CanvasRenderer renderer;
        private readonly HealthTracker healthTracker;
        private readonly CanvasElementBuilder elementBuilder;
        private DispatcherTimer? damageDeltaAnimationTimer;
        
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
            Focusable = true;
            // Background and other properties are set in XAML to Transparent so control can receive input events
            // Controls without background are invisible to hit testing in Avalonia
            this.coordinateConverter = new CanvasCoordinateConverter();
            this.elementManager = new CanvasElementManager();
            this.renderer = new CanvasRenderer(coordinateConverter);
            this.healthTracker = new HealthTracker();
            this.elementBuilder = new CanvasElementBuilder(elementManager, healthTracker, CenterX);
            InitializeDamageDeltaTimer();
            UpdateLayoutConstants();
        }
        
        /// <summary>
        /// Initializes the timer for damage delta fade animations
        /// </summary>
        private void InitializeDamageDeltaTimer()
        {
            damageDeltaAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps for smooth animation
            };
            damageDeltaAnimationTimer.Tick += (s, e) =>
            {
                // Check if health tracker has any active damage deltas
                if (healthTracker.HasActiveDamageDeltas())
                {
                    Refresh();
                }
                else
                {
                    // Stop timer when no active deltas
                    damageDeltaAnimationTimer.Stop();
                }
            };
        }
        
        /// <summary>
        /// Gets the health tracker for tracking previous health values
        /// </summary>
        public HealthTracker HealthTracker => healthTracker;
        
        /// <summary>
        /// Updates LayoutConstants with current grid dimensions and effective visible width
        /// </summary>
        private void UpdateLayoutConstants()
        {
            LayoutConstants.UpdateGridDimensions(GridWidth, GridHeight);
            UpdateEffectiveVisibleWidth();
        }
        
        /// <summary>
        /// Updates effective visible width based on actual canvas bounds
        /// </summary>
        private void UpdateEffectiveVisibleWidth()
        {
            if (Bounds.Width > 0 && coordinateConverter != null)
            {
                coordinateConverter.EnsureCharWidthMeasured();
                double charWidth = coordinateConverter.GetCharWidth();
                if (charWidth > 0)
                {
                    LayoutConstants.UpdateEffectiveVisibleWidth(Bounds.Width, charWidth);
                }
            }
        }

        /// <summary>
        /// Calculates scale factor and grid dimensions based on available space
        /// Scales font size proportionally when window is resized to make UI bigger when fullscreened
        /// </summary>
        private void CalculateScaleAndGrid(double availableWidth, double availableHeight)
        {
            // Keep base grid dimensions fixed
            GridWidth = BASE_GRID_WIDTH;
            GridHeight = BASE_GRID_HEIGHT;
            
            // Calculate scale factor and grid dimensions based on available space
            if (availableWidth > 0 && availableHeight > 0 && 
                availableWidth != double.PositiveInfinity && availableHeight != double.PositiveInfinity)
            {
                // Temporarily set scale to 1.0 to measure base character size
                coordinateConverter.SetScaleFactor(1.0);
                coordinateConverter.EnsureCharWidthMeasured();
                double baseCharWidth = coordinateConverter.GetCharWidth();
                double baseCharHeight = coordinateConverter.GetCharHeight();
                
                // Calculate required size for base grid at scale 1.0
                double requiredWidth = BASE_GRID_WIDTH * baseCharWidth;
                double requiredHeight = BASE_GRID_HEIGHT * baseCharHeight;
                
                // Calculate scale factors for width and height
                double scaleX = availableWidth / requiredWidth;
                double scaleY = availableHeight / requiredHeight;
                
                // Use the smaller scale to maintain aspect ratio and ensure everything fits
                // This will scale the font size up when window is larger
                double scaleFactor = Math.Min(scaleX, scaleY);
                
                // Apply minimum scale of 1.0 and allow scaling up significantly for fullscreen
                scaleFactor = Math.Max(1.0, Math.Min(scaleFactor, 20.0));
                
                // Update the coordinate converter with the calculated scale factor
                // This scales the font size, making all characters bigger
                coordinateConverter.SetScaleFactor(scaleFactor);
            }
            else
            {
                // Use base dimensions if no size available
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
            // Update effective visible width before rendering (bounds are now available)
            UpdateEffectiveVisibleWidth();
            
            // Don't call base.Render() to prevent any default Control rendering
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
        /// Gets the element builder for creating canvas elements with convenience methods
        /// </summary>
        public CanvasElementBuilder Builder => elementBuilder;

        /// <summary>
        /// Adds text to the canvas at the specified position
        /// Delegates to element builder for convenience
        /// </summary>
        public void AddText(int x, int y, string text, Color color)
        {
            elementBuilder.AddText(x, y, text, color);
        }

        /// <summary>
        /// Adds text with glow effect to the canvas at the specified position
        /// </summary>
        public void AddText(int x, int y, string text, Color color, Color glowColor, double glowIntensity = 0.5, int glowRadius = 3)
        {
            elementBuilder.AddText(x, y, text, color, glowColor, glowIntensity, glowRadius);
        }
        
        /// <summary>
        /// Adds text to the canvas at the specified position without removing existing text
        /// This is used for rendering multi-segment colored text where segments may round to the same position
        /// </summary>
        public void AppendText(int x, int y, string text, Color color)
        {
            elementBuilder.AppendText(x, y, text, color);
        }
        
        /// <summary>
        /// Measures the actual pixel width of text using FormattedText
        /// Returns the width in character units (pixels / charWidth)
        /// </summary>
        public double MeasureTextWidth(string text)
        {
            return coordinateConverter.MeasureTextWidth(text);
        }

        /// <summary>
        /// Adds text to the canvas, optionally centered
        /// </summary>
        public void AddText(int x, int y, string text, Color color, bool centered)
        {
            elementBuilder.AddText(x, y, text, color, centered);
        }

        /// <summary>
        /// Adds a box to the canvas
        /// </summary>
        public void AddBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor = default)
        {
            elementBuilder.AddBox(x, y, width, height, borderColor, backgroundColor);
        }

        /// <summary>
        /// Adds a progress bar to the canvas
        /// </summary>
        public void AddProgressBar(int x, int y, int width, double progress, Color foregroundColor, Color backgroundColor, Color borderColor)
        {
            elementBuilder.AddProgressBar(x, y, width, progress, foregroundColor, backgroundColor, borderColor);
        }

        /// <summary>
        /// Adds a border (box with no background) to the canvas
        /// </summary>
        public void AddBorder(int x, int y, int width, int height, Color color)
        {
            elementBuilder.AddBorder(x, y, width, height, color);
        }

        /// <summary>
        /// Adds centered text to the canvas
        /// </summary>
        public void AddCenteredText(int y, string text, Color color)
        {
            elementBuilder.AddCenteredText(y, text, color);
        }

        /// <summary>
        /// Adds a title (centered text with padding) to the canvas
        /// </summary>
        public void AddTitle(int y, string title, Color color = default)
        {
            elementBuilder.AddTitle(y, title, color);
        }

        /// <summary>
        /// Adds a menu option to the canvas
        /// </summary>
        public void AddMenuOption(int x, int y, int number, string option, Color color = default, bool isHovered = false)
        {
            elementBuilder.AddMenuOption(x, y, number, option, color, isHovered);
        }

        /// <summary>
        /// Adds an item display to the canvas
        /// </summary>
        public void AddItem(int x, int y, int number, string itemName, string stats, Color nameColor = default, Color statsColor = default, bool isHovered = false)
        {
            elementBuilder.AddItem(x, y, number, itemName, stats, nameColor, statsColor, isHovered);
        }

        /// <summary>
        /// Adds a character stat display to the canvas
        /// </summary>
        public void AddCharacterStat(int x, int y, string statName, int value, int maxValue, Color nameColor = default, Color valueColor = default)
        {
            elementBuilder.AddCharacterStat(x, y, statName, value, maxValue, nameColor, valueColor);
        }

        /// <summary>
        /// Adds a health bar to the canvas with damage delta animation support
        /// </summary>
        public void AddHealthBar(int x, int y, int width, int currentHealth, int maxHealth, Color healthColor = default, Color backgroundColor = default, string? entityId = null)
        {
            elementBuilder.AddHealthBar(x, y, width, currentHealth, maxHealth, healthColor, backgroundColor, entityId);
            
            // Start animation timer if damage delta is active
            if (!string.IsNullOrEmpty(entityId))
            {
                var damageDeltaStartTime = healthTracker.GetDamageDeltaStartTime(entityId);
                if (damageDeltaStartTime.HasValue && damageDeltaAnimationTimer != null && !damageDeltaAnimationTimer.IsEnabled)
                {
                    damageDeltaAnimationTimer.Start();
                }
            }
        }
        
        /// <summary>
        /// Cleans up resources when control is disposed
        /// </summary>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            damageDeltaAnimationTimer?.Stop();
            damageDeltaAnimationTimer = null;
            base.OnDetachedFromVisualTree(e);
        }

        public void Refresh()
        {
            InvalidateVisual();
        }
    }
}
