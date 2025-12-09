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
        
        public event OnShowSettings? ShowSettingsEvent;
        public event OnShowVariableEditor? ShowVariableEditorEvent;
        public event OnShowActionEditor? ShowActionEditorEvent;

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
            ScrollDebugLogger.Log("DeveloperMenuHandler: ShowDeveloperMenu called");
            DebugLogger.Log("DeveloperMenuHandler", "ShowDeveloperMenu called");
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                ScrollDebugLogger.Log("DeveloperMenuHandler: UI manager is CanvasUICoordinator, rendering developer menu");
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderDeveloperMenu();
                ScrollDebugLogger.Log("DeveloperMenuHandler: RenderDeveloperMenu completed");
            }
            else
            {
                ScrollDebugLogger.Log($"DeveloperMenuHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.DeveloperMenu);
            ScrollDebugLogger.Log("DeveloperMenuHandler: State transitioned to DeveloperMenu");
        }

        /// <summary>
        /// Handle developer menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            DebugLogger.Log("DeveloperMenuHandler", $"HandleMenuInput called with input: '{input}'");
            ScrollDebugLogger.Log($"DeveloperMenuHandler.HandleMenuInput: input='{input}', state={stateManager.CurrentState}");
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
                        // Edit Game Variables
                        DebugLogger.Log("DeveloperMenuHandler", "Showing variable editor");
                        ScrollDebugLogger.Log("DeveloperMenuHandler: Showing variable editor");
                        ShowVariableEditorEvent?.Invoke();
                        break;
                    case "2":
                        // Edit Actions
                        DebugLogger.Log("DeveloperMenuHandler", "Showing action editor");
                        ScrollDebugLogger.Log("DeveloperMenuHandler: Showing action editor");
                        ShowActionEditorEvent?.Invoke();
                        break;
                    case "0":
                        // Back to Settings
                        DebugLogger.Log("DeveloperMenuHandler", "Returning to Settings");
                        ScrollDebugLogger.Log("DeveloperMenuHandler: Returning to Settings");
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.Settings);
                        ShowSettingsEvent?.Invoke();
                        break;
                    default:
                        // Any other input refreshes the menu
                        DebugLogger.Log("DeveloperMenuHandler", $"Unknown input '{input}', refreshing developer menu");
                        ScrollDebugLogger.Log($"DeveloperMenuHandler: Unknown input '{input}', refreshing developer menu");
                        canvasUI.ResetDeleteConfirmation();
                        ShowDeveloperMenu();
                        break;
                }
            }
            else
            {
                DebugLogger.Log("DeveloperMenuHandler", "ERROR: customUIManager is not CanvasUICoordinator");
                ScrollDebugLogger.Log($"DeveloperMenuHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }
    }
}

