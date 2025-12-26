using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Handlers.Inventory
{
    /// <summary>
    /// Handles combo sequence reordering logic
    /// Extracted from InventoryComboManager.cs ApplyReorder() method
    /// </summary>
    public static class ComboReorderer
    {
        /// <summary>
        /// Applies the reorder to the combo sequence
        /// </summary>
        public static bool ApplyReorder(Character character, string input, List<RPGGame.Action> currentComboActions)
        {
            if (character == null) return false;
            
            try
            {
                var newOrder = input.Select(c => int.Parse(c.ToString())).ToList();
                
                // Create a new list with actions in the specified order
                var reorderedActions = new List<RPGGame.Action>();
                for (int i = 0; i < newOrder.Count; i++)
                {
                    int actionIndex = newOrder[i] - 1; // Convert to 0-based index
                    if (actionIndex >= 0 && actionIndex < currentComboActions.Count)
                    {
                        reorderedActions.Add(currentComboActions[actionIndex]);
                    }
                }
                
                if (reorderedActions.Count != currentComboActions.Count)
                {
                    return false;
                }
                
                // Clear current combo (this sets ComboOrder to 0 on removed actions)
                foreach (var action in currentComboActions)
                {
                    character.RemoveFromCombo(action);
                }
                
                // Set ComboOrder values AFTER removing but BEFORE adding
                // This ensures that when AddToCombo calls ReorderComboSequence, it sorts correctly
                for (int i = 0; i < reorderedActions.Count; i++)
                {
                    reorderedActions[i].ComboOrder = i + 1;
                }
                
                // Add actions back in the desired order
                // Since we've set ComboOrder values, ReorderComboSequence will maintain the order
                foreach (var action in reorderedActions)
                {
                    character.AddToCombo(action);
                }
                
                // Reset combo step to first action when actions are reordered
                character.ComboStep = 0;
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

