using System;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia;
using ActionDelegate = System.Action;

namespace RPGGame.GameCore.Editors
{
    /// <summary>
    /// Handles delete confirmation logic for actions
    /// Extracted from ActionEditorHandler.cs to reduce file size
    /// </summary>
    public class DeleteConfirmationHandler
    {
        private readonly GameStateManager stateManager;
        private readonly ActionEditor actionEditor;
        private readonly IUIManager? customUIManager;
        private readonly Action<string> showMessage;
        private readonly ActionDelegate onCancel;
        
        public ActionData? ActionToDelete { get; private set; }

        public DeleteConfirmationHandler(
            GameStateManager stateManager,
            ActionEditor actionEditor,
            IUIManager? customUIManager,
            Action<string> showMessage,
            ActionDelegate onCancel)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.actionEditor = actionEditor ?? throw new ArgumentNullException(nameof(actionEditor));
            this.customUIManager = customUIManager;
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this.onCancel = onCancel ?? throw new ArgumentNullException(nameof(onCancel));
        }

        public void ShowDeleteConfirmation(ActionData action)
        {
            ActionToDelete = action;
            stateManager.TransitionToState(GameState.DeleteActionConfirmation);
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderDeleteActionConfirmation(action);
            }
        }

        public void HandleInput(string input)
        {
            if (ActionToDelete == null)
            {
                onCancel();
                return;
            }

            if (input == "cancel" || input == "0" || input.ToLower() == "no")
            {
                CancelDeletion();
            }
            else if (input.ToUpper() == "DELETE" || input.Equals(ActionToDelete.Name, StringComparison.OrdinalIgnoreCase))
            {
                ConfirmDeletion();
            }
            else
            {
                ShowInvalidConfirmation();
            }
        }

        private void CancelDeletion()
        {
            ActionToDelete = null;
            onCancel();
        }

        private void ConfirmDeletion()
        {
            if (ActionToDelete == null) return;
            
            bool success = actionEditor.DeleteAction(ActionToDelete.Name);
            
            if (success)
            {
                showMessage($"Action '{ActionToDelete.Name}' deleted successfully!");
            }
            else
            {
                showMessage($"Failed to delete action '{ActionToDelete.Name}'. It may not exist.");
            }
            
            ActionToDelete = null;
            onCancel();
        }

        private void ShowInvalidConfirmation()
        {
            if (ActionToDelete == null || customUIManager is not CanvasUICoordinator canvasUI) return;
            
            canvasUI.RenderDeleteActionConfirmation(
                ActionToDelete, 
                $"Invalid confirmation. Type 'DELETE' or the action name '{ActionToDelete.Name}' to confirm, or 'cancel' to abort.");
        }

        public void Reset()
        {
            ActionToDelete = null;
        }
    }
}

