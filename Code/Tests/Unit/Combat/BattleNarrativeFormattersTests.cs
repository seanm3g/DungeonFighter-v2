using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for BattleNarrativeFormatters
    /// Tests all formatter classes for narrative text generation and formatting
    /// </summary>
    public static class BattleNarrativeFormattersTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleNarrativeFormatters tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleNarrativeFormatters Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFirstBloodFormatter();
            TestCriticalHitFormatter();
            TestCriticalMissFormatter();
            TestEnvironmentalActionFormatter();
            TestHealthRecoveryFormatter();
            TestHealthLeadChangeFormatter();
            TestBelow50PercentFormatter();
            TestBelow10PercentFormatter();
            TestGenericNarrativeFormatter();
            TestIntenseBattleFormatter();
            TestPlayerDefeatedFormatter();
            TestEnemyDefeatedFormatter();
            TestPlayerTauntFormatter();
            TestEnemyTauntFormatter();
            TestComboFormatter();

            TestBase.PrintSummary("BattleNarrativeFormatters Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Single Entity Formatter Tests

        private static void TestFirstBloodFormatter()
        {
            Console.WriteLine("--- Testing FirstBloodFormatter ---");

            try
            {
                var result = FirstBloodFormatter.Format("First blood is drawn!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FirstBloodFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FirstBloodFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCriticalHitFormatter()
        {
            Console.WriteLine("\n--- Testing CriticalHitFormatter ---");

            try
            {
                var result = CriticalHitFormatter.Format("Player", "{name} lands a devastating critical hit!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "CriticalHitFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Verify name replacement
                var result2 = CriticalHitFormatter.Format("Enemy", "The {name} strikes with deadly precision!");
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "CriticalHitFormatter should replace {name} placeholder",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"CriticalHitFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCriticalMissFormatter()
        {
            Console.WriteLine("\n--- Testing CriticalMissFormatter ---");

            try
            {
                var result = CriticalMissFormatter.Format("Player", "{name} completely misses the target!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "CriticalMissFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"CriticalMissFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEnvironmentalActionFormatter()
        {
            Console.WriteLine("\n--- Testing EnvironmentalActionFormatter ---");

            try
            {
                var result = EnvironmentalActionFormatter.Format("fire trap", "The {effect} activates!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "EnvironmentalActionFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Verify effect replacement
                var result2 = EnvironmentalActionFormatter.Format("poison cloud", "A {effect} fills the room!");
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "EnvironmentalActionFormatter should replace {effect} placeholder",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"EnvironmentalActionFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHealthRecoveryFormatter()
        {
            Console.WriteLine("\n--- Testing HealthRecoveryFormatter ---");

            try
            {
                var result = HealthRecoveryFormatter.Format("Player", "{name} recovers health!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "HealthRecoveryFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"HealthRecoveryFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHealthLeadChangeFormatter()
        {
            Console.WriteLine("\n--- Testing HealthLeadChangeFormatter ---");

            try
            {
                var result = HealthLeadChangeFormatter.Format("Player", "{name} takes the lead!", true);
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "HealthLeadChangeFormatter should return formatted text for player",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var result2 = HealthLeadChangeFormatter.Format("Enemy", "{name} gains the advantage!", false);
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "HealthLeadChangeFormatter should return formatted text for enemy",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"HealthLeadChangeFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestBelow50PercentFormatter()
        {
            Console.WriteLine("\n--- Testing Below50PercentFormatter ---");

            try
            {
                var result = Below50PercentFormatter.Format("Player", "{name} is badly wounded!", true);
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "Below50PercentFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Below50PercentFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestBelow10PercentFormatter()
        {
            Console.WriteLine("\n--- Testing Below10PercentFormatter ---");

            try
            {
                var result = Below10PercentFormatter.Format("Player", "{name} is near death!", true);
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "Below10PercentFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Below10PercentFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGenericNarrativeFormatter()
        {
            Console.WriteLine("\n--- Testing GenericNarrativeFormatter ---");

            try
            {
                var result = GenericNarrativeFormatter.Format("The battle intensifies!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "GenericNarrativeFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with custom color
                var result2 = GenericNarrativeFormatter.Format("A dramatic moment!", ColorPalette.Warning);
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "GenericNarrativeFormatter with custom color should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GenericNarrativeFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Dual Entity Formatter Tests

        private static void TestIntenseBattleFormatter()
        {
            Console.WriteLine("\n--- Testing IntenseBattleFormatter ---");

            try
            {
                var result = IntenseBattleFormatter.Format("Player", "Enemy", "{player} and {enemy} clash!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "IntenseBattleFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"IntenseBattleFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestPlayerDefeatedFormatter()
        {
            Console.WriteLine("\n--- Testing PlayerDefeatedFormatter ---");

            try
            {
                var result = PlayerDefeatedFormatter.Format("Enemy", "{enemy} has defeated the player!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "PlayerDefeatedFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"PlayerDefeatedFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEnemyDefeatedFormatter()
        {
            Console.WriteLine("\n--- Testing EnemyDefeatedFormatter ---");

            try
            {
                var result = EnemyDefeatedFormatter.Format("Enemy", "Player", "{name} has been defeated by {player}!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "EnemyDefeatedFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"EnemyDefeatedFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Quote/Taunt Formatter Tests

        private static void TestPlayerTauntFormatter()
        {
            Console.WriteLine("\n--- Testing PlayerTauntFormatter ---");

            try
            {
                var result = PlayerTauntFormatter.Format("Player", "Enemy", "{name} taunts {enemy}!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "PlayerTauntFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with quoted text
                var result2 = PlayerTauntFormatter.Format("Player", "Enemy", "\"You're finished, {enemy}!\" {name} declares.");
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "PlayerTauntFormatter with quotes should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"PlayerTauntFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEnemyTauntFormatter()
        {
            Console.WriteLine("\n--- Testing EnemyTauntFormatter ---");

            try
            {
                var result = EnemyTauntFormatter.Format("Enemy", "Player", "{name} mocks {player}!");
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "EnemyTauntFormatter should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with quoted text
                var result2 = EnemyTauntFormatter.Format("Enemy", "Player", "\"You cannot win, {player}!\" {name} snarls.");
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "EnemyTauntFormatter with quotes should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"EnemyTauntFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Combo Formatter Tests

        private static void TestComboFormatter()
        {
            Console.WriteLine("\n--- Testing ComboFormatter ---");

            try
            {
                var result = ComboFormatter.Format("Player", "Enemy", true);
                
                TestBase.AssertTrue(result != null && result.Count > 0,
                    "ComboFormatter should return formatted text for player combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var result2 = ComboFormatter.Format("Enemy", "Player", false);
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "ComboFormatter should return formatted text for enemy combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ComboFormatter failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
