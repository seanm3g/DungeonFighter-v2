using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages layout calculations, positioning, and screen dimensions for the canvas UI
    /// </summary>
    public class CanvasLayoutManager
    {
        // Layout constants
        public const int LEFT_MARGIN = 2;
        public const int RIGHT_MARGIN = 2;
        public const int TOP_MARGIN = 2;
        public const int BOTTOM_MARGIN = 2;
        public const int CONTENT_WIDTH = 206; // 210 - 4 margins
        public const int CONTENT_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Screen dimensions and center point
        public const int SCREEN_WIDTH = 210;  // Total character width of the screen
        public const int SCREEN_HEIGHT = 52;  // Total character height of the screen (reduced from 60)
        public const int SCREEN_CENTER = SCREEN_WIDTH / 2;  // The center point for menus
        
        
        /// <summary>
        /// Calculates the center X position for text given its display length
        /// </summary>
        /// <param name="displayLength">The visible length of the text (excluding color markup)</param>
        /// <param name="screenCenter">The center point of the screen</param>
        /// <returns>X position to center the text</returns>
        public static int CalculateCenterX(int displayLength, int screenCenter)
        {
            return Math.Max(0, screenCenter - (displayLength / 2));
        }
        
        /// <summary>
        /// Calculates the center X position for text given its display length
        /// </summary>
        /// <param name="displayLength">The visible length of the text (excluding color markup)</param>
        /// <returns>X position to center the text</returns>
        public static int CalculateCenterX(int displayLength)
        {
            return CalculateCenterX(displayLength, SCREEN_CENTER);
        }
        
        /// <summary>
        /// Calculates the center Y position for content within a given height
        /// </summary>
        /// <param name="startY">Starting Y position</param>
        /// <param name="height">Available height</param>
        /// <param name="contentLines">Number of lines of content</param>
        /// <returns>Y position to center the content</returns>
        public static int CalculateCenterY(int startY, int height, int contentLines)
        {
            return startY + (height / 2) - (contentLines / 2);
        }
        
        /// <summary>
        /// Calculates the maximum width available for content within margins
        /// </summary>
        /// <param name="totalWidth">Total available width</param>
        /// <param name="leftMargin">Left margin</param>
        /// <param name="rightMargin">Right margin</param>
        /// <returns>Available content width</returns>
        public static int CalculateContentWidth(int totalWidth, int leftMargin = LEFT_MARGIN, int rightMargin = RIGHT_MARGIN)
        {
            return totalWidth - leftMargin - rightMargin;
        }
        
        /// <summary>
        /// Calculates the maximum height available for content within margins
        /// </summary>
        /// <param name="totalHeight">Total available height</param>
        /// <param name="topMargin">Top margin</param>
        /// <param name="bottomMargin">Bottom margin</param>
        /// <returns>Available content height</returns>
        public static int CalculateContentHeight(int totalHeight, int topMargin = TOP_MARGIN, int bottomMargin = BOTTOM_MARGIN)
        {
            return totalHeight - topMargin - bottomMargin;
        }
        
        /// <summary>
        /// Calculates the position for a menu option within a menu area
        /// </summary>
        /// <param name="menuStartX">Starting X position of the menu</param>
        /// <param name="menuStartY">Starting Y position of the menu</param>
        /// <param name="optionIndex">Index of the option (0-based)</param>
        /// <param name="maxOptionLength">Maximum length of any option for alignment</param>
        /// <returns>Tuple of (X, Y) position for the option</returns>
        public static (int x, int y) CalculateMenuOptionPosition(int menuStartX, int menuStartY, int optionIndex, int maxOptionLength)
        {
            int optionX = menuStartX + (maxOptionLength / 2) - (maxOptionLength / 2); // Center within menu
            int optionY = menuStartY + optionIndex;
            return (optionX, optionY);
        }
        
        /// <summary>
        /// Calculates the position for a button within a button area
        /// </summary>
        /// <param name="buttonAreaX">Starting X position of the button area</param>
        /// <param name="buttonAreaY">Starting Y position of the button area</param>
        /// <param name="buttonIndex">Index of the button (0-based)</param>
        /// <param name="buttonsPerRow">Number of buttons per row</param>
        /// <param name="buttonWidth">Width of each button</param>
        /// <param name="buttonSpacing">Spacing between buttons</param>
        /// <returns>Tuple of (X, Y) position for the button</returns>
        public static (int x, int y) CalculateButtonPosition(int buttonAreaX, int buttonAreaY, int buttonIndex, int buttonsPerRow, int buttonWidth, int buttonSpacing = 2)
        {
            int row = buttonIndex / buttonsPerRow;
            int col = buttonIndex % buttonsPerRow;
            
            int buttonX = buttonAreaX + (col * (buttonWidth + buttonSpacing));
            int buttonY = buttonAreaY + row;
            
            return (buttonX, buttonY);
        }
        
        /// <summary>
        /// Calculates the position for text within a content area with padding
        /// </summary>
        /// <param name="contentX">X position of the content area</param>
        /// <param name="contentY">Y position of the content area</param>
        /// <param name="contentWidth">Width of the content area</param>
        /// <param name="padding">Padding from the edges</param>
        /// <returns>Tuple of (X, Y, Width) for the text area</returns>
        public static (int x, int y, int width) CalculateTextArea(int contentX, int contentY, int contentWidth, int padding = 2)
        {
            return (contentX + padding, contentY + padding, contentWidth - (padding * 2));
        }
        
        /// <summary>
        /// Validates that a position is within screen bounds
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width of the element</param>
        /// <param name="height">Height of the element</param>
        /// <param name="screenWidth">Screen width</param>
        /// <param name="screenHeight">Screen height</param>
        /// <returns>True if the position is valid</returns>
        public static bool IsPositionValid(int x, int y, int width, int height, int screenWidth = SCREEN_WIDTH, int screenHeight = SCREEN_HEIGHT)
        {
            return x >= 0 && y >= 0 && 
                   x + width <= screenWidth && 
                   y + height <= screenHeight;
        }
        
        /// <summary>
        /// Clamps a position to ensure it stays within screen bounds
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width of the element</param>
        /// <param name="height">Height of the element</param>
        /// <param name="screenWidth">Screen width</param>
        /// <param name="screenHeight">Screen height</param>
        /// <returns>Tuple of (clampedX, clampedY)</returns>
        public static (int x, int y) ClampPosition(int x, int y, int width, int height, int screenWidth = SCREEN_WIDTH, int screenHeight = SCREEN_HEIGHT)
        {
            int clampedX = Math.Max(0, Math.Min(x, screenWidth - width));
            int clampedY = Math.Max(0, Math.Min(y, screenHeight - height));
            return (clampedX, clampedY);
        }
    }
}
