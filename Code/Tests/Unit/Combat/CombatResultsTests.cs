using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for CombatResults
    /// Tests combat result formatting and UI formatting
    /// </summary>
    public static class CombatResultsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatResults tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatResults Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormatDamageDisplaySeparated();
            TestFormatDamageDisplayColored();
            TestFormatMissMessageColored();
            TestFormatNonAttackActionColored();
            TestFormatHealthMilestoneColored();
            TestFormatBlockMessageColored();
            TestFormatDodgeMessageColored();
            TestFormatStatusEffectColored();
            TestFormatHealingMessageColored();
            TestCheckHealthMilestones();
            TestExecuteActionWithUIAndStatusEffectsColored();

            TestBase.PrintSummary("CombatResults Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Damage Display Tests

        private static void TestFormatDamageDisplaySeparated()
        {
            Console.WriteLine("--- Testing FormatDamageDisplaySeparated ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var (damageText, rollInfo) = CombatResults.FormatDamageDisplaySeparated(
                attacker, target, 50, 45, action, 1.0, 1.0, 0, 10);

            TestBase.AssertNotNull(damageText,
                "Damage text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (damageText != null)
            {
                TestBase.AssertTrue(damageText.Contains(attacker.Name),
                    "Damage text should contain attacker name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestFormatDamageDisplayColored()
        {
            Console.WriteLine("\n--- Testing FormatDamageDisplayColored ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, target, 50, 45, action, 1.0, 1.0, 0, 10);

            TestBase.AssertNotNull(damageText,
                "Colored damage text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Colored roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Miss/Block/Dodge Tests

        private static void TestFormatMissMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatMissMessageColored ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var (missText, rollInfo) = CombatResults.FormatMissMessageColored(attacker, target, action, 0, 0, 3);

            TestBase.AssertNotNull(missText,
                "Miss text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Miss roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatBlockMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatBlockMessageColored ---");

            var defender = TestDataBuilders.Character().WithName("Defender").Build();
            var attacker = TestDataBuilders.Enemy().WithName("Attacker").Build();

            var blockText = CombatResults.FormatBlockMessageColored(defender, attacker, 10);

            TestBase.AssertNotNull(blockText,
                "Block text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatDodgeMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatDodgeMessageColored ---");

            var defender = TestDataBuilders.Character().WithName("Defender").Build();
            var attacker = TestDataBuilders.Enemy().WithName("Attacker").Build();

            var dodgeText = CombatResults.FormatDodgeMessageColored(defender, attacker);

            TestBase.AssertNotNull(dodgeText,
                "Dodge text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Other Action Tests

        private static void TestFormatNonAttackActionColored()
        {
            Console.WriteLine("\n--- Testing FormatNonAttackActionColored ---");

            var actor = TestDataBuilders.Character().WithName("Actor").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("HEAL");
            action.Type = ActionType.Heal;

            var (actionText, rollInfo) = CombatResults.FormatNonAttackActionColored(actor, target, action, 15, 2);

            TestBase.AssertNotNull(actionText,
                "Non-attack action text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(rollInfo,
                "Non-attack roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatHealingMessageColored()
        {
            Console.WriteLine("\n--- Testing FormatHealingMessageColored ---");

            var healer = TestDataBuilders.Character().WithName("Healer").Build();
            var target = TestDataBuilders.Character().WithName("Target").Build();

            var healText = CombatResults.FormatHealingMessageColored(healer, target, 25);

            TestBase.AssertNotNull(healText,
                "Healing text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatStatusEffectColored()
        {
            Console.WriteLine("\n--- Testing FormatStatusEffectColored ---");

            var target = TestDataBuilders.Character().WithName("Target").Build();

            var effectText = CombatResults.FormatStatusEffectColored(target, "Bleed", true, 3, 1);

            TestBase.AssertNotNull(effectText,
                "Status effect text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Health Milestone Tests

        private static void TestFormatHealthMilestoneColored()
        {
            Console.WriteLine("\n--- Testing FormatHealthMilestoneColored ---");

            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var milestoneText = CombatResults.FormatHealthMilestoneColored(actor, 0.5);

            TestBase.AssertNotNull(milestoneText,
                "Health milestone text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCheckHealthMilestones()
        {
            Console.WriteLine("\n--- Testing CheckHealthMilestones ---");

            var actor = TestDataBuilders.Character().WithName("Actor").Build();

            var milestones = CombatResults.CheckHealthMilestones(actor, 10);

            TestBase.AssertNotNull(milestones,
                "Health milestones should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Execution Tests

        private static void TestExecuteActionWithUIAndStatusEffectsColored()
        {
            Console.WriteLine("\n--- Testing ExecuteActionWithUIAndStatusEffectsColored ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();
            var action = TestDataBuilders.CreateMockAction("JAB");

            var result = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                attacker, target, action, null, null, null);

            TestBase.AssertNotNull(result.mainResult.actionText,
                "Action text should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(result.mainResult.rollInfo,
                "Roll info should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(result.statusEffects,
                "Status effects should not be null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
