using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the action editor screen
    /// </summary>
    public class ActionEditorRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        private readonly ActionEditor actionEditor;
        
        public ActionEditorRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.actionEditor = new ActionEditor();
        }
        
        /// <summary>
        /// Renders the action editor content
        /// </summary>
        public int RenderActionEditorContent(int x, int y, int width, int height)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            // Title
            string title = "=== EDIT ACTIONS ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Menu options
            var menuOptions = new[]
            {
                (1, "Create New Action", AsciiArtAssets.Colors.White),
                (2, "Edit Existing Action", AsciiArtAssets.Colors.White),
                (3, "Delete Action", AsciiArtAssets.Colors.White),
                (4, "List All Actions", AsciiArtAssets.Colors.White),
                (0, "Back to Developer Menu", AsciiArtAssets.Colors.White)
            };
            
            foreach (var (number, text, color) in menuOptions)
            {
                string displayText = MenuOptionFormatter.Format(number, text);
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                
                canvas.AddText(menuStartX, menuStartY, displayText, color);
                menuStartY++;
            }
            
            menuStartY += 2;
            
            // Show action count
            var actions = actionEditor.GetActions();
            string actionCount = $"Total Actions: {actions.Count}";
            int actionCountX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, actionCount.Length);
            canvas.AddText(actionCountX, menuStartY, actionCount, AsciiArtAssets.Colors.Cyan);
            menuStartY += 2;
            
            // Navigation hints
            canvas.AddText(menuStartX, menuStartY, "Navigation: 0=Back, 1-4=Select option", AsciiArtAssets.Colors.Gray);
            return currentLineCount;
        }

        /// <summary>
        /// Renders the action list content
        /// </summary>
        public int RenderActionListContent(int x, int y, int width, int height, List<ActionData> actions, int page)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Title
            string title = "=== ALL ACTIONS ===";
            canvas.AddText(menuStartX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 2;
            
            // Calculate pagination
            const int actionsPerPage = 20;
            int startIndex = page * actionsPerPage;
            int endIndex = Math.Min(startIndex + actionsPerPage, actions.Count);
            int totalPages = (int)Math.Ceiling(actions.Count / (double)actionsPerPage);
            
            // Show page info
            if (totalPages > 1)
            {
                string pageInfo = $"Page {page + 1} of {totalPages} (Actions {startIndex + 1}-{endIndex} of {actions.Count})";
                canvas.AddText(menuStartX, menuStartY, pageInfo, AsciiArtAssets.Colors.Cyan);
                menuStartY += 2;
            }
            
            // Display actions
            for (int i = startIndex; i < endIndex; i++)
            {
                var action = actions[i];
                string displayText = $"{i + 1}. {action.Name} ({action.Type})";
                if (displayText.Length > width - 10)
                {
                    displayText = displayText.Substring(0, width - 10) + "...";
                }
                canvas.AddText(menuStartX, menuStartY, displayText, AsciiArtAssets.Colors.White);
                menuStartY++;
            }
            
            menuStartY += 2;
            
            // Navigation instructions
            canvas.AddText(menuStartX, menuStartY, "Enter a number (1-20) to view action details", AsciiArtAssets.Colors.Yellow);
            menuStartY++;
            if (totalPages > 1)
            {
                canvas.AddText(menuStartX, menuStartY, "Use UP/DOWN to navigate pages", AsciiArtAssets.Colors.Gray);
                menuStartY++;
            }
            
            // Back option
            string backText = MenuOptionFormatter.Format(0, "Back to Action Editor");
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

