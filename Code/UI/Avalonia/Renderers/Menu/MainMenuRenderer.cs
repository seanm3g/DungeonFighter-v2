using System;
using System.Collections.Generic;
using Avalonia.Media;
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
        private readonly MenuOptionRenderer menuOptionRenderer;
        private readonly CharacterSelectionRenderer characterSelectionRenderer;
        
        public MainMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.menuOptionRenderer = new MenuOptionRenderer(canvas);
            this.characterSelectionRenderer = new CharacterSelectionRenderer(canvas, clickableElements, menuOptionRenderer);
        }
        
        /// <summary>
        /// Renders the main menu content within the center panel (top-left justified)
        /// </summary>
        public int RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, string? characterName, int characterLevel)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Position menu at top-left of center panel
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
            var playerColor = ColorPalette.Player.GetColor();
            var coolWhiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            
            // Render [1] New Game - Cyan
            int currentY = menuStartY;
            string newGameText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame);
            var newGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = newGameText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = newGameText
            };
            clickableElements.Add(newGameOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 1, UIConstants.MenuOptions.NewGame, cyanColor, newGameOption.IsHovered);
            currentLineCount++;
            currentY++;
            
            // Render [2] Load Game - with special coloring for character name
            string loadGameText;
            if (hasSavedGame && characterName != null)
            {
                loadGameText = string.Format(UIConstants.Formats.LoadGameWithCharacter, characterName, characterLevel);
            }
            else
            {
                loadGameText = UIConstants.MenuOptions.LoadGame;
            }
            
            string loadGameDisplayText = MenuOptionFormatter.Format(2, loadGameText);
            var loadGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = loadGameDisplayText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = loadGameDisplayText
            };
            clickableElements.Add(loadGameOption);
            
            if (hasSavedGame && characterName != null)
            {
                // Render with colored character name and level
                menuOptionRenderer.RenderColoredLoadGameOption(menuStartX, currentY, 2, characterName, characterLevel, playerColor, orangeColor, loadGameOption.IsHovered);
            }
            else
            {
                // Render plain load game option
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 2, UIConstants.MenuOptions.LoadGame, goldColor, loadGameOption.IsHovered);
            }
            currentLineCount++;
            currentY++;
            
            // Render [3] Settings - Orange
            string settingsText = MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings);
            var settingsOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = settingsText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "3",
                DisplayText = settingsText
            };
            clickableElements.Add(settingsOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 3, UIConstants.MenuOptions.Settings, orangeColor, settingsOption.IsHovered);
            currentLineCount++;
            currentY++;
            
            // Note: Characters option (4 or C) is hidden from UI but still accessible via keyboard
            
            // Render [0] Quit - Cool white
            string quitText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit);
            var quitOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = quitText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = quitText
            };
            clickableElements.Add(quitOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 0, UIConstants.MenuOptions.Quit, coolWhiteColor, quitOption.IsHovered);
            currentLineCount++;
            
            // Add instruction text at bottom of center panel
            int instructionY = y + height - 3;
            canvas.AddText(x + 2, instructionY, UIConstants.Messages.ClickOrPressNumber, AsciiArtAssets.Colors.Gray);
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders the main menu with saved game info if available (legacy method)
        /// NOTE: This method is legacy and may not be used. Canvas clearing should be handled by the caller.
        /// </summary>
        public int RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            // Note: Canvas clearing removed - should be handled by caller or LayoutCoordinator
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Title
            canvas.AddTitle(18, "MAIN MENU", AsciiArtAssets.Colors.White);
            currentLineCount = 20; // Title is at line 18 + 2 for spacing
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
            var playerColor = ColorPalette.Player.GetColor();
            var coolWhiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            
            // Find the longest menu option for centering
            string loadGameText = hasSavedGame && characterName != null 
                ? string.Format(UIConstants.Formats.LoadGameWithCharacter, characterName, characterLevel)
                : UIConstants.MenuOptions.LoadGame;
            
            int maxOptionLength = Math.Max(
                MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame).Length,
                Math.Max(
                    MenuOptionFormatter.Format(2, loadGameText).Length,
                    Math.Max(
                        MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings).Length,
                        MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit).Length
                    )
                )
            );
            
            // Center the menu options around screen center
            int menuStartX = MenuLayoutCalculator.SCREEN_CENTER - (maxOptionLength / 2);
            int menuStartY = 20;
            int currentY = menuStartY;
            
            // Render [1] New Game - Cyan
            string newGameText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame);
            var newGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = newGameText
            };
            clickableElements.Add(newGameOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 1, UIConstants.MenuOptions.NewGame, cyanColor, newGameOption.IsHovered);
            currentLineCount++;
            currentY++;
            
            // Render [2] Load Game - with special coloring for character name
            string loadGameDisplayText = MenuOptionFormatter.Format(2, loadGameText);
            var loadGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = loadGameDisplayText
            };
            clickableElements.Add(loadGameOption);
            
            if (hasSavedGame && characterName != null)
            {
                // Render with colored character name and level
                menuOptionRenderer.RenderColoredLoadGameOption(menuStartX, currentY, 2, characterName, characterLevel, playerColor, orangeColor, loadGameOption.IsHovered);
            }
            else
            {
                // Render plain load game option
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 2, UIConstants.MenuOptions.LoadGame, goldColor, loadGameOption.IsHovered);
            }
            currentLineCount++;
            currentY++;
            
            // Render [3] Settings - Orange
            string settingsText = MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings);
            var settingsOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "3",
                DisplayText = settingsText
            };
            clickableElements.Add(settingsOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 3, UIConstants.MenuOptions.Settings, orangeColor, settingsOption.IsHovered);
            currentLineCount++;
            currentY++;
            
            // Note: Characters option (4 or C) is hidden from UI but still accessible via keyboard
            
            // Render [0] Quit - Cool white
            string quitText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit);
            var quitOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = quitText
            };
            clickableElements.Add(quitOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 0, UIConstants.MenuOptions.Quit, coolWhiteColor, quitOption.IsHovered);
            currentLineCount++;
            
            // Instructions
            string instructions = UIConstants.Messages.ClickOrPressNumber;
            int instructionsX = MenuLayoutCalculator.CalculateCenteredTextX(0, MenuLayoutCalculator.SCREEN_WIDTH, instructions.Length);
            canvas.AddText(instructionsX, currentY + 2, instructions, AsciiArtAssets.Colors.White);
            currentLineCount += 3; // +2 for spacing, +1 for instruction line
            
            canvas.Refresh();
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders the character selection menu content
        /// </summary>
        public int RenderCharacterSelectionContent(int x, int y, int width, int height, List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
        {
            return characterSelectionRenderer.RenderCharacterSelectionContent(x, y, width, height, characters, activeCharacterName, characterStatuses);
        }

        /// <summary>
        /// Renders the load character selection menu content (shows saved characters from disk)
        /// </summary>
        public int RenderLoadCharacterSelectionContent(int x, int y, int width, int height, List<(string characterId, string characterName, int level)> savedCharacters)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Position menu at top-left of center panel
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var whiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            
            int currentY = menuStartY;
            
            // Handle empty characters list
            if (savedCharacters == null || savedCharacters.Count == 0)
            {
                canvas.AddText(menuStartX, currentY, "No saved characters found.", whiteColor);
                currentY += 2;
                currentLineCount += 2;
                
                string backText = MenuOptionFormatter.Format(0, "Back to Main Menu");
                var backOption = new ClickableElement
                {
                    X = menuStartX,
                    Y = currentY,
                    Width = backText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = "0",
                    DisplayText = backText
                };
                clickableElements.Add(backOption);
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 0, "Back to Main Menu", goldColor, backOption.IsHovered);
                currentY += 2;
                currentLineCount += 2;
                
                canvas.AddText(menuStartX, currentY, "Press 0 to return to main menu.", whiteColor);
                currentLineCount++;
            }
            else
            {
                // Render saved character list
                for (int i = 0; i < savedCharacters.Count; i++)
                {
                    var (characterId, characterName, level) = savedCharacters[i];
                    
                    var optionText = $"{characterName} - Level {level}";
                    string formattedText = MenuOptionFormatter.Format(i + 1, optionText);
                    var option = new ClickableElement
                    {
                        X = menuStartX,
                        Y = currentY,
                        Width = formattedText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = (i + 1).ToString(),
                        DisplayText = formattedText
                    };
                    clickableElements.Add(option);
                    
                    menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, i + 1, optionText, cyanColor, option.IsHovered);
                    currentY++;
                    currentLineCount++;
                }
                
                currentY++;
                currentLineCount++;
                
                // Back to main menu option
                string backText = MenuOptionFormatter.Format(0, "Back to Main Menu");
                var backOption = new ClickableElement
                {
                    X = menuStartX,
                    Y = currentY,
                    Width = backText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = "0",
                    DisplayText = backText
                };
                clickableElements.Add(backOption);
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 0, "Back to Main Menu", goldColor, backOption.IsHovered);
                currentY += 2;
                currentLineCount += 2;
                
                canvas.AddText(menuStartX, currentY, "Select a character number to load, or press 0 to return.", whiteColor);
                currentLineCount++;
            }
            
            canvas.Refresh();
            return currentLineCount;
        }
    }
}

