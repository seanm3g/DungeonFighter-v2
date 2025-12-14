namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.Items.Helpers;

    /// <summary>
    /// Renders combo management screens
    /// </summary>
    public class ComboManagementRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        
        public ComboManagementRenderer(
            GameCanvasControl canvas,
            List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }
        
        /// <summary>
        /// Renders the combo management menu
        /// </summary>
        public int RenderComboManagement(int x, int y, int width, int height, Character character)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("COMBO MANAGEMENT"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Combo info
            var comboActions = character.GetComboActions();
            canvas.AddText(x + 2, y, $"Current combo step: {character.ComboStep + 1}", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, $"Combo sequence length: {comboActions.Count}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Current combo sequence
            if (comboActions.Count > 0)
            {
                canvas.AddText(x + 2, y, "Combo Sequence:", AsciiArtAssets.Colors.Gold);
                y++;
                currentLineCount++;
                for (int i = 0; i < comboActions.Count; i++)
                {
                    var action = comboActions[i];
                    string currentStep = (character.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                    canvas.AddText(x + 4, y, $"{i + 1}. {action.Name}{currentStep}", AsciiArtAssets.Colors.White);
                    y++;
                    currentLineCount++;
                }
                y++;
                currentLineCount++;
            }
            else
            {
                canvas.AddText(x + 2, y, "(No combo actions set)", AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
            }
            
            // Action pool info
            var actionPool = character.GetActionPool();
            canvas.AddText(x + 2, y, $"Total actions in pool: {actionPool.Count}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            // Menu options
            y = startY + height - 8;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Actions), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            var addButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "1", MenuOptionFormatter.Format(1, "Add action to combo"));
            var removeButton = InventoryButtonFactory.CreateButton(x + 32, y, 28, "2", MenuOptionFormatter.Format(2, "Remove action from combo"));
            var reorderButton = InventoryButtonFactory.CreateButton(x + 2, y + 1, 28, "3", MenuOptionFormatter.Format(3, "Reorder combo actions"));
            var addAllButton = InventoryButtonFactory.CreateButton(x + 32, y + 1, 28, "4", MenuOptionFormatter.Format(4, "Add all available actions"));
            var backButton = InventoryButtonFactory.CreateButton(x + 2, y + 2, 28, "5", MenuOptionFormatter.Format(5, "Back to inventory"));
            
            clickableElements.AddRange(new[] { addButton, removeButton, reorderButton, addAllButton, backButton });
            
            canvas.AddMenuOption(x + 2, y, 1, "Add action to combo", AsciiArtAssets.Colors.White, addButton.IsHovered);
            canvas.AddMenuOption(x + 32, y, 2, "Remove action from combo", AsciiArtAssets.Colors.White, removeButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 1, 3, "Reorder combo actions", AsciiArtAssets.Colors.White, reorderButton.IsHovered);
            canvas.AddMenuOption(x + 32, y + 1, 4, "Add all available actions", AsciiArtAssets.Colors.White, addAllButton.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y + 2, 5, "Back to inventory", AsciiArtAssets.Colors.White, backButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders action selection prompt for adding/removing combo actions
        /// </summary>
        public int RenderComboActionSelection(int x, int y, int width, int height, Character character, string actionType)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            int startY = y;
            
            // Header
            string headerText = actionType == "add" ? "ADD ACTION TO COMBO" : "REMOVE ACTION FROM COMBO";
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(headerText), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            if (actionType == "add")
            {
                var actionPool = character.GetActionPool();
                var comboActions = character.GetComboActions();
                
                if (actionPool.Count == 0)
                {
                    canvas.AddText(x + 2, y, "No actions available to add to combo.", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                }
                else
                {
                    canvas.AddText(x + 2, y, "Available actions:", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                    
                    for (int i = 0; i < actionPool.Count; i++)
                    {
                        var action = actionPool[i];
                        int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                        int timesAvailable = actionPool.Count(ap => ap.Name == action.Name);
                        string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";
                        
                        var button = InventoryButtonFactory.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, action.Name + usageInfo));
                        clickableElements.Add(button);
                        
                        canvas.AddMenuOption(x + 2, y, i + 1, $"{action.Name}{usageInfo}", AsciiArtAssets.Colors.White, button.IsHovered);
                        y++;
                        currentLineCount++;
                        
                        // Show action description
                        canvas.AddText(x + 4, y, ActionDisplayFormatter.GetActionDescription(action), AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                        
                        // Show action stats (damage, speed, effects)
                        canvas.AddText(x + 4, y, ActionDisplayFormatter.GetActionStats(action), AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                    }
                }
            }
            else // remove
            {
                var comboActions = character.GetComboActions();
                
                if (comboActions.Count == 0)
                {
                    canvas.AddText(x + 2, y, "No actions in combo sequence to remove.", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                }
                else
                {
                    canvas.AddText(x + 2, y, "Current combo sequence:", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                    
                    for (int i = 0; i < comboActions.Count; i++)
                    {
                        var action = comboActions[i];
                        string currentStep = (character.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                        
                        var button = InventoryButtonFactory.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, action.Name + currentStep));
                        clickableElements.Add(button);
                        
                        canvas.AddMenuOption(x + 2, y, i + 1, $"{action.Name}{currentStep}", AsciiArtAssets.Colors.White, button.IsHovered);
                        y++;
                        currentLineCount++;
                        
                        canvas.AddText(x + 4, y, $"  {action.Description}", AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                    }
                }
            }
            
            y++;
            currentLineCount++;
            
            // Cancel button
            var cancelButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders combo reorder prompt
        /// </summary>
        public int RenderComboReorderPrompt(int x, int y, int width, int height, Character character, string currentSequence = "")
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);
            
            // Header
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("REORDER COMBO ACTIONS"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            var comboActions = character.GetComboActions();
            
            canvas.AddText(x + 2, y, "Current combo sequence:", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (character.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                canvas.AddText(x + 4, y, $"{i + 1}. {action.Name}{currentStep}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }
            
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, $"Enter the new order using numbers 1-{comboActions.Count} (e.g., 15324):", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, $"New order: {currentSequence}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, "Press 0 to confirm, or continue entering numbers.", AsciiArtAssets.Colors.Gray);
            currentLineCount++;
            
            return currentLineCount;
        }
    }
}

