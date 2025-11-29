using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the in-game menu (after character creation)
    /// </summary>
    public class GameMenuRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public GameMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the in-game menu (after character creation)
        /// </summary>
        public int RenderGameMenu(int x, int y, int width, int height)
        {
            int currentLineCount = 0;
            
            // Welcome message
            int centerY = y + (height / 2) - 5;
            string welcomeText = "═══ WHAT WOULD YOU LIKE TO DO? ═══";
            int welcomeX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, welcomeText.Length);
            canvas.AddText(welcomeX, centerY, welcomeText, AsciiArtAssets.Colors.Gold);
            currentLineCount++;
            centerY += 3;
            
            // Game menu options
            int menuX = x + (width / 2) - 10;
            
            var option1 = new ClickableElement
            {
                X = menuX,
                Y = centerY,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = "[1] Go to Dungeon"
            };
            
            var option2 = new ClickableElement
            {
                X = menuX,
                Y = centerY + 1,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = "[2] Show Inventory"
            };
            
            var option3 = new ClickableElement
            {
                X = menuX,
                Y = centerY + 2,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = "[0] Save & Exit"
            };
            
            clickableElements.AddRange(new[] { option1, option2, option3 });
            
            canvas.AddMenuOption(menuX, centerY, 1, "Go to Dungeon", AsciiArtAssets.Colors.White, option1.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, centerY + 1, 2, "Show Inventory", AsciiArtAssets.Colors.White, option2.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, centerY + 2, 0, "Save & Exit", AsciiArtAssets.Colors.White, option3.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

