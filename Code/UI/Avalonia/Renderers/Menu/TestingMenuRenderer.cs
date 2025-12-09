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
        /// </summary>
        public int RenderTestingMenu(int x, int y, int width, int height)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Test description panel (top section)
            int panelHeight = MenuLayoutCalculator.CalculatePanelHeight(height - 4, 3, 1);
            int currentY = y;
            
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Blue);
            canvas.AddTitle(currentY + 1, "TEST DESCRIPTION", AsciiArtAssets.Colors.Blue);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            int textY = currentY + 3;
            canvas.AddText(x + 2, textY, "These tests verify all game systems:", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Character, Combat, Inventory, Dungeon", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Data Loading, UI, Save/Load, Actions", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Color System, Text System Accuracy", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Performance, Integration, Advanced Mechanics", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            // Test options panel (middle section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Green);
            canvas.AddTitle(currentY + 1, "TEST OPTIONS", AsciiArtAssets.Colors.Green);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            var testOptions = new[]
            {
                (1, "Run All Tests (Complete Suite)"),
                (2, "Character System Tests"),
                (3, "Combat System Tests (includes UI Fixes)"),
                (4, "Inventory & Dungeon Tests"),
                (5, "Data & UI System Tests"),
                (6, "Advanced & Integration Tests"),
                (7, "Generate 10 Random Items"),
                (0, "Back to Settings")
            };
            
            foreach (var (number, text) in testOptions)
            {
                string displayText = $"[{number}] {text}";
                
                // Create clickable element for this menu option
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
            
            // Instructions panel (bottom section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Yellow);
            canvas.AddTitle(currentY + 1, "INSTRUCTIONS", AsciiArtAssets.Colors.Yellow);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            canvas.AddText(x + 2, textY, "Select a test option to run. Results will be displayed", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "in the center panel after test completion.", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

