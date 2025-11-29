using System.Collections.Generic;
using RPGGame.UI;
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
            canvas.AddText(x + 2, textY, "• Color System, Performance, Integration", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Combat UI Fixes and System Validation", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            // Test options panel (middle section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Green);
            canvas.AddTitle(currentY + 1, "TEST OPTIONS", AsciiArtAssets.Colors.Green);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            var testOptions = new[]
            {
                "[1] Run All Tests (Complete Suite)",
                "[2] Character System Tests",
                "[3] Combat System Tests",
                "[4] Inventory System Tests",
                "[5] Dungeon System Tests",
                "[6] Data System Tests",
                "[7] UI System Tests",
                "[8] Combat UI Fixes",
                "[9] Integration Tests",
                "[0] Back to Settings"
            };
            
            foreach (var option in testOptions)
            {
                canvas.AddText(x + 2, textY, option, AsciiArtAssets.Colors.White);
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

