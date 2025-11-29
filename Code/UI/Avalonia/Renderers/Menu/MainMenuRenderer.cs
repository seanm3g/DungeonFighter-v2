using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the main menu screen
    /// </summary>
    public class MainMenuRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public MainMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the main menu content within the center panel (top-left justified)
        /// </summary>
        public int RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, string? characterName, int characterLevel)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Build menu options dynamically
            string loadGameText = hasSavedGame && characterName != null 
                ? $"Load Game - *{characterName} - lvl {characterLevel}*"
                : "Load Game";
            
            // Warm white to cold white gradient using ColorLayerSystem
            var menuConfig = new[]
            {
                (1, "New Game", ColorLayerSystem.GetWhite(WhiteTemperature.Warm)),
                (2, loadGameText, ColorLayerSystem.GetWhiteByDepth(2)),
                (3, "Settings", ColorLayerSystem.GetWhiteByDepth(3)),
                (0, "Quit", ColorLayerSystem.GetWhite(WhiteTemperature.Cool))
            };
            
            // Position menu at top-left of center panel
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Render menu options with colors
            for (int i = 0; i < menuConfig.Length; i++)
            {
                var (number, text, color) = menuConfig[i];
                string displayText = $"[{number}] {text}";
                
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY + i,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                
                canvas.AddMenuOption(menuStartX, menuStartY + i, number, text, color, option.IsHovered);
                currentLineCount++;
            }
            
            // Add instruction text at bottom of center panel
            int instructionY = y + height - 3;
            canvas.AddText(x + 2, instructionY, "Click on options or press number keys. Press H for help", AsciiArtAssets.Colors.Gray);
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders the main menu with saved game info if available (legacy method)
        /// </summary>
        public int RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            canvas.Clear();
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Title
            canvas.AddTitle(18, "MAIN MENU", AsciiArtAssets.Colors.White);
            currentLineCount = 20; // Title is at line 18 + 2 for spacing
            
            // Build menu options dynamically
            string loadGameText = hasSavedGame && characterName != null 
                ? $"Load Game - *{characterName} - lvl {characterLevel}*"
                : "Load Game";
            
            // Warm white to cold white gradient using ColorLayerSystem
            var menuConfig = new[]
            {
                (1, "New Game", ColorLayerSystem.GetWhite(WhiteTemperature.Warm)),
                (2, loadGameText, ColorLayerSystem.GetWhiteByDepth(2)),
                (3, "Settings", ColorLayerSystem.GetWhiteByDepth(3)),
                (0, "Quit", ColorLayerSystem.GetWhite(WhiteTemperature.Cool))
            };
            
            // Find the longest menu option for centering
            int maxOptionLength = 0;
            foreach (var (number, text, _) in menuConfig)
            {
                int length = $"[{number}] {text}".Length;
                if (length > maxOptionLength)
                    maxOptionLength = length;
            }
            
            // Center the menu options around screen center
            int menuStartX = MenuLayoutCalculator.SCREEN_CENTER - (maxOptionLength / 2);
            int menuStartY = 20;
            
            // Render menu options with colors
            for (int i = 0; i < menuConfig.Length; i++)
            {
                var (number, text, color) = menuConfig[i];
                string displayText = $"[{number}] {text}";
                
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY + i,
                    Width = maxOptionLength,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                
                // Render with gradient color (double render for bold effect)
                canvas.AddText(menuStartX, menuStartY + i, displayText, color);
                canvas.AddText(menuStartX, menuStartY + i, displayText, color);
                currentLineCount++;
            }
            
            // Instructions
            string instructions = "Click on options or press number keys. Press H for help";
            int instructionsX = MenuLayoutCalculator.CalculateCenteredTextX(0, MenuLayoutCalculator.SCREEN_WIDTH, instructions.Length);
            canvas.AddText(instructionsX, menuStartY + menuConfig.Length + 2, instructions, AsciiArtAssets.Colors.White);
            currentLineCount += 3; // +2 for spacing, +1 for instruction line
            
            canvas.Refresh();
            return currentLineCount;
        }
    }
}

