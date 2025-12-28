using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Simulation;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Battle Statistics tab in SettingsPanel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class BattleStatisticsTabManager
    {
        private BattleStatisticsRunner.StatisticsResult? currentBattleStatisticsResults;
        private List<BattleStatisticsRunner.WeaponTestResult>? currentWeaponTestResults;
        private BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? currentComprehensiveResults;
        private bool isBattleStatisticsRunning = false;
        
        private Border? progressBorder;
        private ProgressBar? progressBar;
        private TextBlock? progressStatusText;
        private TextBlock? progressPercentageText;
        private TextBlock? battleStatisticsResultsText;
        private Action<string, bool>? showStatusMessage;

        public void Initialize(
            Border progressBorder,
            ProgressBar progressBar,
            TextBlock progressStatusText,
            TextBlock progressPercentageText,
            TextBlock battleStatisticsResultsText,
            Action<string, bool> showStatusMessage)
        {
            this.progressBorder = progressBorder;
            this.progressBar = progressBar;
            this.progressStatusText = progressStatusText;
            this.progressPercentageText = progressPercentageText;
            this.battleStatisticsResultsText = battleStatisticsResultsText;
            this.showStatusMessage = showStatusMessage;
        }

        public async Task RunBattleTest(int numberOfBattles)
        {
            if (isBattleStatisticsRunning)
            {
                showStatusMessage?.Invoke("A test is already running. Please wait for it to complete.", false);
                return;
            }

            isBattleStatisticsRunning = true;
            currentBattleStatisticsResults = null;
            currentWeaponTestResults = null;
            currentComprehensiveResults = null;

            Dispatcher.UIThread.Post(() =>
            {
                if (progressBorder != null) progressBorder.IsVisible = true;
                if (progressBar != null) progressBar.Value = 0;
                if (progressStatusText != null) progressStatusText.Text = "Initializing test...";
                if (progressPercentageText != null) progressPercentageText.Text = "0%";
                if (battleStatisticsResultsText != null) battleStatisticsResultsText.Text = "Running test...";
            });

            try
            {
                // Clear display buffer and center panel for test output
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ClearDisplayBuffer();
                    var centerX = LayoutConstants.CENTER_PANEL_X + 1;
                    var centerY = LayoutConstants.CENTER_PANEL_Y + 1;
                    var centerWidth = LayoutConstants.CENTER_PANEL_WIDTH - 2;
                    var centerHeight = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
                    canvasUI.ClearTextInArea(centerX, centerY, centerWidth, centerHeight);
                    // Note: ClearProgressBarsInArea not available on CanvasUICoordinator - progress bars will be cleared by canvas rendering
                    canvasUI.RestoreDisplayBufferRendering();
                }
                
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

                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        double percentage = p.total > 0 ? (double)p.completed / p.total * 100 : 0;
                        if (progressBar != null) progressBar.Value = percentage;
                        if (progressStatusText != null) progressStatusText.Text = $"{p.status} ({p.completed}/{p.total})";
                        if (progressPercentageText != null) progressPercentageText.Text = $"{percentage:F1}%";
                    });
                });

                currentBattleStatisticsResults = await BattleStatisticsRunner.RunParallelBattles(
                    config,
                    numberOfBattles,
                    progress
                );

                DisplayBattleStatisticsResults(currentBattleStatisticsResults);
            }
            catch (Exception ex)
            {
                // Write error to center panel
                UIManager.WriteLine($"Error running battle test: {ex.Message}");
                UIManager.WriteLine(ex.StackTrace ?? "");
                Dispatcher.UIThread.Post(() =>
                {
                    showStatusMessage?.Invoke($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                isBattleStatisticsRunning = false;
                Dispatcher.UIThread.Post(() =>
                {
                    if (progressBorder != null) progressBorder.IsVisible = false;
                });
            }
        }

        public async Task RunWeaponTypeTests()
        {
            if (isBattleStatisticsRunning)
            {
                showStatusMessage?.Invoke("A test is already running. Please wait for it to complete.", false);
                return;
            }

            isBattleStatisticsRunning = true;
            currentBattleStatisticsResults = null;
            currentWeaponTestResults = null;
            currentComprehensiveResults = null;

            Dispatcher.UIThread.Post(() =>
            {
                if (progressBorder != null) progressBorder.IsVisible = true;
                if (progressBar != null) progressBar.Value = 0;
                if (progressStatusText != null) progressStatusText.Text = "Initializing weapon type tests...";
                if (progressPercentageText != null) progressPercentageText.Text = "0%";
                if (battleStatisticsResultsText != null) battleStatisticsResultsText.Text = "Running weapon type tests...";
            });

            try
            {
                // Clear display buffer and center panel for test output
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ClearDisplayBuffer();
                    var centerX = LayoutConstants.CENTER_PANEL_X + 1;
                    var centerY = LayoutConstants.CENTER_PANEL_Y + 1;
                    var centerWidth = LayoutConstants.CENTER_PANEL_WIDTH - 2;
                    var centerHeight = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
                    canvasUI.ClearTextInArea(centerX, centerY, centerWidth, centerHeight);
                    // Note: ClearProgressBarsInArea not available on CanvasUICoordinator - progress bars will be cleared by canvas rendering
                    canvasUI.RestoreDisplayBufferRendering();
                }
                
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        double percentage = p.total > 0 ? (double)p.completed / p.total * 100 : 0;
                        if (progressBar != null) progressBar.Value = percentage;
                        if (progressStatusText != null) progressStatusText.Text = $"{p.status} ({p.completed}/{p.total})";
                        if (progressPercentageText != null) progressPercentageText.Text = $"{percentage:F1}%";
                    });
                });

                currentWeaponTestResults = await BattleStatisticsRunner.RunWeaponTypeTests(
                    battlesPerWeapon: 50,
                    playerLevel: 1,
                    enemyLevel: 1,
                    progress
                );

                DisplayWeaponTestResults(currentWeaponTestResults);
            }
            catch (Exception ex)
            {
                // Write error to center panel
                UIManager.WriteLine($"Error running weapon type tests: {ex.Message}");
                UIManager.WriteLine(ex.StackTrace ?? "");
                Dispatcher.UIThread.Post(() =>
                {
                    showStatusMessage?.Invoke($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                isBattleStatisticsRunning = false;
                Dispatcher.UIThread.Post(() =>
                {
                    if (progressBorder != null) progressBorder.IsVisible = false;
                });
            }
        }

        public async Task RunComprehensiveWeaponEnemyTests()
        {
            if (isBattleStatisticsRunning)
            {
                showStatusMessage?.Invoke("A test is already running. Please wait for it to complete.", false);
                return;
            }

            isBattleStatisticsRunning = true;
            currentBattleStatisticsResults = null;
            currentWeaponTestResults = null;
            currentComprehensiveResults = null;

            Dispatcher.UIThread.Post(() =>
            {
                if (progressBorder != null) progressBorder.IsVisible = true;
                if (progressBar != null) progressBar.Value = 0;
                if (progressStatusText != null) progressStatusText.Text = "Initializing comprehensive tests...";
                if (progressPercentageText != null) progressPercentageText.Text = "0%";
                if (battleStatisticsResultsText != null) battleStatisticsResultsText.Text = "Running comprehensive weapon vs enemy tests...";
            });

            try
            {
                // Clear display buffer and center panel for test output
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ClearDisplayBuffer();
                    var centerX = LayoutConstants.CENTER_PANEL_X + 1;
                    var centerY = LayoutConstants.CENTER_PANEL_Y + 1;
                    var centerWidth = LayoutConstants.CENTER_PANEL_WIDTH - 2;
                    var centerHeight = LayoutConstants.CENTER_PANEL_HEIGHT - 2;
                    canvasUI.ClearTextInArea(centerX, centerY, centerWidth, centerHeight);
                    // Note: ClearProgressBarsInArea not available on CanvasUICoordinator - progress bars will be cleared by canvas rendering
                    canvasUI.RestoreDisplayBufferRendering();
                }
                
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        double percentage = p.total > 0 ? (double)p.completed / p.total * 100 : 0;
                        if (progressBar != null) progressBar.Value = percentage;
                        if (progressStatusText != null) progressStatusText.Text = $"{p.status} ({p.completed}/{p.total})";
                        if (progressPercentageText != null) progressPercentageText.Text = $"{percentage:F1}%";
                    });
                });

                currentComprehensiveResults = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination: 10,
                    playerLevel: 1,
                    enemyLevel: 1,
                    progress
                );

                DisplayComprehensiveResults(currentComprehensiveResults);
            }
            catch (Exception ex)
            {
                // Write error to center panel
                UIManager.WriteLine($"Error running comprehensive tests: {ex.Message}");
                UIManager.WriteLine(ex.StackTrace ?? "");
                Dispatcher.UIThread.Post(() =>
                {
                    showStatusMessage?.Invoke($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                isBattleStatisticsRunning = false;
                Dispatcher.UIThread.Post(() =>
                {
                    if (progressBorder != null) progressBorder.IsVisible = false;
                });
            }
        }

        private void DisplayBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results)
        {
            if (results == null) return;

            // Write results to center panel (display buffer) instead of TextBlock
            UIManager.WriteLine("=== BATTLE STATISTICS RESULTS ===");
            UIManager.WriteLine("");
            UIManager.WriteLine("Configuration:");
            UIManager.WriteLine($"  Player: {results.Config.PlayerDamage} dmg, {results.Config.PlayerAttackSpeed:F2} speed, {results.Config.PlayerArmor} armor, {results.Config.PlayerHealth} HP");
            UIManager.WriteLine($"  Enemy:  {results.Config.EnemyDamage} dmg, {results.Config.EnemyAttackSpeed:F2} speed, {results.Config.EnemyArmor} armor, {results.Config.EnemyHealth} HP");
            UIManager.WriteLine("");
            UIManager.WriteLine("Results:");
            UIManager.WriteLine($"  Total Battles: {results.TotalBattles}");
            UIManager.WriteLine($"  Player Wins: {results.PlayerWins} ({results.WinRate:F1}%)");
            UIManager.WriteLine($"  Enemy Wins: {results.EnemyWins} ({100.0 - results.WinRate:F1}%)");
            UIManager.WriteLine("");
            UIManager.WriteLine("Turn Statistics:");
            UIManager.WriteLine($"  Average Turns: {results.AverageTurns:F2}");
            UIManager.WriteLine($"  Min Turns: {results.MinTurns}");
            UIManager.WriteLine($"  Max Turns: {results.MaxTurns}");
            UIManager.WriteLine("");
            UIManager.WriteLine("Damage Statistics:");
            UIManager.WriteLine($"  Average Player Damage Dealt: {results.AveragePlayerDamageDealt:F2}");
            UIManager.WriteLine($"  Average Enemy Damage Dealt: {results.AverageEnemyDamageDealt:F2}");
        }

        private void DisplayWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            if (results == null || results.Count == 0) return;

            // Write results to center panel (display buffer) instead of TextBlock
            UIManager.WriteLine("=== WEAPON TYPE TEST RESULTS ===");
            UIManager.WriteLine("");
            
            foreach (var result in results.OrderByDescending(r => r.WinRate))
            {
                UIManager.WriteLine($"{result.WeaponType}:");
                UIManager.WriteLine($"  Wins: {result.PlayerWins}/{result.TotalBattles} ({result.WinRate:F1}%)");
                UIManager.WriteLine($"  Average Turns: {result.AverageTurns:F2}");
                UIManager.WriteLine($"  Average Player Damage: {result.AveragePlayerDamageDealt:F2}");
                UIManager.WriteLine($"  Average Enemy Damage: {result.AverageEnemyDamageDealt:F2}");
                UIManager.WriteLine("");
            }
        }

        private void DisplayComprehensiveResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            if (results == null) return;

            // Write results to center panel (display buffer) instead of TextBlock
            UIManager.WriteLine("=== COMPREHENSIVE WEAPON VS ENEMY TEST RESULTS ===");
            UIManager.WriteLine("");
            UIManager.WriteLine("Overall Statistics:");
            UIManager.WriteLine($"  Total Battles: {results.TotalBattles}");
            UIManager.WriteLine($"  Player Wins: {results.TotalPlayerWins} ({results.OverallWinRate:F1}%)");
            UIManager.WriteLine($"  Enemy Wins: {results.TotalEnemyWins}");
            UIManager.WriteLine($"  Average Turns: {results.OverallAverageTurns:F1}");
            UIManager.WriteLine($"  Average Player Damage: {results.OverallAveragePlayerDamage:F1}");
            UIManager.WriteLine($"  Average Enemy Damage: {results.OverallAverageEnemyDamage:F1}");
            UIManager.WriteLine("");
            
            if (results.WeaponStatistics != null && results.WeaponStatistics.Count > 0)
            {
                UIManager.WriteLine("Weapon Performance (across all enemies):");
                foreach (var weaponType in results.WeaponTypes)
                {
                    if (results.WeaponStatistics.TryGetValue(weaponType, out var weaponStats))
                    {
                        UIManager.WriteLine($"  {weaponType,-8}: {weaponStats.Wins,3}/{weaponStats.TotalBattles,3} wins ({weaponStats.WinRate,5:F1}%) | Avg Turns: {weaponStats.AverageTurns,5:F1} | Avg Damage: {weaponStats.AverageDamage,5:F1}");
                    }
                }
                UIManager.WriteLine("");
            }
            
            if (results.EnemyStatistics != null && results.EnemyStatistics.Count > 0)
            {
                UIManager.WriteLine("Enemy Difficulty (across all weapons):");
                foreach (var enemyType in results.EnemyTypes.OrderByDescending(e => 
                    results.EnemyStatistics.TryGetValue(e, out var enemyStats) ? enemyStats.WinRate : 0))
                {
                    if (results.EnemyStatistics.TryGetValue(enemyType, out var enemyStats))
                    {
                        UIManager.WriteLine($"  {enemyType,-15}: {enemyStats.Wins,3}/{enemyStats.TotalBattles,3} wins ({enemyStats.WinRate,5:F1}%) | Avg Turns: {enemyStats.AverageTurns,5:F1}");
                    }
                }
            }
        }
    }
}

