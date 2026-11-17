using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages mouse interactions and clickable elements for the canvas UI
    /// </summary>
    public class CanvasInteractionManager : ICanvasInteractionManager
    {
        private readonly List<ClickableElement> clickableElements;
        private int hoverX = -1;
        private int hoverY = -1;
        
        public CanvasInteractionManager()
        {
            clickableElements = new List<ClickableElement>();
        }
        
        /// <summary>
        /// Gets all clickable elements
        /// </summary>
        public List<ClickableElement> ClickableElements => clickableElements;
        
        /// <summary>
        /// Gets the current hover position
        /// </summary>
        public (int x, int y) HoverPosition => (hoverX, hoverY);
        
        /// <summary>
        /// Clears all clickable elements
        /// </summary>
        public void ClearClickableElements()
        {
            clickableElements.Clear();
        }
        
        /// <summary>
        /// Adds a clickable element
        /// </summary>
        /// <param name="element">The clickable element to add</param>
        public void AddClickableElement(ClickableElement element)
        {
            clickableElements.Add(element);
        }

        /// <summary>
        /// Creates a clickable menu option
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width of the element</param>
        /// <param name="value">Value associated with the element</param>
        /// <param name="displayText">Text to display</param>
        /// <returns>The created clickable element</returns>
        public ClickableElement CreateMenuOption(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = value,
                DisplayText = displayText
            };
        }
        
        /// <summary>
        /// Creates a clickable button
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width of the element</param>
        /// <param name="value">Value associated with the element</param>
        /// <param name="displayText">Text to display</param>
        /// <returns>The created clickable element</returns>
        public ClickableElement CreateButton(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = 1,
                Type = ElementType.Button,
                Value = value,
                DisplayText = displayText
            };
        }

        /// <summary>
        /// Gets the element at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>The clickable element at the coordinates, or null if none found</returns>
        public ClickableElement? GetElementAt(int x, int y)
        {
            // Last registered wins so smaller/on-top regions (e.g. lphover:* stat rows) beat earlier broad targets.
            for (int i = clickableElements.Count - 1; i >= 0; i--)
            {
                var element = clickableElements[i];
                if (element.Contains(x, y))
                    return element;
            }
            return null;
        }
        
        /// <summary>
        /// Sets the hover position and updates hover states
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if any hover state changed</returns>
        public bool SetHoverPosition(int x, int y)
        {
            bool hoverChanged = false;
            
            // Update hover state for all elements
            foreach (var element in clickableElements)
            {
                bool wasHovered = element.IsHovered;
                element.IsHovered = element.Contains(x, y);
                
                if (wasHovered != element.IsHovered)
                {
                    hoverChanged = true;
                }
            }
            
            hoverX = x;
            hoverY = y;
            
            return hoverChanged;
        }
        
        /// <summary>
        /// Handles a click at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>The clicked element, or null if no element was clicked</returns>
        public ClickableElement? HandleClick(int x, int y)
        {
            var element = GetElementAt(x, y);
            return element;
        }

        /// <summary>
        /// Gets the count of clickable elements
        /// </summary>
        public int ElementCount => clickableElements.Count;
        
        /// <summary>
        /// Checks if there are any clickable elements
        /// </summary>
        public bool HasElements => clickableElements.Count > 0;

        /// <summary>
        /// Clears all hover states
        /// </summary>
        public void ClearHoverStates()
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = false;
            }
            hoverX = -1;
            hoverY = -1;
        }
    }
}
