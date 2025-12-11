namespace RPGGame
{
    using System;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles developer menu display and input.
    /// </summary>
    public class DeveloperMenuHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        
        // Delegates
        public delegate void OnShowSettings();
        public delegate void OnShowVariableEditor();
        public delegate void OnShowActionEditor();
        public delegate void OnShowBattleStatistics();
        
        public event OnShowSettings? ShowSettingsEvent;
        public event OnShowVariableEditor? ShowVariableEditorEvent;
        public event OnShowActionEditor? ShowActionEditorEvent;
        public event OnShowBattleStatistics? ShowBattleStatisticsEvent;

        public DeveloperMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display the developer menu
        /// </summary>
        public void ShowDeveloperMenu()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderDeveloperMenu();
            }
            else
            {
                ScrollDebugLogger.Log($"DeveloperMenuHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.DeveloperMenu);
        }

        /// <summary>
        /// Handle developer menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
                        // Edit Game Variables
                        ShowVariableEditorEvent?.Invoke();
                        break;
                    case "2":
                        // Edit Actions
                        ShowActionEditorEvent?.Invoke();
                        break;
                    case "3":
                        // Battle Statistics
                        ShowBattleStatisticsEvent?.Invoke();
                        break;
                    case "0":
                        // Back to Settings
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.Settings);
                        ShowSettingsEvent?.Invoke();
                        break;
                    default:
                        // Any other input refreshes the menu
                        canvasUI.ResetDeleteConfirmation();
                        ShowDeveloperMenu();
                        break;
                }
            }
            else
            {
                ScrollDebugLogger.Log($"DeveloperMenuHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }
    }
}

