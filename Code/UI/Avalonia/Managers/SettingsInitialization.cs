using Avalonia.Controls;
using RPGGame;
using RPGGame.UI.Avalonia;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles initialization of settings panel managers and tabs.
    /// Extracted from SettingsPanel to improve Single Responsibility Principle compliance.
    /// </summary>
    public class SettingsInitialization
    {
        private readonly SettingsTabInitializer tabInitializer;
        private readonly GameVariablesTabManager gameVariablesTabManager;
        private readonly ActionsTabManager actionsTabManager;
        private readonly SettingsManager settingsManager;

        public SettingsInitialization(
            SettingsManager settingsManager,
            GameVariablesTabManager gameVariablesTabManager,
            ActionsTabManager actionsTabManager,
            Action<string, bool> showStatusMessage)
        {
            this.settingsManager = settingsManager;
            this.gameVariablesTabManager = gameVariablesTabManager;
            this.actionsTabManager = actionsTabManager;
            
            this.tabInitializer = new SettingsTabInitializer(
                gameVariablesTabManager,
                actionsTabManager,
                showStatusMessage);
        }

        /// <summary>
        /// Initializes animation configuration updates for real-time changes
        /// </summary>
        public void InitializeAnimationConfiguration()
        {
            var animationManager = settingsManager.GetAnimationSettingsManager();
            if (animationManager != null)
            {
                animationManager.OnConfigurationUpdated += () =>
                {
                    // Reload animation configuration in CanvasAnimationManager
                    var uiManager = RPGGame.UIManager.GetCustomUIManager();
                    if (uiManager is CanvasUICoordinator coordinator)
                    {
                        var animManager = coordinator.GetAnimationManager();
                        if (animManager is Managers.CanvasAnimationManager canvasAnimManager)
                        {
                            canvasAnimManager.ReloadAnimationConfiguration();
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Initializes handlers for developer tools
        /// </summary>
        public void InitializeHandlers(
            ListBox gameVariablesCategoryListBox,
            Panel gameVariablesPanel,
            ListBox actionsListBox,
            Panel actionFormPanel,
            Button createActionButton,
            Button deleteActionButton,
            UserControl settingsPanel)
        {
            // Initialize tab managers
            if (tabInitializer != null)
            {
                tabInitializer.InitializeGameVariablesTab(gameVariablesCategoryListBox, gameVariablesPanel);
                tabInitializer.InitializeActionsTab(actionsListBox, actionFormPanel, createActionButton, deleteActionButton);
                
            }
        }

        /// <summary>
        /// Initialize GameVariables tab from panel controls
        /// </summary>
        public void InitializeGameVariablesTab(UserControl panel)
        {
            if (panel == null || tabInitializer == null) return;
            
            var categoryListBox = panel.FindControl<ListBox>("GameVariablesCategoryListBox");
            var variablesPanel = panel.FindControl<Panel>("GameVariablesPanel");
            
            tabInitializer.InitializeGameVariablesTab(categoryListBox, variablesPanel);
        }

        /// <summary>
        /// Initialize Actions tab from panel controls
        /// </summary>
        public void InitializeActionsTab(UserControl panel)
        {
            if (panel == null || tabInitializer == null) return;
            
            var actionsListBox = panel.FindControl<ListBox>("ActionsListBox");
            var actionFormPanel = panel.FindControl<Panel>("ActionFormPanel");
            var createActionButton = panel.FindControl<Button>("CreateActionButton");
            var deleteActionButton = panel.FindControl<Button>("DeleteActionButton");
            
            tabInitializer.InitializeActionsTab(actionsListBox, actionFormPanel, createActionButton, deleteActionButton);
        }
    }
}

