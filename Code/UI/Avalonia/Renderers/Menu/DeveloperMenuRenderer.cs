using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the developer menu screen
    /// </summary>
    public class DeveloperMenuRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public DeveloperMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the developer menu content
        /// </summary>
        public int RenderDeveloperMenuContent(int x, int y, int width, int height)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            // Title
            string title = "=== DEVELOPER MENU ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Menu options
            var menuOptions = new[]
            {
                (1, "Edit Game Variables", AsciiArtAssets.Colors.White),
                (2, "Edit Actions", AsciiArtAssets.Colors.White),
                (3, "Battle Statistics", AsciiArtAssets.Colors.White),
                (0, "Back to Settings", AsciiArtAssets.Colors.White)
            };
            
            foreach (var (number, text, color) in menuOptions)
            {
                string displayText = MenuOptionFormatter.Format(number, text);
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                canvas.AddText(menuStartX, menuStartY, displayText, color);
                menuStartY++;
            }
            return currentLineCount;
        }
    }
}

