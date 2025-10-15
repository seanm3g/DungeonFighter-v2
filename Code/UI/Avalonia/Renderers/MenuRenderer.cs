using Avalonia.Media;
using RPGGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of all menu screens (main menu, settings, game menu)
    /// </summary>
    public class MenuRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private int currentLineCount;
        
        // Screen dimensions
        private const int SCREEN_WIDTH = 210;
        private const int SCREEN_CENTER = SCREEN_WIDTH / 2;
        private const int LEFT_MARGIN = 2;
        private const int CONTENT_WIDTH = 206;
        
        public MenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.currentLineCount = 0;
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            clickableElements.Clear();
            currentLineCount = 0;
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        // IInteractiveRenderer implementation
        public List<ClickableElement> GetClickableElements()
        {
            return clickableElements;
        }
        
        public void UpdateHoverState(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = element.Contains(x, y);
            }
        }
        
        public bool HandleClick(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                if (element.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Renders the main menu with saved game info if available
        /// </summary>
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            canvas.Clear();
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Title
            canvas.AddTitle(18, "MAIN MENU", AsciiArtAssets.Colors.White);
            currentLineCount = 20; // Title is at line 18 + 2 for spacing
            
            // Build menu options dynamically
            string loadGameText = hasSavedGame && characterName != null 
                ? $"Load Game - *{characterName} - lvl {characterLevel}*"
                : "Load Game";
            
            // Warm white to cold white gradient using ColorLayerSystem
            // This respects the WhiteTemperatureIntensity configuration parameter
            var menuConfig = new[]
            {
                (1, "New Game", ColorLayerSystem.GetWhite(WhiteTemperature.Warm, 1.0f).ToAvaloniaColor()),
                (2, loadGameText, ColorLayerSystem.GetWhiteByDepth(2, 4, 1.0f).ToAvaloniaColor()),
                (3, "Settings", ColorLayerSystem.GetWhiteByDepth(3, 4, 1.0f).ToAvaloniaColor()),
                (0, "Quit", ColorLayerSystem.GetWhite(WhiteTemperature.Cool, 0.92f).ToAvaloniaColor())
            };
            
            // Find the longest menu option for centering
            int maxOptionLength = menuConfig.Max(m => $"[{m.Item1}] {m.Item2}".Length);
            
            // Center the menu options around screen center
            int menuStartX = SCREEN_CENTER - (maxOptionLength / 2);
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
            int instructionsX = SCREEN_CENTER - (instructions.Length / 2);
            canvas.AddText(instructionsX, menuStartY + menuConfig.Length + 2, instructions, AsciiArtAssets.Colors.White);
            currentLineCount += 3; // +2 for spacing, +1 for instruction line
            
            canvas.Refresh();
        }
        
        /// <summary>
        /// Renders the settings screen
        /// </summary>
        public void RenderSettings()
        {
            canvas.Clear();
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Title
            canvas.AddTitle(2, "SETTINGS", AsciiArtAssets.Colors.White);
            currentLineCount = 4; // Title at line 2 + spacing
            
            // Settings sections
            canvas.AddBorder(LEFT_MARGIN, 4, CONTENT_WIDTH, 8, AsciiArtAssets.Colors.Blue);
            canvas.AddTitle(5, "GAME SETTINGS", AsciiArtAssets.Colors.Blue);
            currentLineCount = 7; // Border + title + spacing
            
            int y = 6;
            canvas.AddText(LEFT_MARGIN + 2, y, "UI Mode: ASCII Interface", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "Color Scheme: Terminal", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "Input: Mouse + Keyboard", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "Performance: Optimized", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            // Controls section
            y = 13;
            canvas.AddBorder(LEFT_MARGIN, y, CONTENT_WIDTH, 10, AsciiArtAssets.Colors.Green);
            canvas.AddTitle(y + 1, "CONTROLS", AsciiArtAssets.Colors.Green);
            currentLineCount = y + 3; // Border + title + spacing
            
            y += 3;
            canvas.AddText(LEFT_MARGIN + 2, y, "Mouse: Click to select options", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "Keyboard: 1-6 for menu options", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "H: Toggle help screen", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "ESC: Return to previous screen", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(LEFT_MARGIN + 2, y, "Hover: Highlight interactive elements", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            // Action buttons
            y = 24;
            int buttonCount = 1;
            
            // Check for saved character
            var (characterName, characterLevel) = CharacterSaveManager.GetSavedCharacterInfo();
            bool hasSavedCharacter = characterName != null;
            
            if (hasSavedCharacter)
            {
                buttonCount = 2;
            }
            
            int borderHeight = 4 + buttonCount * 2;
            canvas.AddBorder(LEFT_MARGIN, y, CONTENT_WIDTH, borderHeight, AsciiArtAssets.Colors.Yellow);
            canvas.AddTitle(y + 1, "ACTIONS", AsciiArtAssets.Colors.Yellow);
            currentLineCount = y + 3; // Up to the start of buttons
            
            int buttonY = y + 3;
            
            // Delete button if saved character exists
            if (hasSavedCharacter)
            {
                var deleteButton = new ClickableElement
                {
                    X = LEFT_MARGIN + 2,
                    Y = buttonY,
                    Width = 30,
                    Height = 1,
                    Type = ElementType.Button,
                    Value = "2",
                    DisplayText = "[2] Delete Saved Character"
                };
                clickableElements.Add(deleteButton);
                
                canvas.AddMenuOption(LEFT_MARGIN + 2, buttonY, 2, "Delete Saved Character", 
                    Color.FromRgb(255, 100, 100), deleteButton.IsHovered);
                currentLineCount++;
                canvas.AddText(LEFT_MARGIN + 4, buttonY + 1, 
                    $"Current Save: {characterName} (Level {characterLevel})", AsciiArtAssets.Colors.Gray);
                currentLineCount++;
                buttonY += 3;
            }
            
            // Back button
            var backButton = new ClickableElement
            {
                X = LEFT_MARGIN + 2,
                Y = buttonY,
                Width = 20,
                Height = 1,
                Type = ElementType.Button,
                Value = "1",
                DisplayText = "[1] Back to Main Menu"
            };
            clickableElements.Add(backButton);
            
            canvas.AddMenuOption(LEFT_MARGIN + 2, buttonY, 1, "Back to Main Menu", 
                AsciiArtAssets.Colors.White, backButton.IsHovered);
            currentLineCount++;
            
            canvas.Refresh();
        }
        
        /// <summary>
        /// Renders the in-game menu (after character creation)
        /// </summary>
        public void RenderGameMenu(int x, int y, int width, int height)
        {
            currentLineCount = 0;
            
            // Welcome message
            int centerY = y + (height / 2) - 5;
            canvas.AddText(x + (width / 2) - 20, centerY, "═══ WHAT WOULD YOU LIKE TO DO? ═══", AsciiArtAssets.Colors.Gold);
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
        }
    }
}

