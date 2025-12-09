using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia;

namespace RPGGame.GameCore.Editors
{
    /// <summary>
    /// Manages action list display, selection, and pagination
    /// Extracted from ActionEditorHandler.cs action list management
    /// </summary>
    public class ActionListManager
    {
        private readonly ActionEditor actionEditor;
        private readonly IUIManager? customUIManager;
        private readonly Action<string> showMessage;
        private int currentListPage = 0;
        private const int ActionsPerPage = 20;

        public ActionListManager(
            ActionEditor actionEditor,
            IUIManager? customUIManager,
            Action<string> showMessage)
        {
            this.actionEditor = actionEditor ?? throw new ArgumentNullException(nameof(actionEditor));
            this.customUIManager = customUIManager;
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        /// <summary>
        /// Show the list of all actions
        /// </summary>
        public void ShowActionList()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderActionList(actionEditor.GetActions(), currentListPage);
            }
        }

        /// <summary>
        /// Handle action selection from the list
        /// </summary>
        public ActionData? HandleActionSelection(int selectionNumber, Action<ActionData> showActionDetails)
        {
            var actions = actionEditor.GetActions();
            int startIndex = currentListPage * ActionsPerPage;
            int actionIndex = startIndex + selectionNumber - 1; // Convert 1-based to 0-based

            if (actionIndex >= 0 && actionIndex < actions.Count)
            {
                var selectedAction = actions[actionIndex];
                showActionDetails(selectedAction);
                return selectedAction;
            }
            else
            {
                showMessage($"Invalid selection. Please choose a number between 1 and {Math.Min(ActionsPerPage, actions.Count - startIndex)}.");
                return null;
            }
        }

        /// <summary>
        /// Handle scrolling
        /// </summary>
        public void HandleScroll(string input)
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

        public int CurrentListPage => currentListPage;
    }
}

