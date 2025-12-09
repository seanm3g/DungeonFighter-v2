using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

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
            ScrollDebugLogger.Log($"SettingsMenuRenderer: RenderSettingsContent called with x={x}, y={y}, width={width}, height={height}");
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            // Title
            string title = "=== SETTINGS ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            ScrollDebugLogger.Log($"SettingsMenuRenderer: Rendering title '{title}' at x={titleX}, y={menuStartY}");
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Menu options - only functional ones
            var menuOptions = new[]
            {
                (1, "Testing", AsciiArtAssets.Colors.White),
                (2, "Developer Menu", AsciiArtAssets.Colors.Yellow),
                (0, "Back to Main Menu", AsciiArtAssets.Colors.White)
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
                
                ScrollDebugLogger.Log($"SettingsMenuRenderer: Rendering option '{option.DisplayText}' at x={menuStartX}, y={menuStartY}");
                canvas.AddText(menuStartX, menuStartY, displayText, color);
                menuStartY++;
            }
            
            ScrollDebugLogger.Log($"SettingsMenuRenderer: Finished rendering {menuOptions.Length} options");
            
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

