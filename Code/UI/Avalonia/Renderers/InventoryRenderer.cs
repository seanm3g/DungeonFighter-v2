using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.Avalonia.Renderers.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Items.Helpers;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of inventory-related screens.
    /// Facade coordinator that delegates to specialized renderers.
    /// </summary>
    public class InventoryRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private int currentLineCount;
        
        // Specialized renderers using composition pattern
        private readonly InventoryScreenRenderer screenRenderer;
        private readonly ItemSelectionRenderer selectionRenderer;
        private readonly ItemComparisonRenderer comparisonRenderer;
        private readonly ComboManagementRenderer comboRenderer;
        
        public InventoryRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            this.currentLineCount = 0;
            
            // Initialize specialized renderers
            this.screenRenderer = new InventoryScreenRenderer(canvas, textWriter, clickableElements);
            this.selectionRenderer = new ItemSelectionRenderer(canvas, textWriter, clickableElements);
            this.comparisonRenderer = new ItemComparisonRenderer(canvas, textWriter, clickableElements);
            this.comboRenderer = new ComboManagementRenderer(canvas, clickableElements);
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            clickableElements.Clear();
            currentLineCount = 0;
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        // IInteractiveRenderer implementation
        public List<ClickableElement> GetClickableElements()
        {
            return clickableElements;
        }
        
        public void UpdateHoverState(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = element.Contains(x, y);
            }
        }
        
        public bool HandleClick(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                if (element.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Renders the inventory screen with items and actions
        /// </summary>
        public void RenderInventory(int x, int y, int width, int height, Character character, List<Item> inventory)
        {
            currentLineCount = screenRenderer.RenderInventory(x, y, width, height, character, inventory);
        }
        
        /// <summary>
        /// Renders item selection prompt for equip/discard actions
        /// </summary>
        public void RenderItemSelectionPrompt(int x, int y, int width, int height, Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            currentLineCount = selectionRenderer.RenderItemSelectionPrompt(x, y, width, height, character, inventory, promptMessage, actionType);
        }
        
        /// <summary>
        /// Renders item comparison screen for equip decision
        /// </summary>
        public void RenderItemComparison(int x, int y, int width, int height, Character character, Item newItem, Item? currentItem, string slot)
        {
            currentLineCount = comparisonRenderer.RenderItemComparison(x, y, width, height, character, newItem, currentItem, slot);
        }
        
        /// <summary>
        /// Renders slot selection prompt for unequip action
        /// </summary>
        public void RenderSlotSelectionPrompt(int x, int y, int width, int height, Character character)
        {
            currentLineCount = selectionRenderer.RenderSlotSelectionPrompt(x, y, width, height, character);
        }

        /// <summary>
        /// Renders the combo management menu
        /// </summary>
        public void RenderComboManagement(int x, int y, int width, int height, Character character)
        {
            currentLineCount = comboRenderer.RenderComboManagement(x, y, width, height, character);
        }
        
        /// <summary>
        /// Renders action selection prompt for adding/removing combo actions
        /// </summary>
        public void RenderComboActionSelection(int x, int y, int width, int height, Character character, string actionType)
        {
            currentLineCount = comboRenderer.RenderComboActionSelection(x, y, width, height, character, actionType);
        }
        
        /// <summary>
        /// Renders combo reorder prompt
        /// </summary>
        public void RenderComboReorderPrompt(int x, int y, int width, int height, Character character, string currentSequence = "")
        {
            currentLineCount = comboRenderer.RenderComboReorderPrompt(x, y, width, height, character, currentSequence);
        }
    }
}



