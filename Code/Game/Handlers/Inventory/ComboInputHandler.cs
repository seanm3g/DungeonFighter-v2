using System;
using System.Linq;
using RPGGame.UI.Avalonia;
using ActionDelegate = System.Action;

namespace RPGGame.Handlers.Inventory
{
    /// <summary>
    /// Handles input for combo management in different states
    /// Extracted from InventoryComboManager.cs input handling logic
    /// </summary>
    public class ComboInputHandler
    {
        private readonly GameStateManager stateManager;
        private readonly InventoryStateManager stateTracker;
        private readonly IUIManager? customUIManager;
        private readonly Action<string> showMessage;
        private readonly System.Action showInventory;
        private readonly System.Action renderComboManagement;

        public ComboInputHandler(
            GameStateManager stateManager,
            InventoryStateManager stateTracker,
            IUIManager? customUIManager,
            Action<string> showMessage,
            System.Action showInventory,
            System.Action renderComboManagement)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
            this.customUIManager = customUIManager;
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this.showInventory = showInventory ?? throw new ArgumentNullException(nameof(showInventory));
            this.renderComboManagement = renderComboManagement ?? throw new ArgumentNullException(nameof(renderComboManagement));
        }

        /// <summary>
        /// Handles action selection for adding to combo
        /// </summary>
        public bool HandleAddActionSelection(string input, Character character, Action<string> onSuccess)
        {
            if (!stateTracker.WaitingForComboActionSelection || !int.TryParse(input, out int actionIndex))
                return false;

            stateTracker.WaitingForComboActionSelection = false;

            if (actionIndex == 0)
            {
                showMessage("Cancelled.");
                stateTracker.InComboManagement = true;
                renderComboManagement();
                return true;
            }

            var actionPool = character.GetActionPool();
            if (actionIndex >= 1 && actionIndex <= actionPool.Count)
            {
                var action = actionPool[actionIndex - 1];
                character.AddToCombo(action);
                onSuccess($"Added {action.Name} to combo sequence.");
            }
            else
            {
                showMessage("Invalid action selection.");
            }

            stateTracker.InComboManagement = true;
            renderComboManagement();
            return true;
        }

        /// <summary>
        /// Handles action removal selection
        /// </summary>
        public bool HandleRemoveActionSelection(string input, Character character, Action<string> onSuccess)
        {
            if (!stateTracker.WaitingForComboRemoveSelection || !int.TryParse(input, out int removeIndex))
                return false;

            stateTracker.WaitingForComboRemoveSelection = false;

            if (removeIndex == 0)
            {
                showMessage("Cancelled.");
                stateTracker.InComboManagement = true;
                renderComboManagement();
                return true;
            }

            var comboActions = character.GetComboActions();
            if (removeIndex >= 1 && removeIndex <= comboActions.Count)
            {
                var action = comboActions[removeIndex - 1];
                character.RemoveFromCombo(action);
                onSuccess($"Removed {action.Name} from combo sequence.");
            }
            else
            {
                showMessage("Invalid action selection.");
            }

            stateTracker.InComboManagement = true;
            renderComboManagement();
            return true;
        }

        /// <summary>
        /// Handles reorder input accumulation and validation
        /// </summary>
        public bool HandleReorderInput(string input, Character character, Action<string> onSuccess, Action<string> onError)
        {
            if (!stateTracker.WaitingForComboReorderInput)
                return false;

            var comboActions = character.GetComboActions();

            // Check if user wants to confirm with 0
            if (input == "0")
            {
                stateTracker.WaitingForComboReorderInput = false;

                if (string.IsNullOrEmpty(stateTracker.ReorderInputSequence))
                {
                    showMessage("Cancelled.");
                    stateTracker.ReorderInputSequence = "";
                    stateTracker.InComboManagement = true;
                    renderComboManagement();
                    return true;
                }

                // Validate and apply the accumulated sequence
                if (ComboValidator.ValidateReorderInput(stateTracker.ReorderInputSequence, comboActions.Count))
                {
                    if (ComboReorderer.ApplyReorder(character, stateTracker.ReorderInputSequence, comboActions))
                    {
                        onSuccess("Combo sequence reordered successfully!");
                    }
                    else
                    {
                        onError("Error: Failed to reorder combo sequence.");
                    }
                }
                else
                {
                    onError($"Invalid input. Please enter numbers 1-{comboActions.Count} in any order (e.g., 15324).");
                }

                stateTracker.ReorderInputSequence = "";
                stateTracker.InComboManagement = true;
                renderComboManagement();
                return true;
            }

            // Handle cancel
            if (input.ToLower() == "cancel")
            {
                stateTracker.WaitingForComboReorderInput = false;
                stateTracker.ReorderInputSequence = "";
                showMessage("Cancelled.");
                stateTracker.InComboManagement = true;
                renderComboManagement();
                return true;
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
                        showMessage($"You've entered all {comboActions.Count} digits. Press 0 to confirm.");
                        return true;
                    }

                    // Check if this digit is already in the sequence
                    if (!stateTracker.ReorderInputSequence.Contains(input))
                    {
                        stateTracker.ReorderInputSequence += input;
                        // Re-render to show the current sequence
                        if (customUIManager is CanvasUICoordinator canvasUI)
                        {
                            canvasUI.RenderComboReorderPrompt(character, stateTracker.ReorderInputSequence);
                        }

                        // If we've entered all digits, suggest confirming
                        if (stateTracker.ReorderInputSequence.Length >= comboActions.Count)
                        {
                            showMessage("All digits entered. Press 0 to confirm.");
                        }
                    }
                    else
                    {
                        showMessage($"Number {input} is already in the sequence. Each number can only be used once.");
                    }
                }
                else
                {
                    showMessage($"Please enter numbers between 1 and {comboActions.Count}.");
                }
            }
            else
            {
                showMessage("Please enter a single digit (1-9) or press 0 to confirm.");
            }

            return true;
        }
    }
}

