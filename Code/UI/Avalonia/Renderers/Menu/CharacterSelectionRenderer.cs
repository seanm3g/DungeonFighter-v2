using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the character selection menu content.
    /// Extracted from MainMenuRenderer to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CharacterSelectionRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly MenuOptionRenderer menuOptionRenderer;

        public CharacterSelectionRenderer(
            GameCanvasControl canvas,
            List<ClickableElement> clickableElements,
            MenuOptionRenderer menuOptionRenderer)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.menuOptionRenderer = menuOptionRenderer;
        }

        /// <summary>
        /// Renders the character selection menu content
        /// </summary>
        public int RenderCharacterSelectionContent(int x, int y, int width, int height, List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Position menu at top-left of center panel
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
            var whiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            
            int currentY = menuStartY;
            
            // Handle empty characters list
            if (characters == null || characters.Count == 0)
            {
                canvas.AddText(menuStartX, currentY, "No characters found.", whiteColor);
                currentY += 2;
                currentLineCount += 2;
                
                string createText = MenuOptionFormatter.Format(1, "Create New Character");
                var createOption = new ClickableElement
                {
                    X = menuStartX,
                    Y = currentY,
                    Width = createText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = "1",
                    DisplayText = createText
                };
                clickableElements.Add(createOption);
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 1, "Create New Character", cyanColor, createOption.IsHovered);
                currentY++;
                currentLineCount++;
                
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
                
                canvas.AddText(menuStartX, currentY, "Press 1 to create a new character, or 0 to return.", whiteColor);
                currentLineCount++;
            }
            else
            {
                // Render character list
                for (int i = 0; i < characters.Count; i++)
                {
                    var character = characters[i];
                    if (character == null) continue;
                    
                    // Check if this character is active (by name)
                    var isActive = character.Name == activeCharacterName;
                    
                    // Get status for this character (keyed by character name)
                    var status = characterStatuses.ContainsKey(character.Name) ? characterStatuses[character.Name] : "";
                    
                    try
                    {
                        var className = character.GetCurrentClass() ?? "Unknown";
                        var suffixText = $" - Level {character.Level} - {className}";
                        if (isActive)
                        {
                            suffixText += " [ACTIVE]";
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            suffixText += $" {status}";
                        }
                        
                        var optionText = $"{character.Name}{suffixText}";
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
                        
                        var color = isActive ? goldColor : cyanColor;
                        menuOptionRenderer.RenderColoredMenuOptionWithCharacter(menuStartX, currentY, i + 1, character, suffixText, color, option.IsHovered);
                        currentY++;
                        currentLineCount++;
                    }
                    catch (Exception)
                    {
                        // If GetCurrentClass fails, just show name and level
                        // status is already declared above, reuse it
                        var suffixText = $" - Level {character.Level}";
                        if (isActive)
                        {
                            suffixText += " [ACTIVE]";
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            suffixText += $" {status}";
                        }
                        
                        var optionText = $"{character.Name}{suffixText}";
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
                        
                        var color = isActive ? goldColor : cyanColor;
                        menuOptionRenderer.RenderColoredMenuOptionWithCharacter(menuStartX, currentY, i + 1, character, suffixText, color, option.IsHovered);
                        currentY++;
                        currentLineCount++;
                    }
                }
                
                currentY++;
                currentLineCount++;
                
                // Create new character option
                string createText = MenuOptionFormatter.Format(characters.Count + 1, "Create New Character");
                var createOption = new ClickableElement
                {
                    X = menuStartX,
                    Y = currentY,
                    Width = createText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = (characters.Count + 1).ToString(),
                    DisplayText = createText
                };
                clickableElements.Add(createOption);
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, characters.Count + 1, "Create New Character", cyanColor, createOption.IsHovered);
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
                
                canvas.AddText(menuStartX, currentY, "Select a character number, or press 0 to return.", whiteColor);
                currentLineCount++;
            }
            
            canvas.Refresh();
            return currentLineCount;
        }
    }
}

