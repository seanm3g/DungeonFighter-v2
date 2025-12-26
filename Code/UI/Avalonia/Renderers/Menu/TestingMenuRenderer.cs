using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the testing menu screen
    /// </summary>
    public class TestingMenuRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public TestingMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the testing menu screen using the 3-panel layout
        /// Supports main menu and sub-menus for categories
        /// </summary>
        public int RenderTestingMenu(int x, int y, int width, int height, string? subMenu = null)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Test description panel (top section)
            int panelHeight = MenuLayoutCalculator.CalculatePanelHeight(height - 4, 3, 1);
            int currentY = y;
            
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Blue);
            string descriptionTitle = subMenu == null ? "TEST DESCRIPTION" : $"TEST CATEGORY: {subMenu.ToUpper()}";
            canvas.AddTitle(currentY + 1, descriptionTitle, AsciiArtAssets.Colors.Blue);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            int textY = currentY + 3;
            if (subMenu == null)
            {
                canvas.AddText(x + 2, textY, "These tests verify all game systems:", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
                canvas.AddText(x + 2, textY, "• System Tests: Character, Combat, Inventory, Dungeon", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
                canvas.AddText(x + 2, textY, "• Item Tests: Generation, Distribution, Modifications", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
                canvas.AddText(x + 2, textY, "• Color System: Parsing, Configuration, Display", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
                canvas.AddText(x + 2, textY, "• Developer Tools: Action Editor, Configuration", AsciiArtAssets.Colors.White);
                currentLineCount++;
            }
            else
            {
                canvas.AddText(x + 2, textY, $"Select a test from the {subMenu} category.", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
            }
            
            // Test options panel (middle section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Green);
            string optionsTitle = subMenu == null ? "TEST OPTIONS" : $"{subMenu.ToUpper()} TESTS";
            canvas.AddTitle(currentY + 1, optionsTitle, AsciiArtAssets.Colors.Green);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            
            if (subMenu == null)
            {
                // Main menu
                var mainMenuOptions = new[]
                {
                    (1, "Run All Tests (Complete Suite)"),
                    (2, "System Tests"),
                    (3, "Item Tests"),
                    (4, "Color System Tests"),
                    (5, "Developer Tools Tests"),
                    (0, "Back to Settings")
                };
                
                foreach (var (number, text) in mainMenuOptions)
                {
                    string displayText = $"[{number}] {text}";
                    
                    var option = new ClickableElement
                    {
                        X = x + 2,
                        Y = textY,
                        Width = displayText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = number.ToString(),
                        DisplayText = displayText
                    };
                    clickableElements.Add(option);
                    
                    canvas.AddText(x + 2, textY, displayText, AsciiArtAssets.Colors.White);
                    textY++;
                    currentLineCount++;
                }
            }
            else if (subMenu == "System")
            {
                // System Tests sub-menu
                var systemTestOptions = new[]
                {
                    (1, "Character System Tests"),
                    (2, "Combat System Tests (includes UI Fixes)"),
                    (3, "Inventory & Dungeon Tests"),
                    (4, "Data & UI System Tests"),
                    (5, "Action System Tests"),
                    (6, "Advanced & Integration Tests"),
                    (7, "Combat Log Filtering Tests"),
                    (0, "Back to Test Menu")
                };
                
                foreach (var (number, text) in systemTestOptions)
                {
                    string displayText = $"[{number}] {text}";
                    
                    var option = new ClickableElement
                    {
                        X = x + 2,
                        Y = textY,
                        Width = displayText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = $"system_{number}",
                        DisplayText = displayText
                    };
                    clickableElements.Add(option);
                    
                    canvas.AddText(x + 2, textY, displayText, AsciiArtAssets.Colors.White);
                    textY++;
                    currentLineCount++;
                }
            }
            else if (subMenu == "Item")
            {
                // Item Tests sub-menu
                var itemTestOptions = new[]
                {
                    (1, "Generate 10 Random Items"),
                    (2, "Item Generation Analysis (100 items per level 1-20)"),
                    (3, "Tier Distribution Verification"),
                    (4, "Common Item Modification Chance (25% verification)"),
                    (0, "Back to Test Menu")
                };
                
                foreach (var (number, text) in itemTestOptions)
                {
                    string displayText = $"[{number}] {text}";
                    
                    var option = new ClickableElement
                    {
                        X = x + 2,
                        Y = textY,
                        Width = displayText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = $"item_{number}",
                        DisplayText = displayText
                    };
                    clickableElements.Add(option);
                    
                    canvas.AddText(x + 2, textY, displayText, AsciiArtAssets.Colors.White);
                    textY++;
                    currentLineCount++;
                }
            }
            else if (subMenu == "Developer")
            {
                // Developer Tools Tests sub-menu
                var developerTestOptions = new[]
                {
                    (1, "Action Editor Tests (Create, Update, Delete, Validation)"),
                    (0, "Back to Test Menu")
                };
                
                foreach (var (number, text) in developerTestOptions)
                {
                    string displayText = $"[{number}] {text}";
                    
                    var option = new ClickableElement
                    {
                        X = x + 2,
                        Y = textY,
                        Width = displayText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = $"developer_{number}",
                        DisplayText = displayText
                    };
                    clickableElements.Add(option);
                    
                    canvas.AddText(x + 2, textY, displayText, AsciiArtAssets.Colors.White);
                    textY++;
                    currentLineCount++;
                }
            }
            
            // Instructions panel (bottom section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Yellow);
            canvas.AddTitle(currentY + 1, "INSTRUCTIONS", AsciiArtAssets.Colors.Yellow);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            if (subMenu == null)
            {
                canvas.AddText(x + 2, textY, "Select a test category to view available tests.", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
                canvas.AddText(x + 2, textY, "Option 1 runs all tests at once.", AsciiArtAssets.Colors.White);
            }
            else
            {
                canvas.AddText(x + 2, textY, "Select a test option to run. Results will be displayed", AsciiArtAssets.Colors.White);
                textY++;
                currentLineCount++;
                canvas.AddText(x + 2, textY, "in the center panel after test completion.", AsciiArtAssets.Colors.White);
            }
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

