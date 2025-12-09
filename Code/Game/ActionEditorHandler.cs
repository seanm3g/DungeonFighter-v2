namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame.Editors;
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
        private int currentListPage = 0;
        private const int ActionsPerPage = 20;
        private bool isViewingActionList = false;
        private ActionData? selectedAction = null;
        
        // Form state for creating actions
        private ActionData? newAction;
        private int currentFormStep = 0;
        private string currentFormInput = "";
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
        
        // Delegates
        public delegate void OnShowDeveloperMenu();
        
        public event OnShowDeveloperMenu? ShowDeveloperMenuEvent;

        public ActionEditorHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.actionEditor = new ActionEditor();
        }

        /// <summary>
        /// Display the action editor menu
        /// </summary>
        public void ShowActionEditor()
        {
            isViewingActionList = false;
            selectedAction = null;
            ScrollDebugLogger.Log("ActionEditorHandler: ShowActionEditor called");
            DebugLogger.Log("ActionEditorHandler", "ShowActionEditor called");
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                ScrollDebugLogger.Log("ActionEditorHandler: UI manager is CanvasUICoordinator, rendering action editor");
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderActionEditor();
                ScrollDebugLogger.Log("ActionEditorHandler: RenderActionEditor completed");
            }
            stateManager.TransitionToState(GameState.ActionEditor);
            ScrollDebugLogger.Log("ActionEditorHandler: State transitioned to ActionEditor");
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
                        // Edit action - for now just show a message
                        ShowMessage("Edit functionality coming soon. Use Actions.json file directly for now.");
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
            DebugLogger.Log("ActionEditorHandler", $"HandleMenuInput called with input: '{input}'");
            ScrollDebugLogger.Log($"ActionEditorHandler.HandleMenuInput: input='{input}', state={stateManager.CurrentState}");
            
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
                        DebugLogger.Log("ActionEditorHandler", "Showing create action screen");
                        StartCreateAction();
                        break;
                    case "2":
                        // Edit Existing Action
                        DebugLogger.Log("ActionEditorHandler", "Showing edit action screen");
                        ShowMessage("Edit Existing Action: Feature coming soon. Use Actions.json file directly for now.");
                        break;
                    case "3":
                        // Delete Action
                        DebugLogger.Log("ActionEditorHandler", "Showing delete action screen");
                        ShowMessage("Delete Action: Feature coming soon. Use Actions.json file directly for now.");
                        break;
                    case "4":
                        // List All Actions
                        DebugLogger.Log("ActionEditorHandler", "Showing action list");
                        ShowActionList();
                        break;
                    case "0":
                        // Back to Developer Menu
                        DebugLogger.Log("ActionEditorHandler", "Returning to Developer Menu");
                        ScrollDebugLogger.Log("ActionEditorHandler: Returning to Developer Menu");
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        ShowDeveloperMenuEvent?.Invoke();
                        break;
                    default:
                        // Any other input refreshes the menu
                        DebugLogger.Log("ActionEditorHandler", $"Unknown input '{input}', refreshing action editor");
                        ScrollDebugLogger.Log($"ActionEditorHandler: Unknown input '{input}', refreshing action editor");
                        canvasUI.ResetDeleteConfirmation();
                        ShowActionEditor();
                        break;
                }
            }
            else
            {
                DebugLogger.Log("ActionEditorHandler", "ERROR: customUIManager is not CanvasUICoordinator");
                ScrollDebugLogger.Log($"ActionEditorHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Show the list of all actions
        /// </summary>
        private void ShowActionList()
        {
            isViewingActionList = true;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderActionList(actionEditor.GetActions(), currentListPage);
            }
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
            var actions = actionEditor.GetActions();
            int startIndex = currentListPage * ActionsPerPage;
            int actionIndex = startIndex + selectionNumber - 1; // Convert 1-based to 0-based
            
            if (actionIndex >= 0 && actionIndex < actions.Count)
            {
                var selectedAction = actions[actionIndex];
                ShowActionDetails(selectedAction);
            }
            else
            {
                ShowMessage($"Invalid selection. Please choose a number between 1 and {Math.Min(ActionsPerPage, actions.Count - startIndex)}.");
            }
        }
        
        /// <summary>
        /// Handle scrolling
        /// </summary>
        private void HandleCombatScroll(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                if (input == "up")
                {
                    currentListPage = Math.Max(0, currentListPage - 1);
                    ShowActionList();
                }
                else if (input == "down")
                {
                    var actions = actionEditor.GetActions();
                    int maxPages = (int)Math.Ceiling(actions.Count / (double)ActionsPerPage);
                    currentListPage = Math.Min(maxPages - 1, currentListPage + 1);
                    ShowActionList();
                }
            }
        }

        /// <summary>
        /// Start creating a new action
        /// </summary>
        private void StartCreateAction()
        {
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
            
            DebugLogger.Log("ActionEditorHandler", $"StartCreateAction: Before state transition, current state={stateManager.CurrentState}");
            stateManager.TransitionToState(GameState.CreateAction);
            DebugLogger.Log("ActionEditorHandler", $"StartCreateAction: After state transition, new state={stateManager.CurrentState}");
            
            ScrollDebugLogger.Log($"ActionEditorHandler: StartCreateAction - entered CreateAction state (verified: {stateManager.CurrentState == GameState.CreateAction})");
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput);
                
                // Try to focus the window to ensure keyboard input is captured
                // This is a workaround - ideally we'd have a reference to MainWindow
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var window = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                            ? desktop.MainWindow
                            : null;
                        if (window != null)
                        {
                            window.Focus();
                            DebugLogger.Log("ActionEditorHandler", "Focused main window for text input");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log("ActionEditorHandler", $"Could not focus window: {ex.Message}");
                    }
                }, Avalonia.Threading.DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Update the form display with current input
        /// </summary>
        public void UpdateFormInput(string input)
        {
            currentFormInput = input;
            if (newAction != null && customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("ActionEditorHandler", $"UpdateFormInput: input='{input}', currentStep={currentFormStep}");
                ScrollDebugLogger.Log($"ActionEditorHandler: UpdateFormInput called with input='{input}'");
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput);
                canvasUI.Refresh(); // Ensure canvas is refreshed after rendering
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
            if (input == "cancel" || input == "0")
            {
                // Cancel creation
                newAction = null;
                currentFormStep = 0;
                ShowActionEditor();
                return;
            }

            if (input == "enter")
            {
                // Enter key pressed - if we have text input, process it
                // Otherwise, this is just a no-op
                return;
            }

            if (input == "back" && currentFormStep > 0)
            {
                // Go back one step
                currentFormStep--;
                currentFormInput = "";
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput);
                }
                return;
            }

            // Skip empty input (unless it's enter to submit)
            if (string.IsNullOrWhiteSpace(input) && input != "enter")
            {
                return;
            }

            // Process current step with the input
            bool stepComplete = ProcessFormStep(input);
            
            if (stepComplete)
            {
                currentFormInput = "";
                if (currentFormStep < formSteps.Length - 1)
                {
                    // Move to next step
                    currentFormStep++;
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput);
                    }
                }
                else
                {
                    // All steps complete, save the action
                    SaveNewAction();
                }
            }
            else
            {
                // Step not complete, refresh form to show error/current state
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput);
                }
            }
        }

        /// <summary>
        /// Process input for the current form step
        /// </summary>
        private bool ProcessFormStep(string input)
        {
            if (newAction == null) return false;

            switch (currentFormStep)
            {
                case 0: // Name
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        newAction.Name = input.Trim().ToUpper();
                        return true;
                    }
                    ShowMessage("Name cannot be empty. Please enter a name.");
                    return false;

                case 1: // Type
                    string typeLower = input.Trim().ToLower();
                    if (typeLower == "attack" || typeLower == "heal" || typeLower == "buff" || 
                        typeLower == "debuff" || typeLower == "spell" || typeLower == "interact" || 
                        typeLower == "move" || typeLower == "useitem")
                    {
                        newAction.Type = char.ToUpper(typeLower[0]) + typeLower.Substring(1);
                        return true;
                    }
                    ShowMessage("Invalid type. Use: Attack, Heal, Buff, Debuff, Spell, Interact, Move, or UseItem");
                    return false;

                case 2: // TargetType
                    string targetLower = input.Trim().ToLower();
                    if (targetLower == "self" || targetLower == "singletarget" || 
                        targetLower == "areaofeffect" || targetLower == "environment")
                    {
                        newAction.TargetType = targetLower == "singletarget" ? "SingleTarget" :
                                              targetLower == "areaofeffect" ? "AreaOfEffect" :
                                              char.ToUpper(targetLower[0]) + targetLower.Substring(1);
                        return true;
                    }
                    ShowMessage("Invalid target type. Use: Self, SingleTarget, AreaOfEffect, or Environment");
                    return false;

                case 3: // BaseValue
                    if (int.TryParse(input.Trim(), out int baseValue))
                    {
                        newAction.BaseValue = baseValue;
                        return true;
                    }
                    ShowMessage("Invalid number. Please enter a valid integer.");
                    return false;

                case 4: // Description
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        newAction.Description = input.Trim();
                        return true;
                    }
                    ShowMessage("Description cannot be empty. Please enter a description.");
                    return false;

                case 5: // DamageMultiplier
                    if (double.TryParse(input.Trim(), out double damageMult))
                    {
                        newAction.DamageMultiplier = damageMult;
                        return true;
                    }
                    ShowMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5).");
                    return false;

                case 6: // Length
                    if (double.TryParse(input.Trim(), out double length))
                    {
                        newAction.Length = length;
                        return true;
                    }
                    ShowMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5).");
                    return false;

                case 7: // Cooldown
                    if (int.TryParse(input.Trim(), out int cooldown))
                    {
                        newAction.Cooldown = cooldown;
                        return true;
                    }
                    ShowMessage("Invalid number. Please enter a valid integer.");
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Save the newly created action
        /// </summary>
        private void SaveNewAction()
        {
            if (newAction == null)
            {
                ShowActionEditor();
                return;
            }

            bool success = actionEditor.CreateAction(newAction);
            
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

        private void ShowMessage(string message)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowMessage(message);
            }
        }
    }
}

