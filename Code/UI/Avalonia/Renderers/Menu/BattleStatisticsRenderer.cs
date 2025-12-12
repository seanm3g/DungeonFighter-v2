using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the battle statistics menu screen
    /// </summary>
    public class BattleStatisticsRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public BattleStatisticsRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the battle statistics menu content
        /// </summary>
        public int RenderBattleStatisticsMenuContent(int x, int y, int width, int height, BattleStatisticsRunner.StatisticsResult? results, bool isRunning)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 30);
            
            // Title
            string title = "=== BATTLE STATISTICS ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            if (isRunning)
            {
                canvas.AddText(menuStartX, menuStartY, "Running battles... Please wait.", AsciiArtAssets.Colors.Yellow);
                menuStartY += 2;
            }
            else
            {
                // Menu options
                var menuOptions = new[]
                {
                    (1, "Quick Test (100 battles)", AsciiArtAssets.Colors.White),
                    (2, "Standard Test (500 battles)", AsciiArtAssets.Colors.White),
                    (3, "Comprehensive Test (1000 battles)", AsciiArtAssets.Colors.White),
                    (4, "Custom Test Configuration", AsciiArtAssets.Colors.White),
                    (5, "Weapon Type Tests (each weapon vs random enemies)", AsciiArtAssets.Colors.Yellow),
                    (6, "Comprehensive: Every Weapon vs Every Enemy", AsciiArtAssets.Colors.Cyan),
                    (7, "View Last Results", AsciiArtAssets.Colors.White),
                    (0, "Back to Developer Menu", AsciiArtAssets.Colors.White)
                };
                
                foreach (var (number, text, color) in menuOptions)
                {
                    string displayText = MenuOptionFormatter.Format(number, text);
                    var option = new ClickableElement
                    {
                        X = menuStartX,
                        Y = menuStartY,
                        Width = displayText.Length,
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = number.ToString(),
                        DisplayText = displayText
                    };
                    clickableElements.Add(option);
                    canvas.AddText(menuStartX, menuStartY, displayText, color);
                    menuStartY++;
                }
            }
            
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders battle statistics results
        /// </summary>
        public int RenderBattleStatisticsResults(int x, int y, int width, int height, BattleStatisticsRunner.StatisticsResult results)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 80);
            
            // Title
            string title = "=== BATTLE STATISTICS RESULTS ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Configuration
            canvas.AddText(menuStartX, menuStartY, "Configuration:", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Player: {results.Config.PlayerDamage} dmg, {results.Config.PlayerAttackSpeed:F2} speed, {results.Config.PlayerArmor} armor, {results.Config.PlayerHealth} HP", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Enemy:  {results.Config.EnemyDamage} dmg, {results.Config.EnemyAttackSpeed:F2} speed, {results.Config.EnemyArmor} armor, {results.Config.EnemyHealth} HP", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
            // Results
            canvas.AddText(menuStartX, menuStartY, "Results:", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Total Battles: {results.TotalBattles}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Player Wins: {results.PlayerWins} ({results.WinRate:F1}%)", AsciiArtAssets.Colors.Green);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Enemy Wins: {results.EnemyWins} ({100.0 - results.WinRate:F1}%)", AsciiArtAssets.Colors.Red);
            menuStartY += 2;
            
            canvas.AddText(menuStartX, menuStartY, "Turn Statistics:", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Average Turns: {results.AverageTurns:F1}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Min Turns: {results.MinTurns}, Max Turns: {results.MaxTurns}", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
            canvas.AddText(menuStartX, menuStartY, "Damage Statistics:", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Avg Player Damage Dealt: {results.AveragePlayerDamageDealt:F1}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Avg Enemy Damage Dealt: {results.AverageEnemyDamageDealt:F1}", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
            // Back option
            string backText = MenuOptionFormatter.Format(0, "Back to Battle Statistics Menu");
            var backOption = new ClickableElement
            {
                X = menuStartX,
                Y = menuStartY,
                Width = backText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = backText
            };
            clickableElements.Add(backOption);
            canvas.AddText(menuStartX, menuStartY, backText, AsciiArtAssets.Colors.White);
            
            return currentLineCount;
        }

        /// <summary>
        /// Renders weapon test results showing statistics for each weapon type
        /// </summary>
        public int RenderWeaponTestResults(int x, int y, int width, int height, List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, height, 2, 100);
            
            // Title
            string title = "=== WEAPON TYPE TEST RESULTS ===";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Summary
            canvas.AddText(menuStartX, menuStartY, $"Tested {results.Count} weapon types against random enemies", AsciiArtAssets.Colors.Cyan);
            menuStartY += 2;
            
            // Results for each weapon
            foreach (var weaponResult in results)
            {
                canvas.AddText(menuStartX, menuStartY, $"--- {weaponResult.WeaponType} ---", AsciiArtAssets.Colors.Yellow);
                menuStartY++;
                canvas.AddText(menuStartX + 2, menuStartY, $"Battles: {weaponResult.TotalBattles} | Wins: {weaponResult.PlayerWins} ({weaponResult.WinRate:F1}%) | Losses: {weaponResult.EnemyWins}", AsciiArtAssets.Colors.White);
                menuStartY++;
                canvas.AddText(menuStartX + 2, menuStartY, $"Avg Turns: {weaponResult.AverageTurns:F1} | Min: {weaponResult.MinTurns} | Max: {weaponResult.MaxTurns}", AsciiArtAssets.Colors.White);
                menuStartY++;
                canvas.AddText(menuStartX + 2, menuStartY, $"Avg Player Damage: {weaponResult.AveragePlayerDamageDealt:F1} | Avg Enemy Damage: {weaponResult.AverageEnemyDamageDealt:F1}", AsciiArtAssets.Colors.White);
                menuStartY += 2;
            }
            
            // Back option
            string backText = MenuOptionFormatter.Format(0, "Back to Battle Statistics Menu");
            var backOption = new ClickableElement
            {
                X = menuStartX,
                Y = menuStartY,
                Width = backText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = backText
            };
            clickableElements.Add(backOption);
            canvas.AddText(menuStartX, menuStartY, backText, AsciiArtAssets.Colors.White);
            
            return currentLineCount;
        }

        /// <summary>
        /// Renders comprehensive weapon-enemy test results
        /// Shows every weapon tested against every enemy
        /// </summary>
        public int RenderComprehensiveWeaponEnemyResults(int x, int y, int width, int height, BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Calculate total line count for proper vertical centering
            int totalLines = 3; // Title + spacing
            totalLines += 5; // Overall Statistics: header + 2 data lines + 2 spacing
            totalLines += 1 + results.WeaponTypes.Count + 2; // Weapon Performance: header + weapon lines + spacing
            totalLines += 1 + results.EnemyTypes.Count + 2; // Enemy Difficulty: header + enemy lines + spacing
            
            // Best and worst matchups
            var bestMatchups = results.CombinationResults
                .Where(c => c.BattleResults.Count(r => r.ErrorMessage == null) > 0)
                .OrderByDescending(c => c.WinRate)
                .Take(5)
                .ToList();
            
            var worstMatchups = results.CombinationResults
                .Where(c => c.BattleResults.Count(r => r.ErrorMessage == null) > 0)
                .OrderBy(c => c.WinRate)
                .Take(5)
                .ToList();
            
            if (bestMatchups.Count > 0)
                totalLines += 1 + bestMatchups.Count + 2; // Best Matchups: header + lines + spacing
            if (worstMatchups.Count > 0)
                totalLines += 1 + worstMatchups.Count + 2; // Worst Matchups: header + lines + spacing
            
            totalLines += 3; // Note + spacing
            totalLines += 1; // Back option
            
            // Title (needed for width calculation)
            string title = "=== COMPREHENSIVE WEAPON-ENEMY TEST RESULTS ===";
            
            // Calculate content width - find the longest line
            int maxContentWidth = title.Length; // Start with title width
            maxContentWidth = Math.Max(maxContentWidth, "Overall Statistics:".Length);
            maxContentWidth = Math.Max(maxContentWidth, "Weapon Performance (across all enemies):".Length);
            maxContentWidth = Math.Max(maxContentWidth, "Enemy Difficulty (across all weapons):".Length);
            maxContentWidth = Math.Max(maxContentWidth, "Best Matchups (Highest Win Rate):".Length);
            maxContentWidth = Math.Max(maxContentWidth, "Worst Matchups (Lowest Win Rate):".Length);
            maxContentWidth = Math.Max(maxContentWidth, "Note: Full matrix view available in detailed results".Length);
            
            // Estimate longest data line (weapon/enemy stats lines are typically longest)
            // Format: "WeaponType: XXX/XXX wins (XXX.X%) | Avg Turns: XXX.X | Avg Damage: XXX.X"
            int estimatedDataLineWidth = 100; // Reasonable estimate for longest data line
            maxContentWidth = Math.Max(maxContentWidth, estimatedDataLineWidth);
            
            // Calculate centered position using actual line count and content width
            int menuStartX = x + (width / 2) - (maxContentWidth / 2); // Center horizontally
            int menuStartY = y + (height / 2) - (totalLines / 2); // Center vertically
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length);
            canvas.AddText(titleX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Overall summary
            canvas.AddText(menuStartX, menuStartY, "Overall Statistics:", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Total Battles: {results.TotalBattles} | Player Wins: {results.TotalPlayerWins} ({results.OverallWinRate:F1}%) | Enemy Wins: {results.TotalEnemyWins}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX + 2, menuStartY, $"Avg Turns: {results.OverallAverageTurns:F1} | Avg Player Damage: {results.OverallAveragePlayerDamage:F1} | Avg Enemy Damage: {results.OverallAverageEnemyDamage:F1}", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
            // Per-weapon summary
            canvas.AddText(menuStartX, menuStartY, "Weapon Performance (across all enemies):", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            foreach (var weaponType in results.WeaponTypes)
            {
                if (results.WeaponStatistics.TryGetValue(weaponType, out var weaponStats))
                {
                    canvas.AddText(menuStartX + 2, menuStartY, 
                        $"{weaponType,-8}: {weaponStats.Wins,3}/{weaponStats.TotalBattles,3} wins ({weaponStats.WinRate,5:F1}%) | Avg Turns: {weaponStats.AverageTurns,5:F1} | Avg Damage: {weaponStats.AverageDamage,5:F1}", 
                        AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
            }
            menuStartY += 2;
            
            // Per-enemy summary
            canvas.AddText(menuStartX, menuStartY, "Enemy Difficulty (across all weapons):", AsciiArtAssets.Colors.Cyan);
            menuStartY++;
            foreach (var enemyType in results.EnemyTypes.OrderByDescending(e => 
                results.EnemyStatistics.TryGetValue(e, out var stats) ? stats.WinRate : 0))
            {
                if (results.EnemyStatistics.TryGetValue(enemyType, out var enemyStats))
                {
                    // Show enemy difficulty (lower win rate = harder enemy)
                    string difficulty = enemyStats.WinRate < 30 ? "Hard" : enemyStats.WinRate < 60 ? "Medium" : "Easy";
                    canvas.AddText(menuStartX + 2, menuStartY, 
                        $"{enemyType,-20}: {enemyStats.Wins,3}/{enemyStats.TotalBattles,3} wins ({enemyStats.WinRate,5:F1}%) [{difficulty}] | Avg Turns: {enemyStats.AverageTurns,5:F1}", 
                        AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
            }
            menuStartY += 2;
            
            // Best and worst matchups (already calculated above for line count)
            if (bestMatchups.Count > 0)
            {
                canvas.AddText(menuStartX, menuStartY, "Best Matchups (Highest Win Rate):", AsciiArtAssets.Colors.Green);
                menuStartY++;
                foreach (var matchup in bestMatchups)
                {
                    canvas.AddText(menuStartX + 2, menuStartY, 
                        $"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% ({matchup.PlayerWins}/{matchup.TotalBattles})", 
                        AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                menuStartY += 2;
            }
            
            if (worstMatchups.Count > 0)
            {
                canvas.AddText(menuStartX, menuStartY, "Worst Matchups (Lowest Win Rate):", AsciiArtAssets.Colors.Red);
                menuStartY++;
                foreach (var matchup in worstMatchups)
                {
                    canvas.AddText(menuStartX + 2, menuStartY, 
                        $"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% ({matchup.PlayerWins}/{matchup.TotalBattles})", 
                        AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                menuStartY += 2;
            }
            
            // Note about detailed view
            canvas.AddText(menuStartX, menuStartY, "Note: Full matrix view available in detailed results", AsciiArtAssets.Colors.Yellow);
            menuStartY += 2;
            
            // Back option
            string backText = MenuOptionFormatter.Format(0, "Back to Battle Statistics Menu");
            var backOption = new ClickableElement
            {
                X = menuStartX,
                Y = menuStartY,
                Width = backText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = backText
            };
            clickableElements.Add(backOption);
            canvas.AddText(menuStartX, menuStartY, backText, AsciiArtAssets.Colors.White);
            
            return currentLineCount;
        }
    }
}

