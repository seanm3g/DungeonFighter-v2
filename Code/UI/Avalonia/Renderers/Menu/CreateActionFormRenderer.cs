using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the create action form screen
    /// </summary>
    public class CreateActionFormRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public CreateActionFormRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the create action form content
        /// </summary>
        public int RenderCreateActionFormContent(int x, int y, int width, int height, ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Title
            string title = isEditMode ? "=== EDIT ACTION ===" : "=== CREATE NEW ACTION ===";
            canvas.AddText(menuStartX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 2;
            
            // Progress indicator
            string progress = $"Step {currentStep + 1} of {formSteps.Length}";
            canvas.AddText(menuStartX, menuStartY, progress, AsciiArtAssets.Colors.Cyan);
            menuStartY += 2;
            
            // Current field
            string currentField = formSteps[currentStep];
            canvas.AddText(menuStartX, menuStartY, $"Enter {currentField}:", AsciiArtAssets.Colors.White);
            menuStartY += 1;
            
            // Show current input or value
            if (!string.IsNullOrEmpty(currentInput))
            {
                canvas.AddText(menuStartX, menuStartY, $"> {currentInput}_", AsciiArtAssets.Colors.Yellow);
                menuStartY += 2;
            }
            else
            {
                string currentValue = GetCurrentValue(actionData, currentStep);
                if (!string.IsNullOrEmpty(currentValue))
                {
                    canvas.AddText(menuStartX, menuStartY, $"Current: {currentValue}", AsciiArtAssets.Colors.Gray);
                    menuStartY += 1;
                }
                canvas.AddText(menuStartX, menuStartY, "> _", AsciiArtAssets.Colors.Yellow);
                menuStartY += 2;
            }
            
            // Show preview of action so far
            menuStartY += 1;
            canvas.AddText(menuStartX, menuStartY, "Action Preview:", AsciiArtAssets.Colors.Yellow);
            menuStartY += 1;
            
            canvas.AddText(menuStartX, menuStartY, $"  Name: {actionData.Name}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Type: {actionData.Type}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Target: {actionData.TargetType}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Base Value: {actionData.BaseValue}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Description: {(string.IsNullOrEmpty(actionData.Description) ? "(not set)" : actionData.Description)}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Damage Multiplier: {actionData.DamageMultiplier}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Length: {actionData.Length}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Cooldown: {actionData.Cooldown}", AsciiArtAssets.Colors.White);
            
            menuStartY += 3;
            
            // Instructions
            canvas.AddText(menuStartX, menuStartY, "Type your input and press Enter", AsciiArtAssets.Colors.Gray);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, "Type 'back' to go to previous step", AsciiArtAssets.Colors.Gray);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, "Type 'cancel' or '0' to cancel", AsciiArtAssets.Colors.Gray);
            return currentLineCount;
        }

        private string GetCurrentValue(ActionData actionData, int step)
        {
            return step switch
            {
                0 => actionData.Name,
                1 => actionData.Type,
                2 => actionData.TargetType,
                3 => actionData.BaseValue.ToString(),
                4 => actionData.Description,
                5 => actionData.DamageMultiplier.ToString(),
                6 => actionData.Length.ToString(),
                7 => actionData.Cooldown.ToString(),
                _ => ""
            };
        }
    }
}

