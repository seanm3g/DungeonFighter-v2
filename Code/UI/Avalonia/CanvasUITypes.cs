using System;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Types of clickable elements in the canvas UI
    /// </summary>
    public enum ElementType
    {
        MenuOption,
        Item,
        Button,
        Text
    }

    /// <summary>
    /// Represents a clickable element in the canvas UI
    /// </summary>
    public class ClickableElement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ElementType Type { get; set; }
        public string Value { get; set; } = "";
        public string DisplayText { get; set; } = "";
        public bool IsHovered { get; set; } = false;
        
        /// <summary>
        /// Checks if the given coordinates are within this element's bounds
        /// </summary>
        public bool Contains(int x, int y)
        {
            return x >= X && x < X + Width && y >= Y && y < Y + Height;
        }
    }
}
