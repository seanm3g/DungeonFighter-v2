using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// Builder class for creating canvas elements with convenience methods.
    /// Extracted from GameCanvasControl to separate rendering logic from control lifecycle.
    /// </summary>
    public class CanvasElementBuilder
    {
        private readonly CanvasElementManager elementManager;
        private readonly HealthTracker healthTracker;
        private readonly int centerX;

        /// <summary>
        /// Initializes a new instance of CanvasElementBuilder
        /// </summary>
        /// <param name="elementManager">The element manager to add elements to</param>
        /// <param name="healthTracker">The health tracker for health bar animations</param>
        /// <param name="centerX">The center X coordinate for centering operations</param>
        public CanvasElementBuilder(CanvasElementManager elementManager, HealthTracker healthTracker, int centerX)
        {
            this.elementManager = elementManager ?? throw new System.ArgumentNullException(nameof(elementManager));
            this.healthTracker = healthTracker ?? throw new System.ArgumentNullException(nameof(healthTracker));
            this.centerX = centerX;
        }

        /// <summary>
        /// Adds text to the canvas at the specified position
        /// Automatically removes any existing text at the exact same position to prevent overlap
        /// Enhanced to detect and handle near-overlaps (adjacent positions)
        /// Merges adjacent text elements on the same line with the same color to prevent gaps
        /// </summary>
        public void AddText(int x, int y, string text, Color color)
        {
            elementManager.AddTextWithMerging(x, y, text, color);
        }

        /// <summary>
        /// Adds text with glow effect to the canvas at the specified position
        /// </summary>
        public void AddText(int x, int y, string text, Color color, Color glowColor, double glowIntensity = 0.5, int glowRadius = 3)
        {
            var textElement = new CanvasText
            {
                X = x,
                Y = y,
                Content = text,
                Color = color,
                HasGlow = true,
                GlowColor = glowColor,
                GlowIntensity = glowIntensity,
                GlowRadius = glowRadius
            };
            elementManager.AddText(textElement);
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
        /// Adds text to the canvas, optionally centered
        /// </summary>
        public void AddText(int x, int y, string text, Color color, bool centered)
        {
            if (centered)
            {
                // Use CenterX as the center point
                // Use GetDisplayLength to exclude color markup characters from the length calculation
                var segments = ColoredTextParser.Parse(text);
                int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
                x = centerX - (displayLength / 2);
            }
            AddText(x, y, text, color);
        }

        /// <summary>
        /// Adds a box to the canvas
        /// </summary>
        public void AddBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor = default)
        {
            if (backgroundColor == default) backgroundColor = Colors.Transparent;
            elementManager.AddBox(new CanvasBox 
            { 
                X = x, Y = y, Width = width, Height = height, 
                BorderColor = borderColor, BackgroundColor = backgroundColor 
            });
        }

        /// <summary>
        /// Adds a progress bar to the canvas
        /// </summary>
        public void AddProgressBar(int x, int y, int width, double progress, Color foregroundColor, Color backgroundColor, Color borderColor)
        {
            elementManager.AddProgressBar(new CanvasProgressBar
            {
                X = x, Y = y, Width = width, Progress = progress,
                ForegroundColor = foregroundColor, BackgroundColor = backgroundColor, BorderColor = borderColor
            });
        }

        /// <summary>
        /// Adds a border (box with no background) to the canvas
        /// </summary>
        public void AddBorder(int x, int y, int width, int height, Color color)
        {
            AddBox(x, y, width, height, color);
        }

        /// <summary>
        /// Adds centered text to the canvas
        /// </summary>
        public void AddCenteredText(int y, string text, Color color)
        {
            AddText(0, y, text, color, true);
        }

        /// <summary>
        /// Adds a title (centered text with padding) to the canvas
        /// </summary>
        public void AddTitle(int y, string title, Color color = default)
        {
            if (color == default) color = Colors.White;
            AddCenteredText(y, $" {title} ", color);
        }

        /// <summary>
        /// Adds a menu option to the canvas
        /// </summary>
        public void AddMenuOption(int x, int y, int number, string option, Color color = default, bool isHovered = false)
        {
            if (color == default) color = Colors.White;
            if (isHovered) color = Colors.Yellow;
            
            AddText(x, y, $"[{number}]", color);
            AddText(x + $"[{number}]".Length + 1, y, option, isHovered ? Colors.Yellow : Colors.White);
        }

        /// <summary>
        /// Adds an item display to the canvas
        /// </summary>
        public void AddItem(int x, int y, int number, string itemName, string stats, Color nameColor = default, Color statsColor = default, bool isHovered = false)
        {
            if (nameColor == default) nameColor = Colors.White;
            if (statsColor == default) statsColor = Colors.Gray;
            if (isHovered) nameColor = statsColor = Colors.Yellow;
            
            AddText(x, y, $"[{number}]", nameColor);
            AddText(x + $"[{number}]".Length + 1, y, itemName, isHovered ? Colors.Yellow : Colors.White);
            if (!string.IsNullOrEmpty(stats))
                AddText(x + 2, y + 1, $"    {stats}", statsColor);
        }

        /// <summary>
        /// Adds a character stat display to the canvas
        /// </summary>
        public void AddCharacterStat(int x, int y, string statName, int value, int maxValue, Color nameColor = default, Color valueColor = default)
        {
            if (nameColor == default) nameColor = Colors.White;
            if (valueColor == default) valueColor = Colors.Yellow;
            
            // Format stat name with colon (no space before colon)
            // Then add spacing after colon based on stat name
            // STR:   # (3 spaces), AGI:   # (3 spaces), TECH: # (1 space), INT:   # (3 spaces)
            string statNameWithColon = statName + ":";
            
            // Determine spacing based on stat name
            int spacesNeeded;
            if (statName == "TECH")
            {
                spacesNeeded = 1; // TECH: # (1 space)
            }
            else
            {
                spacesNeeded = 2; // STR:   #, AGI:   #, INT:   # (3 spaces)
            }
            string spaceAfterColon = new string(' ', spacesNeeded);
            
            // Only show maxValue if it's greater than 0
            string statText = maxValue > 0 ? $"{statNameWithColon}{spaceAfterColon}{value}/{maxValue}" : $"{statNameWithColon}{spaceAfterColon}{value}";
            AddText(x, y, statText, nameColor);
        }

        /// <summary>
        /// Adds a health bar to the canvas with damage delta animation support
        /// </summary>
        public void AddHealthBar(int x, int y, int width, int currentHealth, int maxHealth, Color healthColor = default, Color backgroundColor = default, string? entityId = null)
        {
            if (healthColor == default) healthColor = Colors.Red;
            if (backgroundColor == default) backgroundColor = Colors.DarkRed;
            
            double progress = (double)currentHealth / maxHealth;
            
            // Get previous health if entity ID is provided
            int? previousHealth = null;
            System.DateTime? damageDeltaStartTime = null;
            
            if (!string.IsNullOrEmpty(entityId))
            {
                // Get previous health BEFORE updating (so we can detect damage)
                int? healthBeforeUpdate = healthTracker.GetPreviousHealth(entityId);
                
                // Update tracker with current health (this will set damage delta start time if health decreased)
                healthTracker.UpdateHealth(entityId, currentHealth);
                
                // Get the health at damage time (preserved from when damage occurred)
                previousHealth = healthTracker.GetHealthAtDamageTime(entityId);
                
                // If no damage time health, use the previous health (for first render or when no damage)
                if (!previousHealth.HasValue)
                {
                    previousHealth = healthBeforeUpdate;
                }
                
                // Get the damage delta start time from tracker
                damageDeltaStartTime = healthTracker.GetDamageDeltaStartTime(entityId);
                
                // Only show delta if we have a valid damage start time and previous health is greater than current
                if (damageDeltaStartTime.HasValue && previousHealth.HasValue && previousHealth.Value <= currentHealth)
                {
                    // Health increased or stayed same, clear the damage delta
                    previousHealth = null;
                    damageDeltaStartTime = null;
                }
            }
            
            var progressBar = new CanvasProgressBar
            {
                X = x,
                Y = y,
                Width = width,
                Progress = progress,
                ForegroundColor = healthColor,
                BackgroundColor = backgroundColor,
                BorderColor = Colors.White,
                PreviousHealth = previousHealth,
                MaxHealth = maxHealth,
                DamageDeltaStartTime = damageDeltaStartTime
            };
            
            elementManager.AddProgressBar(progressBar);
        }
    }
}

