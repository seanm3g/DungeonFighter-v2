using System;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles test execution for settings panel
    /// Extracted from SettingsPanel to separate test execution logic
    /// </summary>
    public class SettingsTestExecutor
    {
        private readonly TestExecutionManager? testExecutionManager;
        private readonly BattleStatisticsTabManager? battleStatisticsTabManager;
        private readonly GameCoordinator? gameCoordinator;
        private readonly CanvasUICoordinator? canvasUI;
        private readonly Action<string, bool> showStatusMessage;

        public SettingsTestExecutor(
            TestExecutionManager? testExecutionManager,
            BattleStatisticsTabManager? battleStatisticsTabManager,
            GameCoordinator? gameCoordinator,
            CanvasUICoordinator? canvasUI,
            Action<string, bool> showStatusMessage)
        {
            this.testExecutionManager = testExecutionManager;
            this.battleStatisticsTabManager = battleStatisticsTabManager;
            this.gameCoordinator = gameCoordinator;
            this.canvasUI = canvasUI;
            this.showStatusMessage = showStatusMessage ?? throw new ArgumentNullException(nameof(showStatusMessage));
        }

        /// <summary>
        /// Executes a test by command key
        /// </summary>
        public Task ExecuteTest(string commandKey)
        {
            if (testExecutionManager == null)
            {
                showStatusMessage("Test execution manager not initialized", false);
                return Task.CompletedTask;
            }
            
            return testExecutionManager.ExecuteTest(commandKey);
        }

        /// <summary>
        /// Gets the canvas UI, trying stored reference first, then UIManager fallback
        /// </summary>
        public CanvasUICoordinator? GetCanvasUI()
        {
            if (canvasUI != null)
                return canvasUI;
            
            var uiManager = RPGGame.UIManager.GetCustomUIManager();
            return uiManager as CanvasUICoordinator;
        }

        /// <summary>
        /// Opens battle statistics while keeping settings panel open
        /// </summary>
        public void ShowBattleStatistics()
        {
            var uiToUse = GetCanvasUI();
            if (gameCoordinator == null || uiToUse == null)
            {
                showStatusMessage("Developer tools not available - game may not be fully initialized", false);
                return;
            }
            
            uiToUse.RestoreDisplayBufferRendering();
            gameCoordinator.ShowBattleStatistics();
        }

        /// <summary>
        /// Runs a battle test with the specified number of battles
        /// </summary>
        public async Task RunBattleTest(int numberOfBattles)
        {
            if (battleStatisticsTabManager == null)
            {
                showStatusMessage("Battle statistics manager not initialized", false);
                return;
            }
            
            await battleStatisticsTabManager.RunBattleTest(numberOfBattles);
        }

        /// <summary>
        /// Runs weapon type tests
        /// </summary>
        public async Task RunWeaponTypeTests()
        {
            if (battleStatisticsTabManager == null)
            {
                showStatusMessage("Battle statistics manager not initialized", false);
                return;
            }
            
            await battleStatisticsTabManager.RunWeaponTypeTests();
        }

        /// <summary>
        /// Runs comprehensive weapon-enemy tests
        /// </summary>
        public async Task RunComprehensiveWeaponEnemyTests()
        {
            if (battleStatisticsTabManager == null)
            {
                showStatusMessage("Battle statistics manager not initialized", false);
                return;
            }
            
            await battleStatisticsTabManager.RunComprehensiveWeaponEnemyTests();
        }
    }
}

