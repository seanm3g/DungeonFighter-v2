using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Handles battle statistics menu display and input for parallel battle testing
    /// </summary>
    public class BattleStatisticsHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private BattleStatisticsRunner.StatisticsResult? currentResults;
        private List<BattleStatisticsRunner.WeaponTestResult>? currentWeaponResults;
        private BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? currentComprehensiveResults;
        private bool isRunning = false;
        
        // Delegates
        public delegate void OnShowDeveloperMenu();
        
        public event OnShowDeveloperMenu? ShowDeveloperMenuEvent;

        public BattleStatisticsHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Display the battle statistics menu
        /// </summary>
        public void ShowBattleStatisticsMenu()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderBattleStatisticsMenu(currentResults, isRunning);
            }
            else
            {
                ScrollDebugLogger.Log($"BattleStatisticsHandler: UI manager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
            stateManager.TransitionToState(GameState.BattleStatistics);
        }

        /// <summary>
        /// Handle battle statistics menu input
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                switch (input)
                {
                    case "1":
                        // Run quick test (100 battles)
                        StartBackgroundTask(RunBattleTest(100), "Quick battle test (100 battles)");
                        break;
                    case "2":
                        // Run standard test (500 battles)
                        StartBackgroundTask(RunBattleTest(500), "Standard battle test (500 battles)");
                        break;
                    case "3":
                        // Run comprehensive test (1000 battles)
                        StartBackgroundTask(RunBattleTest(1000), "Comprehensive battle test (1000 battles)");
                        break;
                    case "4":
                        // Run custom test
                        StartBackgroundTask(RunBattleTest(100), "Custom battle test (100 battles)");
                        break;
                    case "5":
                        // Run weapon type tests (each weapon vs random enemies)
                        StartBackgroundTask(RunWeaponTypeTests(), "Weapon type tests");
                        break;
                    case "6":
                        // Run comprehensive tests (every weapon vs every enemy)
                        StartBackgroundTask(RunComprehensiveWeaponEnemyTests(), "Comprehensive weapon-enemy tests");
                        break;
                    case "7":
                        // View last results
                        if (currentResults != null)
                        {
                            ShowResults();
                        }
                        else if (currentComprehensiveResults != null)
                        {
                            ShowComprehensiveResults();
                        }
                        else
                        {
                            ShowBattleStatisticsMenu(); // Refresh if no results
                        }
                        break;
                    case "0":
                        // Back to Developer Menu
                        canvasUI.ResetDeleteConfirmation();
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        ShowDeveloperMenuEvent?.Invoke();
                        break;
                    default:
                        // Any other input refreshes the menu
                        canvasUI.ResetDeleteConfirmation();
                        ShowBattleStatisticsMenu();
                        break;
                }
            }
            else
            {
                ScrollDebugLogger.Log($"BattleStatisticsHandler: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        /// <summary>
        /// Starts a background task with proper error handling
        /// </summary>
        private void StartBackgroundTask(Task task, string taskName)
        {
            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ScrollDebugLogger.Log($"BattleStatisticsHandler: Background task '{taskName}' failed: {t.Exception?.GetBaseException().Message}");
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.RenderBattleStatisticsMenu(null, false);
                    }
                }
                else if (t.IsCanceled)
                {
                    ScrollDebugLogger.Log($"BattleStatisticsHandler: Background task '{taskName}' was cancelled");
                }
                else
                {
                    ScrollDebugLogger.Log($"BattleStatisticsHandler: Background task '{taskName}' completed successfully");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Runs a battle test with default configuration
        /// </summary>
        private async Task RunBattleTest(int numberOfBattles)
        {
            if (isRunning)
            {
                return; // Prevent multiple simultaneous runs
            }

            isRunning = true;
            currentResults = null;

            // Default test configuration
            var config = new BattleStatisticsRunner.BattleConfiguration
            {
                PlayerDamage = 10,
                PlayerAttackSpeed = 1.0,
                PlayerArmor = 2,
                PlayerHealth = 100,
                EnemyDamage = 8,
                EnemyAttackSpeed = 1.2,
                EnemyArmor = 1,
                EnemyHealth = 80
            };

            try
            {
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.UpdateBattleStatisticsProgress(p.completed, p.total, p.status);
                    }
                });

                currentResults = await BattleStatisticsRunner.RunParallelBattles(
                    config, 
                    numberOfBattles,
                    progress
                );

                // Show results
                ShowResults();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BattleStatisticsHandler: Error running battle test: {ex.Message}");
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.RenderBattleStatisticsMenu(null, false);
                }
            }
            finally
            {
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs weapon type tests (each weapon vs random enemies)
        /// </summary>
        private async Task RunWeaponTypeTests()
        {
            if (isRunning)
            {
                return; // Prevent multiple simultaneous runs
            }

            isRunning = true;
            currentResults = null;
            currentWeaponResults = null;

            try
            {
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.UpdateBattleStatisticsProgress(p.completed, p.total, p.status);
                    }
                });

                currentWeaponResults = await BattleStatisticsRunner.RunWeaponTypeTests(
                    battlesPerWeapon: 50,
                    playerLevel: 1,
                    enemyLevel: 1,
                    progress
                );

                // Show weapon test results
                ShowWeaponResults();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BattleStatisticsHandler: Error running weapon tests: {ex.Message}");
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.RenderBattleStatisticsMenu(null, false);
                }
            }
            finally
            {
                isRunning = false;
            }
        }

        /// <summary>
        /// Shows the custom test configuration menu
        /// </summary>
        private void ShowCustomTestMenu()
        {
            // For now, just run with default config
            // In the future, this could show a form to input custom stats
            StartBackgroundTask(RunBattleTest(100), "Custom test menu battle test");
        }

        /// <summary>
        /// Runs comprehensive weapon-enemy tests (every weapon vs every enemy)
        /// </summary>
        private async Task RunComprehensiveWeaponEnemyTests()
        {
            if (isRunning)
            {
                return; // Prevent multiple simultaneous runs
            }

            isRunning = true;
            currentResults = null;
            currentWeaponResults = null;
            currentComprehensiveResults = null;

            try
            {
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.UpdateBattleStatisticsProgress(p.completed, p.total, p.status);
                    }
                });

                currentComprehensiveResults = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination: 10,
                    playerLevel: 1,
                    enemyLevel: 1,
                    progress
                );

                // Show comprehensive test results
                ShowComprehensiveResults();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BattleStatisticsHandler: Error running comprehensive tests: {ex.Message}");
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.RenderBattleStatisticsMenu(null, false);
                }
            }
            finally
            {
                isRunning = false;
            }
        }

        /// <summary>
        /// Shows the weapon test results
        /// </summary>
        private void ShowWeaponResults()
        {
            if (currentWeaponResults == null || customUIManager is not CanvasUICoordinator canvasUI)
            {
                return;
            }

            canvasUI.RenderWeaponTestResults(currentWeaponResults);
        }

        /// <summary>
        /// Shows the comprehensive test results
        /// </summary>
        private void ShowComprehensiveResults()
        {
            if (currentComprehensiveResults == null || customUIManager is not CanvasUICoordinator canvasUI)
            {
                return;
            }

            canvasUI.RenderComprehensiveWeaponEnemyResults(currentComprehensiveResults);
        }

        /// <summary>
        /// Shows the results of the battle test
        /// </summary>
        private void ShowResults()
        {
            if (currentResults == null || customUIManager is not CanvasUICoordinator canvasUI)
            {
                return;
            }

            canvasUI.RenderBattleStatisticsResults(currentResults);
        }
    }
}

