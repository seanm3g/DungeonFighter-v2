using System.Collections.Generic;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the variable editor screen
    /// </summary>
    public class VariableEditorRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        private readonly VariableEditor variableEditor;
        
        public VariableEditorRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.variableEditor = new VariableEditor();
        }
        
        /// <summary>
        /// Renders the variable editor content
        /// </summary>
        public int RenderVariableEditorContent(int x, int y, int width, int height)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            // Title
            string title = "=== EDIT GAME VARIABLES ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Display variables
            var variables = variableEditor.GetVariables();
            int displayCount = System.Math.Min(variables.Count, 15); // Show first 15 variables
            
            for (int i = 0; i < displayCount; i++)
            {
                var variable = variables[i];
                string displayText = $"{variable.Name}: {variable.GetValue()}";
                if (displayText.Length > width - 10)
                {
                    displayText = displayText.Substring(0, width - 10) + "...";
                }
                canvas.AddText(menuStartX, menuStartY, displayText, AsciiArtAssets.Colors.White);
                menuStartY++;
            }
            
            if (variables.Count > displayCount)
            {
                canvas.AddText(menuStartX, menuStartY, $"... and {variables.Count - displayCount} more variables", AsciiArtAssets.Colors.Gray);
                menuStartY++;
            }
            
            menuStartY += 2;
            
            // Back option
            string backText = MenuOptionFormatter.Format(0, "Back to Developer Menu");
            var backOption = new ClickableElement
            {
                X = menuStartX,
                Y = menuStartY,
                Width = backText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = backText
            };
            clickableElements.Add(backOption);
            canvas.AddText(menuStartX, menuStartY, backText, AsciiArtAssets.Colors.White);
            return currentLineCount;
        }
    }
}

