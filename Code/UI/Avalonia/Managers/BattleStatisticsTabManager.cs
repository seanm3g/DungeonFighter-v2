using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Simulation;
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
                Dispatcher.UIThread.Post(() =>
                {
                    if (battleStatisticsResultsText != null) 
                        battleStatisticsResultsText.Text = $"Error running battle test: {ex.Message}\n\n{ex.StackTrace}";
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
                Dispatcher.UIThread.Post(() =>
                {
                    if (battleStatisticsResultsText != null) 
                        battleStatisticsResultsText.Text = $"Error running weapon type tests: {ex.Message}\n\n{ex.StackTrace}";
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
                Dispatcher.UIThread.Post(() =>
                {
                    if (battleStatisticsResultsText != null) 
                        battleStatisticsResultsText.Text = $"Error running comprehensive tests: {ex.Message}\n\n{ex.StackTrace}";
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

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== BATTLE STATISTICS RESULTS ===");
            output.AppendLine();
            
            output.AppendLine("Configuration:");
            output.AppendLine($"  Player: {results.Config.PlayerDamage} dmg, {results.Config.PlayerAttackSpeed:F2} speed, {results.Config.PlayerArmor} armor, {results.Config.PlayerHealth} HP");
            output.AppendLine($"  Enemy:  {results.Config.EnemyDamage} dmg, {results.Config.EnemyAttackSpeed:F2} speed, {results.Config.EnemyArmor} armor, {results.Config.EnemyHealth} HP");
            output.AppendLine();
            
            output.AppendLine("Results:");
            output.AppendLine($"  Total Battles: {results.TotalBattles}");
            output.AppendLine($"  Player Wins: {results.PlayerWins} ({results.WinRate:F1}%)");
            output.AppendLine($"  Enemy Wins: {results.EnemyWins} ({100.0 - results.WinRate:F1}%)");
            output.AppendLine();
            
            output.AppendLine("Turn Statistics:");
            output.AppendLine($"  Average Turns: {results.AverageTurns:F2}");
            output.AppendLine($"  Min Turns: {results.MinTurns}");
            output.AppendLine($"  Max Turns: {results.MaxTurns}");
            output.AppendLine();
            
            output.AppendLine("Damage Statistics:");
            output.AppendLine($"  Average Player Damage Dealt: {results.AveragePlayerDamageDealt:F2}");
            output.AppendLine($"  Average Enemy Damage Dealt: {results.AverageEnemyDamageDealt:F2}");

            Dispatcher.UIThread.Post(() =>
            {
                if (battleStatisticsResultsText != null) battleStatisticsResultsText.Text = output.ToString();
            });
        }

        private void DisplayWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            if (results == null || results.Count == 0) return;

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== WEAPON TYPE TEST RESULTS ===");
            output.AppendLine();
            
            foreach (var result in results.OrderByDescending(r => r.WinRate))
            {
                output.AppendLine($"{result.WeaponType}:");
                output.AppendLine($"  Wins: {result.PlayerWins}/{result.TotalBattles} ({result.WinRate:F1}%)");
                output.AppendLine($"  Average Turns: {result.AverageTurns:F2}");
                output.AppendLine($"  Average Player Damage: {result.AveragePlayerDamageDealt:F2}");
                output.AppendLine($"  Average Enemy Damage: {result.AverageEnemyDamageDealt:F2}");
                output.AppendLine();
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (battleStatisticsResultsText != null) battleStatisticsResultsText.Text = output.ToString();
            });
        }

        private void DisplayComprehensiveResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            if (results == null) return;

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== COMPREHENSIVE WEAPON VS ENEMY TEST RESULTS ===");
            output.AppendLine();
            
            output.AppendLine("Overall Statistics:");
            output.AppendLine($"  Total Battles: {results.TotalBattles}");
            output.AppendLine($"  Player Wins: {results.TotalPlayerWins} ({results.OverallWinRate:F1}%)");
            output.AppendLine($"  Enemy Wins: {results.TotalEnemyWins}");
            output.AppendLine($"  Average Turns: {results.OverallAverageTurns:F1}");
            output.AppendLine($"  Average Player Damage: {results.OverallAveragePlayerDamage:F1}");
            output.AppendLine($"  Average Enemy Damage: {results.OverallAverageEnemyDamage:F1}");
            output.AppendLine();
            
            if (results.WeaponStatistics != null && results.WeaponStatistics.Count > 0)
            {
                output.AppendLine("Weapon Performance (across all enemies):");
                foreach (var weaponType in results.WeaponTypes)
                {
                    if (results.WeaponStatistics.TryGetValue(weaponType, out var weaponStats))
                    {
                        output.AppendLine($"  {weaponType,-8}: {weaponStats.Wins,3}/{weaponStats.TotalBattles,3} wins ({weaponStats.WinRate,5:F1}%) | Avg Turns: {weaponStats.AverageTurns,5:F1} | Avg Damage: {weaponStats.AverageDamage,5:F1}");
                    }
                }
                output.AppendLine();
            }
            
            if (results.EnemyStatistics != null && results.EnemyStatistics.Count > 0)
            {
                output.AppendLine("Enemy Difficulty (across all weapons):");
                foreach (var enemyType in results.EnemyTypes.OrderByDescending(e => 
                    results.EnemyStatistics.TryGetValue(e, out var enemyStats) ? enemyStats.WinRate : 0))
                {
                    if (results.EnemyStatistics.TryGetValue(enemyType, out var enemyStats))
                    {
                        output.AppendLine($"  {enemyType,-15}: {enemyStats.Wins,3}/{enemyStats.TotalBattles,3} wins ({enemyStats.WinRate,5:F1}%) | Avg Turns: {enemyStats.AverageTurns,5:F1}");
                    }
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (battleStatisticsResultsText != null) battleStatisticsResultsText.Text = output.ToString();
            });
        }
    }
}

