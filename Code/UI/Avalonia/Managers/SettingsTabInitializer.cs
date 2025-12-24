using Avalonia.Controls;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles initialization of settings panel tabs
    /// Extracted from SettingsPanel to separate tab initialization logic
    /// </summary>
    public class SettingsTabInitializer
    {
        private readonly GameVariablesTabManager? gameVariablesTabManager;
        private readonly ActionsTabManager? actionsTabManager;
        private readonly BattleStatisticsTabManager? battleStatisticsTabManager;
        private readonly Action<string, bool> showStatusMessage;

        public SettingsTabInitializer(
            GameVariablesTabManager? gameVariablesTabManager,
            ActionsTabManager? actionsTabManager,
            BattleStatisticsTabManager? battleStatisticsTabManager,
            Action<string, bool> showStatusMessage)
        {
            this.gameVariablesTabManager = gameVariablesTabManager;
            this.actionsTabManager = actionsTabManager;
            this.battleStatisticsTabManager = battleStatisticsTabManager;
            this.showStatusMessage = showStatusMessage ?? throw new ArgumentNullException(nameof(showStatusMessage));
        }

        /// <summary>
        /// Initialize the Game Variables tab
        /// </summary>
        public void InitializeGameVariablesTab(ListBox? gameVariablesCategoryListBox, Panel? gameVariablesPanel)
        {
            if (gameVariablesTabManager != null && gameVariablesCategoryListBox != null && gameVariablesPanel != null)
            {
                gameVariablesTabManager.Initialize(
                    gameVariablesCategoryListBox,
                    gameVariablesPanel,
                    showStatusMessage);
            }
        }

        /// <summary>
        /// Initialize the Actions tab
        /// </summary>
        public void InitializeActionsTab(
            ListBox? actionsListBox,
            Panel? actionFormPanel,
            Button? createActionButton,
            Button? deleteActionButton)
        {
            if (actionsTabManager != null && actionsListBox != null && actionFormPanel != null &&
                createActionButton != null && deleteActionButton != null)
            {
                actionsTabManager.Initialize(
                    actionsListBox,
                    actionFormPanel,
                    createActionButton,
                    deleteActionButton,
                    showStatusMessage);
            }
        }

        /// <summary>
        /// Initialize the Battle Statistics tab
        /// </summary>
        public void InitializeBattleStatisticsTab(
            Border? progressBorder,
            ProgressBar? progressBar,
            TextBlock? progressStatusText,
            TextBlock? progressPercentageText,
            TextBlock? battleStatisticsResultsText)
        {
            if (battleStatisticsTabManager != null && progressBorder != null && progressBar != null && 
                progressStatusText != null && progressPercentageText != null && battleStatisticsResultsText != null)
            {
                battleStatisticsTabManager.Initialize(
                    progressBorder,
                    progressBar,
                    progressStatusText,
                    progressPercentageText,
                    battleStatisticsResultsText,
                    showStatusMessage);
            }
        }
    }
}

