using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.Utils;
using System;
using System.ComponentModel;

namespace RPGGame.UI.Avalonia
{
    public partial class SettingsWindow : Window
    {
        private GameCoordinator? gameCoordinator;
        private CanvasUICoordinator? canvasUI;
        private GameStateManager? gameStateManager;
        private System.Action<string>? updateStatus;

        public SettingsWindow()
        {
            InitializeComponent();
            
            // Handle window closing to restore game state
            this.Closing += OnWindowClosing;
            
            // Handle Escape key to close window
            this.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// Initializes the settings window with game references
        /// </summary>
        public void Initialize(
            GameCoordinator? game,
            CanvasUICoordinator? ui,
            GameStateManager? stateManager,
            System.Action<string>? statusCallback)
        {
            gameCoordinator = game;
            canvasUI = ui;
            gameStateManager = stateManager;
            updateStatus = statusCallback;

            // Initialize the settings panel
            if (SettingsMenuPanel != null)
            {
                // Set up callbacks for back button and status updates
                SettingsMenuPanel.SetBackCallback(() =>
                {
                    Close();
                });
                
                SettingsMenuPanel.SetStatusCallback((message) =>
                {
                    updateStatus?.Invoke(message);
                });
                
                // Initialize handlers for developer tools
                SettingsMenuPanel.InitializeHandlers(
                    gameCoordinator?.DeveloperMenuHandler,
                    gameCoordinator,
                    canvasUI,
                    gameStateManager);
            }
        }

        /// <summary>
        /// Handles window closing to restore game state
        /// </summary>
        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            // Restore canvas rendering when closing settings window
            if (canvasUI != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    canvasUI.RestoreDisplayBufferRendering();
                });
            }

            // Transition back to previous state (or main menu if no previous state)
            if (gameStateManager != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // If we're in Settings state, transition back to MainMenu
                    if (gameStateManager.CurrentState == GameState.Settings)
                    {
                        gameStateManager.TransitionToState(GameState.MainMenu);
                    }
                });
            }
        }

        /// <summary>
        /// Handles keyboard input (Escape key to close)
        /// </summary>
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}

