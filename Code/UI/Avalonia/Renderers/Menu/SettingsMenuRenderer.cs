using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the settings menu screen
    /// </summary>
    public class SettingsMenuRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public SettingsMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the simplified settings content with only functional options
        /// </summary>
        public int RenderSettingsContent(int x, int y, int width, int height, bool hasSavedCharacter, string? characterName, int characterLevel)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            // Title
            string title = "=== SETTINGS ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Menu options - only functional ones
            var menuOptions = new[]
            {
                (1, "Testing", AsciiArtAssets.Colors.White),
                (0, "Back to Main Menu", AsciiArtAssets.Colors.White)
            };
            
            foreach (var (number, text, color) in menuOptions)
            {
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY,
                    Width = text.Length + 4,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = $"[{number}] {text}"
                };
                clickableElements.Add(option);
                
                canvas.AddText(menuStartX, menuStartY, $"[{number}] {text}", color);
                menuStartY++;
            }
            
            // Show saved character info if available
            if (hasSavedCharacter && characterName != null)
            {
                menuStartY += 2;
                string savedInfo = $"Saved Character: {characterName} (Level {characterLevel})";
                int savedInfoX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, savedInfo.Length);
                canvas.AddText(savedInfoX, menuStartY, savedInfo, AsciiArtAssets.Colors.Cyan);
                menuStartY++;
                string deleteInfo = "Use 'Delete Saved Character' in game menu to remove";
                int deleteInfoX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, deleteInfo.Length);
                canvas.AddText(deleteInfoX, menuStartY, deleteInfo, AsciiArtAssets.Colors.Gray);
            }
            
            return currentLineCount;
        }
    }
}

