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
            
            // Position menu at top-left of center panel
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
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
            RenderColoredMenuOption(menuStartX, currentY, 1, UIConstants.MenuOptions.NewGame, cyanColor, newGameOption.IsHovered);
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
                RenderColoredLoadGameOption(menuStartX, currentY, 2, characterName, characterLevel, goldColor, orangeColor, loadGameOption.IsHovered);
            }
            else
            {
                // Render plain load game option
                RenderColoredMenuOption(menuStartX, currentY, 2, UIConstants.MenuOptions.LoadGame, goldColor, loadGameOption.IsHovered);
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
            RenderColoredMenuOption(menuStartX, currentY, 3, UIConstants.MenuOptions.Settings, orangeColor, settingsOption.IsHovered);
            currentLineCount++;
            currentY++;
            
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
            RenderColoredMenuOption(menuStartX, currentY, 0, UIConstants.MenuOptions.Quit, coolWhiteColor, quitOption.IsHovered);
            currentLineCount++;
            
            // Add instruction text at bottom of center panel
            int instructionY = y + height - 3;
            canvas.AddText(x + 2, instructionY, UIConstants.Messages.ClickOrPressNumber, AsciiArtAssets.Colors.Gray);
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders a menu option with colored text
        /// </summary>
        private void RenderColoredMenuOption(int x, int y, int number, string text, Color color, bool isHovered)
        {
            Color numberColor = isHovered ? Colors.Yellow : color;
            Color textColor = isHovered ? Colors.Yellow : color;
            
            string numberText = $"[{number}]";
            canvas.AddText(x, y, numberText, numberColor);
            canvas.AddText(x + numberText.Length + 1, y, text, textColor);
        }
        
        /// <summary>
        /// Renders the Load Game option with special coloring for character name and level
        /// </summary>
        private void RenderColoredLoadGameOption(int x, int y, int number, string characterName, int characterLevel, Color nameColor, Color levelColor, bool isHovered)
        {
            Color baseColor = isHovered ? Colors.Yellow : Colors.White;
            Color charNameColor = isHovered ? Colors.Yellow : nameColor;
            Color charLevelColor = isHovered ? Colors.Yellow : levelColor;
            
            string numberText = $"[{number}]";
            string prefix = "Load Game - *";
            string namePart = characterName;
            string middlePart = " - lvl ";
            string levelPart = characterLevel.ToString();
            string suffix = "*";
            
            int currentX = x;
            
            // Render [2]
            canvas.AddText(currentX, y, numberText, isHovered ? Colors.Yellow : baseColor);
            currentX += numberText.Length + 1;
            
            // Render "Load Game - *"
            canvas.AddText(currentX, y, prefix, baseColor);
            currentX += prefix.Length;
            
            // Render character name in gold
            canvas.AddText(currentX, y, namePart, charNameColor);
            currentX += namePart.Length;
            
            // Render " - lvl "
            canvas.AddText(currentX, y, middlePart, baseColor);
            currentX += middlePart.Length;
            
            // Render level in orange/gold
            canvas.AddText(currentX, y, levelPart, charLevelColor);
            currentX += levelPart.Length;
            
            // Render closing "*"
            canvas.AddText(currentX, y, suffix, baseColor);
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
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
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
            RenderColoredMenuOption(menuStartX, currentY, 1, UIConstants.MenuOptions.NewGame, cyanColor, newGameOption.IsHovered);
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
                RenderColoredLoadGameOption(menuStartX, currentY, 2, characterName, characterLevel, goldColor, orangeColor, loadGameOption.IsHovered);
            }
            else
            {
                // Render plain load game option
                RenderColoredMenuOption(menuStartX, currentY, 2, UIConstants.MenuOptions.LoadGame, goldColor, loadGameOption.IsHovered);
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
            RenderColoredMenuOption(menuStartX, currentY, 3, UIConstants.MenuOptions.Settings, orangeColor, settingsOption.IsHovered);
            currentLineCount++;
            currentY++;
            
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
            RenderColoredMenuOption(menuStartX, currentY, 0, UIConstants.MenuOptions.Quit, coolWhiteColor, quitOption.IsHovered);
            currentLineCount++;
            
            // Instructions
            string instructions = UIConstants.Messages.ClickOrPressNumber;
            int instructionsX = MenuLayoutCalculator.CalculateCenteredTextX(0, MenuLayoutCalculator.SCREEN_WIDTH, instructions.Length);
            canvas.AddText(instructionsX, currentY + 2, instructions, AsciiArtAssets.Colors.White);
            currentLineCount += 3; // +2 for spacing, +1 for instruction line
            
            canvas.Refresh();
            return currentLineCount;
        }
    }
}

