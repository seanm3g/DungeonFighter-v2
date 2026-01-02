using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for CombatResultsColoredText
    /// Tests colored combat result formatting, damage display, and message generation
    /// </summary>
    public static class CombatResultsColoredTextTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatResultsColoredText tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatResultsColoredText Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormatDamageDisplayColored();
            TestFormatRollInfoColored();
            TestFormatMissMessageColored();
            TestFormatNonAttackActionColored();
            TestFormatHealthMilestoneColored();
            TestFormatBlockMessageColored();
            TestFormatDodgeMessageColored();
            TestFormatStatusEffectColored();
            TestFormatHealingMessageColored();
            TestFormatVictoryMessageColored();
            TestFormatDefeatMessageColored();

            TestBase.PrintSummary("CombatResultsColoredText Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region FormatDamageDisplayColored Tests

        private static void TestFormatDamageDisplayColored()
        {
            Console.WriteLine("--- Testing FormatDamageDisplayColored ---");

            try
            {
                var attacker = TestDataBuilders.Character()
                    .WithName("Attacker")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var target = TestDataBuilders.Enemy()
                    .WithName("Target")
                    .WithHealth(100)
                    .Build();

                var action = TestDataBuilders.CreateMockAction("JAB");

                var (damageText, rollInfo) = CombatResultsColoredText.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 1.0, 1.0, 0, 10, 1);

                TestBase.AssertTrue(damageText != null && damageText.Count > 0,
                    "FormatDamageDisplayColored should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(rollInfo != null && rollInfo.Count > 0,
                    "FormatDamageDisplayColored should return roll info",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with null action
                var (damageText2, rollInfo2) = CombatResultsColoredText.FormatDamageDisplayColored(
                    attacker, target, 20, 15, null, 1.0, 1.0, 0, 10, 1);

                TestBase.AssertTrue(damageText2 != null && damageText2.Count > 0,
                    "FormatDamageDisplayColored with null action should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatDamageDisplayColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatRollInfoColored Tests

        private static void TestFormatRollInfoColored()
        {
            Console.WriteLine("\n--- Testing FormatRollInfoColored ---");

            try
            {
                var result = CombatResultsColoredText.FormatRollInfoColored(
                    10, 5, 15, 5, 1.5, 1.0, TestDataBuilders.CreateMockAction("JAB"));

                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FormatRollInfoColored should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with negative rollBonus
                var result2 = CombatResultsColoredText.FormatRollInfoColored(
                    10, -2, 15, 5, 1.5, null, null);

                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "FormatRollInfoColored with negative rollBonus should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with zero speed
                var result3 = CombatResultsColoredText.FormatRollInfoColored(
                    10, 0, 15, 5, 0, null, null);

                TestBase.AssertTrue(result3 != null && result3.Count > 0,
                    "FormatRollInfoColored with zero speed should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatRollInfoColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatMissMessageColored Tests

        private static void TestFormatMissMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatMissMessageColored ---");

            try
            {
                var attacker = TestDataBuilders.Character()
                    .WithName("Attacker")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var target = TestDataBuilders.Enemy()
                    .WithName("Target")
                    .WithHealth(100)
                    .Build();

                var action = TestDataBuilders.CreateMockAction("JAB");

                var (missText, rollInfo) = CombatResultsColoredText.FormatMissMessageColored(
                    attacker, target, action, 5, 0, 5);

                TestBase.AssertTrue(missText != null && missText.Count > 0,
                    "FormatMissMessageColored should return miss text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(rollInfo != null && rollInfo.Count > 0,
                    "FormatMissMessageColored should return roll info",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatMissMessageColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatNonAttackActionColored Tests

        private static void TestFormatNonAttackActionColored()
        {
            Console.WriteLine("\n--- Testing FormatNonAttackActionColored ---");

            try
            {
                var source = TestDataBuilders.Character()
                    .WithName("Healer")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var target = TestDataBuilders.Character()
                    .WithName("Patient")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var action = TestDataBuilders.CreateMockAction("HEAL", RPGGame.ActionType.Heal);

                var (actionText, rollInfo) = CombatResultsColoredText.FormatNonAttackActionColored(
                    source, target, action, 10, 0);

                TestBase.AssertTrue(actionText != null && actionText.Count > 0,
                    "FormatNonAttackActionColored should return action text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(rollInfo != null && rollInfo.Count > 0,
                    "FormatNonAttackActionColored should return roll info",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatNonAttackActionColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatHealthMilestoneColored Tests

        private static void TestFormatHealthMilestoneColored()
        {
            Console.WriteLine("\n--- Testing FormatHealthMilestoneColored ---");

            try
            {
                var actor = TestDataBuilders.Character()
                    .WithName("Player")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                // Test near death (<= 10%)
                var result1 = CombatResultsColoredText.FormatHealthMilestoneColored(actor, 0.05);
                TestBase.AssertTrue(result1 != null && result1.Count > 0,
                    "FormatHealthMilestoneColored should return text for near death",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test critically wounded (<= 25%)
                var result2 = CombatResultsColoredText.FormatHealthMilestoneColored(actor, 0.20);
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "FormatHealthMilestoneColored should return text for critically wounded",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test badly wounded (<= 50%)
                var result3 = CombatResultsColoredText.FormatHealthMilestoneColored(actor, 0.40);
                TestBase.AssertTrue(result3 != null && result3.Count > 0,
                    "FormatHealthMilestoneColored should return text for badly wounded",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test above 50% (should not add status)
                var result4 = CombatResultsColoredText.FormatHealthMilestoneColored(actor, 0.75);
                TestBase.AssertTrue(result4 != null,
                    "FormatHealthMilestoneColored should return text even above 50%",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatHealthMilestoneColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatBlockMessageColored Tests

        private static void TestFormatBlockMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatBlockMessageColored ---");

            try
            {
                var defender = TestDataBuilders.Character()
                    .WithName("Defender")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var attacker = TestDataBuilders.Enemy()
                    .WithName("Attacker")
                    .WithHealth(100)
                    .Build();

                var result = CombatResultsColoredText.FormatBlockMessageColored(defender, attacker, 10);

                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FormatBlockMessageColored should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatBlockMessageColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatDodgeMessageColored Tests

        private static void TestFormatDodgeMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatDodgeMessageColored ---");

            try
            {
                var defender = TestDataBuilders.Character()
                    .WithName("Defender")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var attacker = TestDataBuilders.Enemy()
                    .WithName("Attacker")
                    .WithHealth(100)
                    .Build();

                var result = CombatResultsColoredText.FormatDodgeMessageColored(defender, attacker);

                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FormatDodgeMessageColored should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatDodgeMessageColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatStatusEffectColored Tests

        private static void TestFormatStatusEffectColored()
        {
            Console.WriteLine("\n--- Testing FormatStatusEffectColored ---");

            try
            {
                var target = TestDataBuilders.Character()
                    .WithName("Target")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                // Test applied status effect
                var result1 = CombatResultsColoredText.FormatStatusEffectColored(target, "Poison", true, 3, 2);
                TestBase.AssertTrue(result1 != null && result1.Count > 0,
                    "FormatStatusEffectColored should return text for applied effect",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test removed status effect
                var result2 = CombatResultsColoredText.FormatStatusEffectColored(target, "Poison", false);
                TestBase.AssertTrue(result2 != null && result2.Count > 0,
                    "FormatStatusEffectColored should return text for removed effect",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test without duration/stacks
                var result3 = CombatResultsColoredText.FormatStatusEffectColored(target, "Burning", true);
                TestBase.AssertTrue(result3 != null && result3.Count > 0,
                    "FormatStatusEffectColored should return text without duration/stacks",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatStatusEffectColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatHealingMessageColored Tests

        private static void TestFormatHealingMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatHealingMessageColored ---");

            try
            {
                var healer = TestDataBuilders.Character()
                    .WithName("Healer")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var target = TestDataBuilders.Character()
                    .WithName("Patient")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var result = CombatResultsColoredText.FormatHealingMessageColored(healer, target, 25);

                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FormatHealingMessageColored should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatHealingMessageColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatVictoryMessageColored Tests

        private static void TestFormatVictoryMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatVictoryMessageColored ---");

            try
            {
                var victor = TestDataBuilders.Character()
                    .WithName("Victor")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var defeated = TestDataBuilders.Enemy()
                    .WithName("Defeated")
                    .WithHealth(100)
                    .Build();

                var result = CombatResultsColoredText.FormatVictoryMessageColored(victor, defeated);

                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FormatVictoryMessageColored should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatVictoryMessageColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region FormatDefeatMessageColored Tests

        private static void TestFormatDefeatMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatDefeatMessageColored ---");

            try
            {
                var victor = TestDataBuilders.Enemy()
                    .WithName("Victor")
                    .WithHealth(100)
                    .Build();

                var defeated = TestDataBuilders.Character()
                    .WithName("Defeated")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var result = CombatResultsColoredText.FormatDefeatMessageColored(victor, defeated);

                TestBase.AssertTrue(result != null && result.Count > 0,
                    "FormatDefeatMessageColored should return formatted text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatDefeatMessageColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
