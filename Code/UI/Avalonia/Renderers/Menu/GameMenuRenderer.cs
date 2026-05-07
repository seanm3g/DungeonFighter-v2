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
            
            // Game menu options
            int centerY = y + (height / 2) - 5;
            int menuX = x + (width / 2) - 10;
            const int menuWidth = 20;

            // Prompt (plain white, centered above options)
            const string promptText = "What would you like to do?";
            int promptX = menuX + System.Math.Max(0, (menuWidth - promptText.Length) / 2);
            canvas.AddText(promptX, centerY, promptText, AsciiArtAssets.Colors.White);
            currentLineCount++;
            centerY += 2;
            
            var option1 = new ClickableElement
            {
                X = menuX,
                Y = centerY,
                Width = menuWidth,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.GoToDungeon)
            };
            
            var option2 = new ClickableElement
            {
                X = menuX,
                Y = centerY + 1,
                Width = menuWidth,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = MenuOptionFormatter.Format(2, UIConstants.MenuOptions.ShowInventory)
            };
            
            var option3 = new ClickableElement
            {
                X = menuX,
                Y = centerY + 2,
                Width = menuWidth,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.BackToMainMenu)
            };
            
            clickableElements.AddRange(new[] { option1, option2, option3 });
            
            canvas.AddMenuOption(menuX, centerY, 1, UIConstants.MenuOptions.GoToDungeon, AsciiArtAssets.Colors.White, option1.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, centerY + 1, 2, UIConstants.MenuOptions.ShowInventory, AsciiArtAssets.Colors.White, option2.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, centerY + 2, 0, UIConstants.MenuOptions.BackToMainMenu, AsciiArtAssets.Colors.White, option3.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

