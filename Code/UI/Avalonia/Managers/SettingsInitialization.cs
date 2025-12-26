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
        private readonly BattleStatisticsTabManager battleStatisticsTabManager;
        private readonly SettingsManager settingsManager;

        public SettingsInitialization(
            SettingsManager settingsManager,
            GameVariablesTabManager gameVariablesTabManager,
            ActionsTabManager actionsTabManager,
            BattleStatisticsTabManager battleStatisticsTabManager,
            Action<string, bool> showStatusMessage)
        {
            this.settingsManager = settingsManager;
            this.gameVariablesTabManager = gameVariablesTabManager;
            this.actionsTabManager = actionsTabManager;
            this.battleStatisticsTabManager = battleStatisticsTabManager;
            
            this.tabInitializer = new SettingsTabInitializer(
                gameVariablesTabManager,
                actionsTabManager,
                battleStatisticsTabManager,
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
        /// Initializes handlers for testing and developer tools
        /// </summary>
        public void InitializeHandlers(
            TestExecutionManager testExecutionManager,
            SettingsTestExecutor testExecutor,
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
                
                var progressBorder = settingsPanel.FindControl<Border>("ProgressBorder");
                var progressBar = settingsPanel.FindControl<ProgressBar>("ProgressBar");
                var progressStatusText = settingsPanel.FindControl<TextBlock>("ProgressStatusText");
                var progressPercentageText = settingsPanel.FindControl<TextBlock>("ProgressPercentageText");
                var battleStatisticsResultsText = settingsPanel.FindControl<TextBlock>("BattleStatisticsResultsText");
                
                tabInitializer.InitializeBattleStatisticsTab(
                    progressBorder,
                    progressBar,
                    progressStatusText,
                    progressPercentageText,
                    battleStatisticsResultsText);
            }
        }
    }
}

