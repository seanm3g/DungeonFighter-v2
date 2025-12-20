namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.Editors;
    using RPGGame.GameCore.Editors;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles action editor menu display and input.
    /// </summary>
    public class ActionEditorHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private ActionEditor actionEditor;
        private bool isViewingActionList = false;
        private ActionData? selectedAction = null;
        
        // Form state for creating/editing actions
        private ActionData? newAction;
        private int currentFormStep = 0;
        private string currentFormInput = "";
        private bool isEditMode = false;
        private string? originalActionName = null;
        private bool isDeletingAction = false;
        private ActionData? actionToDelete = null;
        private readonly string[] formSteps = {
            "Name",
            "Type (Attack/Heal/Buff/Debuff/Spell)",
            "TargetType (Self/SingleTarget/AreaOfEffect/Environment)",
            "BaseValue (number)",
            "Description",
            "DamageMultiplier (number, default 1.0)",
            "Length (number, default 1.0)",
            "Cooldown (number, default 0)"
        };
        
        // Extracted components
        private readonly RPGGame.GameCore.Editors.ActionFormProcessor formProcessor;
        private readonly RPGGame.GameCore.Editors.ActionListManager listManager;
        
        // Delegates
        public delegate void OnShowDeveloperMenu();
        
        public event OnShowDeveloperMenu? ShowDeveloperMenuEvent;

        public ActionEditorHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.actionEditor = new ActionEditor();
            this.formProcessor = new RPGGame.GameCore.Editors.ActionFormProcessor(ShowMessage);
            this.listManager = new RPGGame.GameCore.Editors.ActionListManager(actionEditor, customUIManager, ShowMessage);
        }

        /// <summary>
        /// Display the action editor menu
        /// </summary>
        public void ShowActionEditor()
        {
            isViewingActionList = false;
            selectedAction = null;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderActionEditor();
            }
            stateManager.TransitionToState(GameState.ActionEditor);
        }
        
        /// <summary>
        /// Handle input when viewing action details
        /// </summary>
        public void HandleActionDetailInput(string input)
        {
            if (selectedAction == null)
            {
                ShowActionEditor();
                return;
            }
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "0":
                    case "back":
                        // Return to action list
                        isViewingActionList = true;
                        stateManager.TransitionToState(GameState.ActionEditor);
                        ShowActionList();
                        break;
                    case "edit":
                    case "e":
                        // Edit action
                        if (selectedAction != null)
                        {
                            StartEditActionWithAction(selectedAction);
                        }
                        break;
                    default:
                        // Refresh the view
                        canvasUI.RenderActionDetails(selectedAction);
                        break;
                }
            }
        }

        /// <summary>
        /// Handle action editor menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // If we're viewing the action list, handle numeric input as action selection
                if (isViewingActionList)
                {
                    if (int.TryParse(input, out int actionIndex))
                    {
                        HandleActionSelection(actionIndex);
                        return;
                    }
                    
                    switch (input)
                    {
                        case "0":
                            // Back to Action Editor from list
                            isViewingActionList = false;
                            isDeletingAction = false;
                            isEditMode = false;
                            ShowActionEditor();
                            break;
                        case "up":
                        case "down":
                            // Handle scrolling in list view
                            HandleCombatScroll(input);
                            break;
                        default:
                            // Refresh the list
                            ShowActionList();
                            break;
                    }
                    return;
                }
                
                // Otherwise, handle as menu options
                switch (input)
                {
                    case "1":
                        // Create New Action
                        StartCreateAction();
                        break;
                    case "2":
                        // Edit Existing Action
                        StartEditAction();
                        break;
                    case "3":
                        // Delete Action
                        StartDeleteAction();
                        break;
                    case "4":
                        // List All Actions
                        ShowActionList();
                        break;
                    case "0":
                        // Back to Developer Menu
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        ShowDeveloperMenuEvent?.Invoke();
                        break;
                    default:
                        // Any other input refreshes the menu
                        canvasUI.ResetDeleteConfirmation();
                        ShowActionEditor();
                        break;
                }
            }
            else
            {
                ScrollDebugLogger.Log($"ActionEditorHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Show the list of all actions
        /// </summary>
        private void ShowActionList()
        {
            isViewingActionList = true;
            listManager.ShowActionList();
        }
        
        /// <summary>
        /// Show action details for viewing/editing
        /// </summary>
        private void ShowActionDetails(ActionData action)
        {
            selectedAction = action;
            isViewingActionList = false;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderActionDetails(action);
            }
            stateManager.TransitionToState(GameState.ViewAction);
        }

        /// <summary>
        /// Handle action selection from the list
        /// </summary>
        private void HandleActionSelection(int selectionNumber)
        {
            var selected = listManager.HandleActionSelection(selectionNumber, (action) => {
                if (isDeletingAction)
                {
                    // Show delete confirmation
                    actionToDelete = action;
                    ShowDeleteConfirmation(action);
                }
                else if (isEditMode || !isViewingActionList)
                {
                    // Start editing the selected action
                    StartEditActionWithAction(action);
                }
                else
                {
                    // Show action details
                    ShowActionDetails(action);
                }
            });
            if (selected != null && !isDeletingAction && !isEditMode)
            {
                selectedAction = selected;
            }
        }
        
        /// <summary>
        /// Handle scrolling
        /// </summary>
        private void HandleCombatScroll(string input)
        {
            listManager.HandleScroll(input);
        }

        /// <summary>
        /// Start creating a new action
        /// </summary>
        private void StartCreateAction()
        {
            isEditMode = false;
            originalActionName = null;
            newAction = new ActionData
            {
                Name = "",
                Type = "Attack",
                TargetType = "SingleTarget",
                BaseValue = 0,
                Range = 1,
                Cooldown = 0,
                Description = "",
                DamageMultiplier = 1.0,
                Length = 1.0,
                Tags = new List<string>()
            };
            currentFormStep = 0;
            currentFormInput = "";
            stateManager.TransitionToState(GameState.CreateAction);
            ScrollDebugLogger.Log($"ActionEditorHandler: StartCreateAction - entered CreateAction state (verified: {stateManager.CurrentState == GameState.CreateAction})");
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput, isEditMode);
                
                // Focus the canvas to ensure keyboard input is captured
                canvasUI.FocusCanvas();
            }
        }

        /// <summary>
        /// Start editing an existing action - show action list first
        /// </summary>
        private void StartEditAction()
        {
            isViewingActionList = true;
            isEditMode = false;
            originalActionName = null;
            listManager.ShowActionList();
        }

        /// <summary>
        /// Start editing a specific action (called from list selection or detail view)
        /// </summary>
        private void StartEditActionWithAction(ActionData action)
        {
            isEditMode = true;
            originalActionName = action.Name;
            
            // Create a copy of the action for editing
            newAction = new ActionData
            {
                Name = action.Name,
                Type = action.Type,
                TargetType = action.TargetType,
                BaseValue = action.BaseValue,
                Range = action.Range,
                Cooldown = action.Cooldown,
                Description = action.Description ?? "",
                DamageMultiplier = action.DamageMultiplier,
                Length = action.Length,
                Tags = action.Tags != null ? new List<string>(action.Tags) : new List<string>(),
                CausesBleed = action.CausesBleed,
                CausesWeaken = action.CausesWeaken,
                CausesSlow = action.CausesSlow,
                CausesPoison = action.CausesPoison,
                CausesBurn = action.CausesBurn,
                IsComboAction = action.IsComboAction,
                ComboOrder = action.ComboOrder,
                ComboBonusAmount = action.ComboBonusAmount,
                ComboBonusDuration = action.ComboBonusDuration,
                StatBonus = action.StatBonus,
                StatBonusType = action.StatBonusType,
                StatBonusDuration = action.StatBonusDuration,
                RollBonus = action.RollBonus,
                MultiHitCount = action.MultiHitCount,
                SelfDamagePercent = action.SelfDamagePercent,
                SkipNextTurn = action.SkipNextTurn,
                RepeatLastAction = action.RepeatLastAction,
                EnemyRollPenalty = action.EnemyRollPenalty
            };
            
            currentFormStep = 0;
            currentFormInput = "";
            isViewingActionList = false;
            stateManager.TransitionToState(GameState.EditAction);
            ScrollDebugLogger.Log($"ActionEditorHandler: StartEditActionWithAction - entered EditAction state for '{originalActionName}'");
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput, isEditMode);
                
                // Focus the canvas to ensure keyboard input is captured
                canvasUI.FocusCanvas();
            }
        }

        /// <summary>
        /// Start deleting an action - show action list first
        /// </summary>
        private void StartDeleteAction()
        {
            isViewingActionList = true;
            isDeletingAction = true;
            actionToDelete = null;
            listManager.ShowActionList();
        }

        /// <summary>
        /// Update the form display with current input
        /// </summary>
        public void UpdateFormInput(string input)
        {
            ScrollDebugLogger.LogAlways($"[ActionEditor] UpdateFormInput called with input='{input}'");
            currentFormInput = input;
            if (newAction != null && customUIManager is CanvasUICoordinator canvasUI)
            {
                ScrollDebugLogger.LogAlways($"[ActionEditor] Rendering form with currentFormInput='{currentFormInput}'");
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput, isEditMode);
                canvasUI.Refresh(); // Ensure canvas is refreshed after rendering
                ScrollDebugLogger.LogAlways($"[ActionEditor] Form rendered and refreshed");
            }
            else
            {
                ScrollDebugLogger.LogAlways($"[ActionEditor] UpdateFormInput: newAction={newAction != null}, canvasUI={customUIManager is CanvasUICoordinator}");
            }
        }

        /// <summary>
        /// Handle input for creating an action
        /// </summary>
        public void HandleCreateActionInput(string input)
        {
            if (newAction == null)
            {
                ShowActionEditor();
                return;
            }

            // Handle special commands
            if (HandleFormCommand(input))
            {
                return;
            }

            // Skip empty input (unless it's enter to submit)
            if (string.IsNullOrWhiteSpace(input) && input != "enter")
            {
                return;
            }

            // Process current step with the input
            bool stepComplete = formProcessor.ProcessFormStep(newAction, currentFormStep, input);
            
            if (stepComplete)
            {
                currentFormInput = "";
                if (currentFormStep < formSteps.Length - 1)
                {
                    currentFormStep++;
                    RefreshForm();
                }
                else
                {
                    SaveNewAction();
                }
            }
            else
            {
                RefreshForm();
            }
        }

        /// <summary>
        /// Handles form command inputs (cancel, enter, back). Returns true if command was handled.
        /// </summary>
        private bool HandleFormCommand(string input)
        {
            if (input == "cancel" || input == "0")
            {
                newAction = null;
                currentFormStep = 0;
                ShowActionEditor();
                return true;
            }

            if (input == "enter")
            {
                return true; // No-op, just consume the input
            }

            if (input == "back" && currentFormStep > 0)
            {
                currentFormStep--;
                currentFormInput = "";
                RefreshForm();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the form display with current state.
        /// </summary>
        private void RefreshForm()
        {
            if (customUIManager is CanvasUICoordinator canvasUI && newAction != null)
            {
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput, isEditMode);
            }
        }

        /// <summary>
        /// Save the newly created or edited action
        /// </summary>
        private void SaveNewAction()
        {
            if (newAction == null)
            {
                ShowActionEditor();
                return;
            }

            bool success;
            if (isEditMode && !string.IsNullOrEmpty(originalActionName))
            {
                // Validate before saving
                string? validationError = ValidateAction(newAction, originalActionName);
                if (validationError != null)
                {
                    ShowMessage($"Validation error: {validationError}");
                    return;
                }

                success = actionEditor.UpdateAction(originalActionName, newAction);
                
                if (success)
                {
                    ShowMessage($"Action '{newAction.Name}' updated successfully!");
                    newAction = null;
                    originalActionName = null;
                    isEditMode = false;
                    currentFormStep = 0;
                    ShowActionEditor();
                }
                else
                {
                    ShowMessage($"Failed to update action. Action '{newAction.Name}' may already exist or '{originalActionName}' may not exist.");
                }
            }
            else
            {
                // Validate before saving
                string? validationError = ValidateAction(newAction, null);
                if (validationError != null)
                {
                    ShowMessage($"Validation error: {validationError}");
                    return;
                }

                success = actionEditor.CreateAction(newAction);
                
                if (success)
                {
                    ShowMessage($"Action '{newAction.Name}' created successfully!");
                    newAction = null;
                    currentFormStep = 0;
                    ShowActionEditor();
                }
                else
                {
                    ShowMessage($"Failed to create action. Action '{newAction.Name}' may already exist.");
                }
            }
        }

        /// <summary>
        /// Validate an action before saving
        /// </summary>
        private string? ValidateAction(ActionData action, string? originalName)
        {
            return actionEditor.ValidateAction(action, originalName);
        }

        /// <summary>
        /// Show delete confirmation screen
        /// </summary>
        private void ShowDeleteConfirmation(ActionData action)
        {
            actionToDelete = action;
            isViewingActionList = false;
            stateManager.TransitionToState(GameState.DeleteActionConfirmation);
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderDeleteActionConfirmation(action);
            }
        }

        /// <summary>
        /// Handle delete confirmation input
        /// </summary>
        public void HandleDeleteConfirmationInput(string input)
        {
            if (actionToDelete == null)
            {
                ShowActionEditor();
                return;
            }

            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                if (input == "cancel" || input == "0" || input.ToLower() == "no")
                {
                    // Cancel deletion
                    actionToDelete = null;
                    isDeletingAction = false;
                    ShowActionEditor();
                }
                else if (input.ToUpper() == "DELETE" || input.Equals(actionToDelete.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // Confirm deletion
                    bool success = actionEditor.DeleteAction(actionToDelete.Name);
                    
                    if (success)
                    {
                        ShowMessage($"Action '{actionToDelete.Name}' deleted successfully!");
                        actionToDelete = null;
                        isDeletingAction = false;
                        ShowActionEditor();
                    }
                    else
                    {
                        ShowMessage($"Failed to delete action '{actionToDelete.Name}'. It may not exist.");
                        actionToDelete = null;
                        isDeletingAction = false;
                        ShowActionEditor();
                    }
                }
                else
                {
                    // Invalid confirmation - show error and refresh
                    canvasUI.RenderDeleteActionConfirmation(actionToDelete, $"Invalid confirmation. Type 'DELETE' or the action name '{actionToDelete.Name}' to confirm, or 'cancel' to abort.");
                }
            }
        }

        private void ShowMessage(string message)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowMessage(message);
            }
        }
    }
}

