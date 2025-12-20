using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the delete action confirmation screen
    /// </summary>
    public class DeleteActionConfirmationRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public DeleteActionConfirmationRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the delete action confirmation content
        /// </summary>
        public int RenderDeleteActionConfirmationContent(int x, int y, int width, int height, ActionData action, string? errorMessage = null)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 50);
            
            // Title
            string title = "=== DELETE ACTION CONFIRMATION ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Red);
            menuStartY += 3;
            
            // Warning message
            canvas.AddText(menuStartX, menuStartY, "WARNING: This action cannot be undone!", AsciiArtAssets.Colors.Red);
            menuStartY += 2;
            
            // Action details
            canvas.AddText(menuStartX, menuStartY, "Action to delete:", AsciiArtAssets.Colors.Yellow);
            menuStartY += 1;
            canvas.AddText(menuStartX, menuStartY, $"  Name: {action.Name}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Type: {action.Type}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Description: {action.Description}", AsciiArtAssets.Colors.White);
            menuStartY += 3;
            
            // Confirmation instructions
            canvas.AddText(menuStartX, menuStartY, "To confirm deletion, type:", AsciiArtAssets.Colors.Cyan);
            menuStartY += 1;
            canvas.AddText(menuStartX, menuStartY, $"  - 'DELETE' (all caps)", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  - Or the action name: '{action.Name}'", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            canvas.AddText(menuStartX, menuStartY, "To cancel, type 'cancel' or '0'", AsciiArtAssets.Colors.Gray);
            menuStartY += 2;
            
            // Error message if any
            if (!string.IsNullOrEmpty(errorMessage))
            {
                canvas.AddText(menuStartX, menuStartY, $"Error: {errorMessage}", AsciiArtAssets.Colors.Red);
                menuStartY += 2;
            }
            
            return currentLineCount;
        }
    }
}

