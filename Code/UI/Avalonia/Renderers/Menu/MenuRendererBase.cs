using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Base class for menu renderers that extracts common menu rendering patterns
    /// Provides reusable methods for creating clickable elements, rendering options, and handling instructions
    /// </summary>
    public abstract class MenuRendererBase
    {
        protected readonly GameCanvasControl canvas;
        protected readonly List<ClickableElement> clickableElements;

        protected MenuRendererBase(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        /// <summary>
        /// Creates a clickable menu option element
        /// </summary>
        protected ClickableElement CreateMenuOption(int x, int y, int number, string text, int? width = null)
        {
            string displayText = MenuOptionFormatter.Format(number, text);
            return new ClickableElement
            {
                X = x,
                Y = y,
                Width = width ?? displayText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = number.ToString(),
                DisplayText = displayText
            };
        }

        /// <summary>
        /// Creates and adds a menu option to the clickable elements list
        /// </summary>
        protected ClickableElement AddMenuOption(int x, int y, int number, string text, int? width = null)
        {
            var option = CreateMenuOption(x, y, number, text, width);
            clickableElements.Add(option);
            return option;
        }

        /// <summary>
        /// Renders a menu option using canvas.AddMenuOption
        /// </summary>
        protected void RenderMenuOption(int x, int y, int number, string text, Color color, bool isHovered = false)
        {
            canvas.AddMenuOption(x, y, number, text, color, isHovered);
        }

        /// <summary>
        /// Creates, adds, and renders a menu option in one call
        /// </summary>
        protected ClickableElement CreateAndRenderMenuOption(int x, int y, int number, string text, Color color, int? width = null)
        {
            var option = AddMenuOption(x, y, number, text, width);
            RenderMenuOption(x, y, number, text, color, option.IsHovered);
            return option;
        }

        /// <summary>
        /// Renders instruction text at the specified position
        /// </summary>
        protected void RenderInstructions(int x, int y, string instructions, Color color = default)
        {
            if (color == default)
                color = AsciiArtAssets.Colors.White;
            canvas.AddText(x, y, instructions, color);
        }

        /// <summary>
        /// Renders centered instruction text
        /// </summary>
        protected void RenderCenteredInstructions(int x, int y, int width, string instructions, Color color = default)
        {
            if (color == default)
                color = AsciiArtAssets.Colors.White;
            int instructionX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, instructions.Length);
            RenderInstructions(instructionX, y, instructions, color);
        }

        /// <summary>
        /// Renders a header using the standard header format
        /// </summary>
        protected void RenderHeader(int x, int y, string headerText, Color color = default)
        {
            if (color == default)
                color = AsciiArtAssets.Colors.Gold;
            string header = AsciiArtAssets.UIText.CreateHeader(headerText);
            canvas.AddText(x, y, header, color);
        }

        /// <summary>
        /// Renders a centered header
        /// </summary>
        protected void RenderCenteredHeader(int x, int y, int width, string headerText, Color color = default)
        {
            if (color == default)
                color = AsciiArtAssets.Colors.Gold;
            string header = AsciiArtAssets.UIText.CreateHeader(headerText);
            int headerX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, header.Length);
            canvas.AddText(headerX, y, header, color);
        }

        /// <summary>
        /// Calculates the maximum length of formatted menu options
        /// </summary>
        protected int CalculateMaxOptionLength(IEnumerable<(int number, string text)> menuConfig)
        {
            int maxLength = 0;
            foreach (var (number, text) in menuConfig)
            {
                int length = MenuOptionFormatter.Format(number, text).Length;
                if (length > maxLength)
                    maxLength = length;
            }
            return maxLength;
        }
    }
}

