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
        /// Adds multiple clickable elements
        /// </summary>
        /// <param name="elements">The clickable elements to add</param>
        public void AddClickableElements(IEnumerable<ClickableElement> elements)
        {
            clickableElements.AddRange(elements);
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
        /// Creates a clickable item
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width of the element</param>
        /// <param name="value">Value associated with the element</param>
        /// <param name="displayText">Text to display</param>
        /// <returns>The created clickable element</returns>
        public ClickableElement CreateItem(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = 1,
                Type = ElementType.Item,
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
            return clickableElements.FirstOrDefault(element => element.Contains(x, y));
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
        /// Gets all elements of a specific type
        /// </summary>
        /// <param name="type">The element type to filter by</param>
        /// <returns>List of elements of the specified type</returns>
        public List<ClickableElement> GetElementsByType(ElementType type)
        {
            return clickableElements.Where(e => e.Type == type).ToList();
        }
        
        /// <summary>
        /// Gets all elements with a specific value
        /// </summary>
        /// <param name="value">The value to filter by</param>
        /// <returns>List of elements with the specified value</returns>
        public List<ClickableElement> GetElementsByValue(string value)
        {
            return clickableElements.Where(e => e.Value == value).ToList();
        }
        
        /// <summary>
        /// Removes elements of a specific type
        /// </summary>
        /// <param name="type">The element type to remove</param>
        /// <returns>Number of elements removed</returns>
        public int RemoveElementsByType(ElementType type)
        {
            int removedCount = clickableElements.Count(e => e.Type == type);
            clickableElements.RemoveAll(e => e.Type == type);
            return removedCount;
        }
        
        /// <summary>
        /// Removes elements with a specific value
        /// </summary>
        /// <param name="value">The value to remove by</param>
        /// <returns>Number of elements removed</returns>
        public int RemoveElementsByValue(string value)
        {
            int removedCount = clickableElements.Count(e => e.Value == value);
            clickableElements.RemoveAll(e => e.Value == value);
            return removedCount;
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
        /// Gets all currently hovered elements
        /// </summary>
        /// <returns>List of currently hovered elements</returns>
        public List<ClickableElement> GetHoveredElements()
        {
            return clickableElements.Where(e => e.IsHovered).ToList();
        }
        
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
