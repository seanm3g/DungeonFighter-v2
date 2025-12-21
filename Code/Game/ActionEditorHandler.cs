namespace RPGGame
{
    using System;
    using RPGGame.Editors;
    using RPGGame.GameCore.Editors;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;
    using ActionDelegate = System.Action;

    /// <summary>
    /// Handles action editor menu display and input.
    /// Refactored to use specialized managers for reduced complexity.
    /// </summary>
    public class ActionEditorHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private ActionEditor actionEditor;
        private bool isViewingActionList = false;
        private ActionData? selectedAction = null;
        private bool isDeletingAction = false;
        
        // Extracted components
        private readonly ActionFormProcessor formProcessor;
        private readonly ActionListManager listManager;
        private readonly ActionStateManager stateManagerHelper;
        private readonly DeleteConfirmationHandler deleteHandler;
        
        // Delegates
        public delegate void OnShowDeveloperMenu();
        
        public event OnShowDeveloperMenu? ShowDeveloperMenuEvent;

        public ActionEditorHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.actionEditor = new ActionEditor();
            this.formProcessor = new ActionFormProcessor(ShowMessage);
            this.listManager = new ActionListManager(actionEditor, customUIManager, ShowMessage);
            this.stateManagerHelper = new ActionStateManager(stateManager, customUIManager, ShowMessage);
            this.deleteHandler = new DeleteConfirmationHandler(
                stateManager,
                actionEditor,
                customUIManager,
                ShowMessage,
                () => { isDeletingAction = false; ShowActionEditor(); });
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
                        isViewingActionList = true;
                        stateManager.TransitionToState(GameState.ActionEditor);
                        ShowActionList();
                        break;
                    case "edit":
                    case "e":
                        if (selectedAction != null)
                        {
                            StartEditActionWithAction(selectedAction);
                        }
                        break;
                    default:
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
                            isViewingActionList = false;
                            isDeletingAction = false;
                            stateManagerHelper.Reset();
                            ShowActionEditor();
                            break;
                        case "up":
                        case "down":
                            HandleCombatScroll(input);
                            break;
                        default:
                            ShowActionList();
                            break;
                    }
                    return;
                }
                
                switch (input)
                {
                    case "1":
                        StartCreateAction();
                        break;
                    case "2":
                        StartEditAction();
                        break;
                    case "3":
                        StartDeleteAction();
                        break;
                    case "4":
                        ShowActionList();
                        break;
                    case "0":
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        ShowDeveloperMenuEvent?.Invoke();
                        break;
                    default:
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

        private void ShowActionList()
        {
            isViewingActionList = true;
            listManager.ShowActionList();
        }
        
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

        private void HandleActionSelection(int selectionNumber)
        {
            var selected = listManager.HandleActionSelection(selectionNumber, (action) => {
                if (isDeletingAction)
                {
                    deleteHandler.ShowDeleteConfirmation(action);
                }
                else if (stateManagerHelper.IsEditMode || !isViewingActionList)
                {
                    StartEditActionWithAction(action);
                }
                else
                {
                    ShowActionDetails(action);
                }
            });
            if (selected != null && !isDeletingAction && !stateManagerHelper.IsEditMode)
            {
                selectedAction = selected;
            }
        }
        
        private void HandleCombatScroll(string input)
        {
            listManager.HandleScroll(input);
        }

        private void StartCreateAction()
        {
            stateManagerHelper.StartCreateAction();
        }

        private void StartEditAction()
        {
            isViewingActionList = true;
            stateManagerHelper.Reset();
            listManager.ShowActionList();
        }

        private void StartEditActionWithAction(ActionData action)
        {
            stateManagerHelper.StartEditAction(action);
            isViewingActionList = false;
        }

        private void StartDeleteAction()
        {
            isViewingActionList = true;
            isDeletingAction = true;
            deleteHandler.Reset();
            listManager.ShowActionList();
        }

        public void UpdateFormInput(string input)
        {
            ScrollDebugLogger.LogAlways($"[ActionEditor] UpdateFormInput called with input='{input}'");
            stateManagerHelper.CurrentFormInput = input;
            if (stateManagerHelper.NewAction != null && customUIManager is CanvasUICoordinator canvasUI)
            {
                ScrollDebugLogger.LogAlways($"[ActionEditor] Rendering form with currentFormInput='{stateManagerHelper.CurrentFormInput}'");
                canvasUI.RenderCreateActionForm(
                    stateManagerHelper.NewAction,
                    stateManagerHelper.CurrentFormStep,
                    stateManagerHelper.FormSteps,
                    stateManagerHelper.CurrentFormInput,
                    stateManagerHelper.IsEditMode);
                canvasUI.Refresh();
                ScrollDebugLogger.LogAlways($"[ActionEditor] Form rendered and refreshed");
            }
            else
            {
                ScrollDebugLogger.LogAlways($"[ActionEditor] UpdateFormInput: newAction={stateManagerHelper.NewAction != null}, canvasUI={customUIManager is CanvasUICoordinator}");
            }
        }

        public void HandleCreateActionInput(string input)
        {
            if (stateManagerHelper.NewAction == null)
            {
                ShowActionEditor();
                return;
            }

            if (HandleFormCommand(input))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(input) && input != "enter")
            {
                return;
            }

            bool stepComplete = formProcessor.ProcessFormStep(
                stateManagerHelper.NewAction,
                stateManagerHelper.CurrentFormStep,
                input);
            
            if (stepComplete)
            {
                stateManagerHelper.CurrentFormInput = "";
                if (stateManagerHelper.CurrentFormStep < stateManagerHelper.FormSteps.Length - 1)
                {
                    stateManagerHelper.MoveToNextStep();
                    stateManagerHelper.RefreshForm();
                }
                else
                {
                    SaveNewAction();
                }
            }
            else
            {
                stateManagerHelper.RefreshForm();
            }
        }

        private bool HandleFormCommand(string input)
        {
            if (input == "cancel" || input == "0")
            {
                stateManagerHelper.Reset();
                ShowActionEditor();
                return true;
            }

            if (input == "enter")
            {
                return true;
            }

            if (input == "back" && stateManagerHelper.CurrentFormStep > 0)
            {
                stateManagerHelper.MoveToPreviousStep();
                stateManagerHelper.RefreshForm();
                return true;
            }

            return false;
        }

        private void SaveNewAction()
        {
            if (stateManagerHelper.NewAction == null)
            {
                ShowActionEditor();
                return;
            }

            bool success;
            if (stateManagerHelper.IsEditMode && !string.IsNullOrEmpty(stateManagerHelper.OriginalActionName))
            {
                string? validationError = actionEditor.ValidateAction(
                    stateManagerHelper.NewAction,
                    stateManagerHelper.OriginalActionName);
                if (validationError != null)
                {
                    ShowMessage($"Validation error: {validationError}");
                    return;
                }

                success = actionEditor.UpdateAction(
                    stateManagerHelper.OriginalActionName,
                    stateManagerHelper.NewAction);
                
                if (success)
                {
                    ShowMessage($"Action '{stateManagerHelper.NewAction.Name}' updated successfully!");
                    stateManagerHelper.Reset();
                    ShowActionEditor();
                }
                else
                {
                    ShowMessage($"Failed to update action. Action '{stateManagerHelper.NewAction.Name}' may already exist or '{stateManagerHelper.OriginalActionName}' may not exist.");
                }
            }
            else
            {
                string? validationError = actionEditor.ValidateAction(
                    stateManagerHelper.NewAction,
                    null);
                if (validationError != null)
                {
                    ShowMessage($"Validation error: {validationError}");
                    return;
                }

                success = actionEditor.CreateAction(stateManagerHelper.NewAction);
                
                if (success)
                {
                    ShowMessage($"Action '{stateManagerHelper.NewAction.Name}' created successfully!");
                    stateManagerHelper.Reset();
                    ShowActionEditor();
                }
                else
                {
                    ShowMessage($"Failed to create action. Action '{stateManagerHelper.NewAction.Name}' may already exist.");
                }
            }
        }

        public void HandleDeleteConfirmationInput(string input)
        {
            deleteHandler.HandleInput(input);
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
