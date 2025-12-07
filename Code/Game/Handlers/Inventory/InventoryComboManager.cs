namespace RPGGame.Handlers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Manages combo sequence operations: add, remove, and reorder actions.
    /// </summary>
    public class InventoryComboManager
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly InventoryStateManager stateTracker;
        
        // Event delegates
        public delegate void OnShowMessage(string message);
        public delegate void OnShowInventory();
        
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowInventory? ShowInventoryEvent;
        
        public InventoryComboManager(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            InventoryStateManager stateTracker)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        }
        
        /// <summary>
        /// Show combo management menu
        /// </summary>
        public void ShowComboManagement()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            stateTracker.InComboManagement = true;
            RenderComboManagementScreen();
        }
        
        /// <summary>
        /// Helper method to render combo management screen
        /// </summary>
        private void RenderComboManagementScreen()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboManagement(stateManager.CurrentPlayer);
            }
        }
        
        /// <summary>
        /// Handle input for combo management menu
        /// </summary>
        public void HandleComboManagementInput(string input)
        {
            if (stateManager.CurrentPlayer == null) return;
            
            // Handle action selection for adding to combo
            if (stateTracker.WaitingForComboActionSelection && int.TryParse(input, out int actionIndex))
            {
                stateTracker.WaitingForComboActionSelection = false;
                
                if (actionIndex == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    stateTracker.InComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                var actionPool = stateManager.CurrentPlayer.GetActionPool();
                if (actionIndex >= 1 && actionIndex <= actionPool.Count)
                {
                    var action = actionPool[actionIndex - 1];
                    stateManager.CurrentPlayer.AddToCombo(action);
                    ShowMessageEvent?.Invoke($"Added {action.Name} to combo sequence.");
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid action selection.");
                }
                
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            // Handle action removal
            if (stateTracker.WaitingForComboRemoveSelection && int.TryParse(input, out int removeIndex))
            {
                stateTracker.WaitingForComboRemoveSelection = false;
                
                if (removeIndex == 0)
                {
                    ShowMessageEvent?.Invoke("Cancelled.");
                    stateTracker.InComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                var comboActions = stateManager.CurrentPlayer.GetComboActions();
                if (removeIndex >= 1 && removeIndex <= comboActions.Count)
                {
                    var action = comboActions[removeIndex - 1];
                    stateManager.CurrentPlayer.RemoveFromCombo(action);
                    ShowMessageEvent?.Invoke($"Removed {action.Name} from combo sequence.");
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid action selection.");
                }
                
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            // Handle reorder input
            if (stateTracker.WaitingForComboReorderInput)
            {
                var comboActions = stateManager.CurrentPlayer.GetComboActions();
                
                // Check if user wants to confirm with 0
                if (input == "0")
                {
                    stateTracker.WaitingForComboReorderInput = false;
                    
                    if (string.IsNullOrEmpty(stateTracker.ReorderInputSequence))
                    {
                        ShowMessageEvent?.Invoke("Cancelled.");
                        stateTracker.ReorderInputSequence = "";
                        stateTracker.InComboManagement = true;
                        RenderComboManagementScreen();
                        return;
                    }
                    
                    // Validate and apply the accumulated sequence
                    if (ValidateReorderInput(stateTracker.ReorderInputSequence, comboActions.Count))
                    {
                        if (ApplyReorder(stateTracker.ReorderInputSequence, comboActions))
                        {
                            ShowMessageEvent?.Invoke("Combo sequence reordered successfully!");
                        }
                        else
                        {
                            ShowMessageEvent?.Invoke("Error: Failed to reorder combo sequence.");
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke($"Invalid input. Please enter numbers 1-{comboActions.Count} in any order (e.g., 15324).");
                    }
                    
                    stateTracker.ReorderInputSequence = "";
                    stateTracker.InComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                // Handle cancel
                if (input.ToLower() == "cancel")
                {
                    stateTracker.WaitingForComboReorderInput = false;
                    stateTracker.ReorderInputSequence = "";
                    ShowMessageEvent?.Invoke("Cancelled.");
                    stateTracker.InComboManagement = true;
                    RenderComboManagementScreen();
                    return;
                }
                
                // Accumulate numeric input
                if (input.Length == 1 && char.IsDigit(input[0]))
                {
                    int digit = int.Parse(input);
                    // Only accept digits that are valid for the combo (1 to comboActions.Count)
                    if (digit >= 1 && digit <= comboActions.Count)
                    {
                        // Check if we've already entered all required digits
                        if (stateTracker.ReorderInputSequence.Length >= comboActions.Count)
                        {
                            ShowMessageEvent?.Invoke($"You've entered all {comboActions.Count} digits. Press 0 to confirm.");
                            return;
                        }
                        
                        // Check if this digit is already in the sequence
                        if (!stateTracker.ReorderInputSequence.Contains(input))
                        {
                            stateTracker.ReorderInputSequence += input;
                            // Re-render to show the current sequence
                            if (customUIManager is CanvasUICoordinator canvasUI)
                            {
                                canvasUI.RenderComboReorderPrompt(stateManager.CurrentPlayer, stateTracker.ReorderInputSequence);
                            }
                            
                            // If we've entered all digits, suggest confirming
                            if (stateTracker.ReorderInputSequence.Length >= comboActions.Count)
                            {
                                ShowMessageEvent?.Invoke("All digits entered. Press 0 to confirm.");
                            }
                        }
                        else
                        {
                            ShowMessageEvent?.Invoke($"Number {input} is already in the sequence. Each number can only be used once.");
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke($"Please enter numbers between 1 and {comboActions.Count}.");
                    }
                }
                else
                {
                    ShowMessageEvent?.Invoke("Please enter a single digit (1-9) or press 0 to confirm.");
                }
                
                return;
            }
            
            // Normal combo management menu actions
            switch (input)
            {
                case "1":
                    PromptAddActionToCombo();
                    break;
                case "2":
                    PromptRemoveActionFromCombo();
                    break;
                case "3":
                    PromptReorderComboActions();
                    break;
                case "4":
                    AddAllAvailableActionsToCombo();
                    break;
                case "5":
                    stateTracker.InComboManagement = false;
                    ShowInventoryEvent?.Invoke();
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Press 1-5.");
                    break;
            }
        }
        
        /// <summary>
        /// Prompt user to add an action to combo
        /// </summary>
        private void PromptAddActionToCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var actionPool = stateManager.CurrentPlayer.GetActionPool();
            if (actionPool.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions available to add to combo.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            stateTracker.WaitingForComboActionSelection = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboActionSelection(stateManager.CurrentPlayer, "add");
            }
        }
        
        /// <summary>
        /// Prompt user to remove an action from combo
        /// </summary>
        private void PromptRemoveActionFromCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            if (comboActions.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions in combo sequence to remove.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            stateTracker.WaitingForComboRemoveSelection = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboActionSelection(stateManager.CurrentPlayer, "remove");
            }
        }
        
        /// <summary>
        /// Prompt user to reorder combo actions
        /// </summary>
        private void PromptReorderComboActions()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            if (comboActions.Count < 2)
            {
                ShowMessageEvent?.Invoke("You need at least 2 actions to reorder them.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            stateTracker.WaitingForComboReorderInput = true;
            stateTracker.ReorderInputSequence = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderComboReorderPrompt(stateManager.CurrentPlayer, "");
            }
        }
        
        /// <summary>
        /// Add all available actions to combo
        /// </summary>
        private void AddAllAvailableActionsToCombo()
        {
            if (stateManager.CurrentPlayer == null) return;
            
            var actionPool = stateManager.CurrentPlayer.GetActionPool();
            var comboActions = stateManager.CurrentPlayer.GetComboActions();
            
            if (actionPool.Count == 0)
            {
                ShowMessageEvent?.Invoke("No actions available in action pool to add to combo.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            var actionsToAdd = actionPool.Where(action => 
                !comboActions.Any(comboAction => comboAction.Name == action.Name)).ToList();
            
            if (actionsToAdd.Count == 0)
            {
                ShowMessageEvent?.Invoke("All available actions are already in your combo sequence.");
                stateTracker.InComboManagement = true;
                RenderComboManagementScreen();
                return;
            }
            
            int addedCount = 0;
            foreach (var action in actionsToAdd)
            {
                stateManager.CurrentPlayer.AddToCombo(action);
                addedCount++;
            }
            
            ShowMessageEvent?.Invoke($"Successfully added {addedCount} actions to combo sequence!");
            stateTracker.InComboManagement = true;
            RenderComboManagementScreen();
        }
        
        /// <summary>
        /// Validates the reorder input string
        /// </summary>
        private bool ValidateReorderInput(string input, int actionCount)
        {
            if (string.IsNullOrEmpty(input))
                return false;
                
            // Check if input contains only digits
            if (!input.All(char.IsDigit))
                return false;
                
            // Check if all numbers 1 to actionCount are present exactly once
            var numbers = input.Select(c => int.Parse(c.ToString())).ToList();
            
            if (numbers.Count != actionCount)
                return false;
                
            // Check if all numbers from 1 to actionCount are present
            for (int i = 1; i <= actionCount; i++)
            {
                if (!numbers.Contains(i))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Applies the reorder to the combo sequence
        /// </summary>
        private bool ApplyReorder(string input, List<RPGGame.Action> currentComboActions)
        {
            if (stateManager.CurrentPlayer == null) return false;
            
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
                    stateManager.CurrentPlayer.RemoveFromCombo(action);
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
                    stateManager.CurrentPlayer.AddToCombo(action);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

