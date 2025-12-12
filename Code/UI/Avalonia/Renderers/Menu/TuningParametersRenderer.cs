using System.Collections.Generic;
using System.Linq;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the tuning parameters menu screen
    /// </summary>
    public class TuningParametersRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        private readonly VariableEditor variableEditor;
        
        public TuningParametersRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.variableEditor = new VariableEditor();
        }
        
        /// <summary>
        /// Renders the tuning parameters menu content
        /// Shows categories if no category selected, or variables if category selected
        /// </summary>
        public int RenderTuningParametersContent(int x, int y, int width, int height, string? selectedCategory = null, EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            if (string.IsNullOrEmpty(selectedCategory))
            {
                // Show category selection
                string title = "=== TUNING PARAMETERS ===";
                int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
                canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
                menuStartY += 3;
                
                canvas.AddText(menuStartX, menuStartY, "Select a category:", AsciiArtAssets.Colors.White);
                menuStartY += 2;
                
                // Display categories
                var categories = variableEditor.GetCategories();
                for (int i = 0; i < categories.Count; i++)
                {
                    string displayText = MenuOptionFormatter.Format(i + 1, categories[i]);
                    var option = new ClickableElement
                    {
                        X = menuStartX,
                        Y = menuStartY,
                        Width = displayText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = (i + 1).ToString(),
                        DisplayText = displayText
                    };
                    clickableElements.Add(option);
                    canvas.AddText(menuStartX, menuStartY, displayText, AsciiArtAssets.Colors.White);
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
            }
            else
            {
                if (isEditing && selectedVariable != null)
                {
                    // Show editing prompt
                    string title = $"=== EDIT {selectedVariable.Name.ToUpper()} ===";
                    int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
                    canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
                    menuStartY += 3;
                    
                    canvas.AddText(menuStartX, menuStartY, $"Current Value: {selectedVariable.GetValue()}", AsciiArtAssets.Colors.White);
                    menuStartY += 2;
                    
                    canvas.AddText(menuStartX, menuStartY, $"Description: {selectedVariable.Description}", AsciiArtAssets.Colors.Cyan);
                    menuStartY += 2;
                    
                    canvas.AddText(menuStartX, menuStartY, $"Type: {selectedVariable.GetValueType().Name}", AsciiArtAssets.Colors.Gray);
                    menuStartY += 2;
                    
                    canvas.AddText(menuStartX, menuStartY, "Enter new value:", AsciiArtAssets.Colors.Yellow);
                    menuStartY += 2;
                    
                    // Show current input buffer
                    string inputDisplay = string.IsNullOrEmpty(currentInput) ? "_" : currentInput + "_";
                    canvas.AddText(menuStartX, menuStartY, $"Input: {inputDisplay}", AsciiArtAssets.Colors.Green);
                    menuStartY += 2;
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        canvas.AddText(menuStartX, menuStartY, message, message.Contains("Error") || message.Contains("Invalid") ? AsciiArtAssets.Colors.Red : AsciiArtAssets.Colors.Yellow);
                        menuStartY += 2;
                    }
                    
                    canvas.AddText(menuStartX, menuStartY, "Press Enter to confirm, 'C' to cancel, Backspace to delete", AsciiArtAssets.Colors.Gray);
                }
                else
                {
                    // Show variables in selected category
                    string title = $"=== {selectedCategory.ToUpper()} PARAMETERS ===";
                    int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
                    canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
                    menuStartY += 3;
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        canvas.AddText(menuStartX, menuStartY, message, message.Contains("Error") ? AsciiArtAssets.Colors.Red : AsciiArtAssets.Colors.Green);
                        menuStartY += 2;
                    }
                    
                    var variables = variableEditor.GetVariablesByCategory(selectedCategory);
                    int displayCount = System.Math.Min(variables.Count, 20); // Show up to 20 variables
                    
                    for (int i = 0; i < displayCount; i++)
                    {
                        var variable = variables[i];
                        string displayText = MenuOptionFormatter.Format(i + 1, $"{variable.Name}: {variable.GetValue()}");
                        if (displayText.Length > width - 10)
                        {
                            displayText = displayText.Substring(0, width - 10) + "...";
                        }
                        
                        var option = new ClickableElement
                        {
                            X = menuStartX,
                            Y = menuStartY,
                            Width = displayText.Length,
                            Height = 1,
                            Type = ElementType.MenuOption,
                            Value = (i + 1).ToString(),
                            DisplayText = displayText
                        };
                        clickableElements.Add(option);
                        canvas.AddText(menuStartX, menuStartY, displayText, AsciiArtAssets.Colors.White);
                        menuStartY++;
                    }
                    
                    if (variables.Count > displayCount)
                    {
                        canvas.AddText(menuStartX, menuStartY, $"... and {variables.Count - displayCount} more variables", AsciiArtAssets.Colors.Gray);
                        menuStartY++;
                    }
                    
                    menuStartY += 2;
                    
                    // Instructions
                    canvas.AddText(menuStartX, menuStartY, "Select a number to edit, 'S' to save, '0' to go back", AsciiArtAssets.Colors.Cyan);
                    menuStartY += 2;
                    
                    // Back option
                    string backText = MenuOptionFormatter.Format(0, "Back to Categories");
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
                }
            }
            
            return currentLineCount;
        }
    }
}

