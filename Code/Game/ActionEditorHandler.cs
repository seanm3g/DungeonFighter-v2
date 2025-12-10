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
            var selected = listManager.HandleActionSelection(selectionNumber, ShowActionDetails);
            if (selected != null)
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
                
                // Focus the window to ensure keyboard input is captured
                WindowFocusHelper.FocusMainWindow("ActionEditorHandler");
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
                canvasUI.RenderCreateActionForm(newAction, currentFormStep, formSteps, currentFormInput);
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

