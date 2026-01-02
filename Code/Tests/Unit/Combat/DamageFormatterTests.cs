using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Combat.Formatting;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for DamageFormatter
    /// Tests damage display formatting, spacing, and color application
    /// </summary>
    public static class DamageFormatterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DamageFormatter tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DamageFormatter Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestAddForAmountUnit();
            TestAddWithAction();
            TestAddUsesAction();
            TestAddHitsTarget();
            TestAddAttackVsArmor();
            TestAddSpeedInfo();
            TestAddAmpInfo();
            TestAddTakesDamageFrom();
            TestAddBracketedActorTakesDamage();
            TestAddActorTakesDamage();
            TestAddBracketedActorNoLongerAffected();
            TestAddActorNoLongerAffected();
            TestAddEffectStacksRemain();
            TestFormatDamageDisplayColored();

            TestBase.PrintSummary("DamageFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region AddForAmountUnit Tests

        private static void TestAddForAmountUnit()
        {
            Console.WriteLine("--- Testing AddForAmountUnit ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddForAmountUnit(builder, "10", ColorPalette.Damage, "damage", ColorPalette.Info);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddForAmountUnit should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Color overload
            var builder2 = new ColoredTextBuilder();
            DamageFormatter.AddForAmountUnit(builder2, "20", ColorPalette.Damage, "health", Colors.White);
            var result2 = builder2.Build();

            TestBase.AssertTrue(result2.Count > 0,
                "AddForAmountUnit (Color overload) should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddWithAction Tests

        private static void TestAddWithAction()
        {
            Console.WriteLine("\n--- Testing AddWithAction ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddWithAction(builder, "JAB", ColorPalette.Green);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddWithAction should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddUsesAction Tests

        private static void TestAddUsesAction()
        {
            Console.WriteLine("\n--- Testing AddUsesAction ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddUsesAction(builder, "HEAL", ColorPalette.Green);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddUsesAction should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddHitsTarget Tests

        private static void TestAddHitsTarget()
        {
            Console.WriteLine("\n--- Testing AddHitsTarget ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddHitsTarget(builder, "Enemy", ColorPalette.Enemy);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddHitsTarget should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with hitsColor
            var builder2 = new ColoredTextBuilder();
            DamageFormatter.AddHitsTarget(builder2, "Player", ColorPalette.Player, ColorPalette.Success);
            var result2 = builder2.Build();

            TestBase.AssertTrue(result2.Count > 0,
                "AddHitsTarget with hitsColor should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Color overload
            var builder3 = new ColoredTextBuilder();
            DamageFormatter.AddHitsTarget(builder3, "Target", Colors.Red);
            var result3 = builder3.Build();

            TestBase.AssertTrue(result3.Count > 0,
                "AddHitsTarget (Color overload) should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddAttackVsArmor Tests

        private static void TestAddAttackVsArmor()
        {
            Console.WriteLine("\n--- Testing AddAttackVsArmor ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddAttackVsArmor(builder, 15, 5);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddAttackVsArmor should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddSpeedInfo Tests

        private static void TestAddSpeedInfo()
        {
            Console.WriteLine("\n--- Testing AddSpeedInfo ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddSpeedInfo(builder, 1.5);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddSpeedInfo should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with different speed values
            var builder2 = new ColoredTextBuilder();
            DamageFormatter.AddSpeedInfo(builder2, 0.5);
            var result2 = builder2.Build();

            TestBase.AssertTrue(result2.Count > 0,
                "AddSpeedInfo with different speed should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddAmpInfo Tests

        private static void TestAddAmpInfo()
        {
            Console.WriteLine("\n--- Testing AddAmpInfo ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddAmpInfo(builder, 2.5);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddAmpInfo should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddTakesDamageFrom Tests

        private static void TestAddTakesDamageFrom()
        {
            Console.WriteLine("\n--- Testing AddTakesDamageFrom ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddTakesDamageFrom(builder, 10, "Poison", ColorPalette.Warning);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddTakesDamageFrom should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddBracketedActorTakesDamage Tests

        private static void TestAddBracketedActorTakesDamage()
        {
            Console.WriteLine("\n--- Testing AddBracketedActorTakesDamage ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddBracketedActorTakesDamage(builder, "Enemy", ColorPalette.Enemy, 15, "poison");
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddBracketedActorTakesDamage should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddActorTakesDamage Tests

        private static void TestAddActorTakesDamage()
        {
            Console.WriteLine("\n--- Testing AddActorTakesDamage ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddActorTakesDamage(builder, "Player", Colors.White, 20, "bleed");
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddActorTakesDamage should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddBracketedActorNoLongerAffected Tests

        private static void TestAddBracketedActorNoLongerAffected()
        {
            Console.WriteLine("\n--- Testing AddBracketedActorNoLongerAffected ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddBracketedActorNoLongerAffected(builder, "Enemy", ColorPalette.Enemy, "poisoned", ColorPalette.Warning);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddBracketedActorNoLongerAffected should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddActorNoLongerAffected Tests

        private static void TestAddActorNoLongerAffected()
        {
            Console.WriteLine("\n--- Testing AddActorNoLongerAffected ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddActorNoLongerAffected(builder, "Player", Colors.White, "burning", ColorPalette.Error);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddActorNoLongerAffected should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddEffectStacksRemain Tests

        private static void TestAddEffectStacksRemain()
        {
            Console.WriteLine("\n--- Testing AddEffectStacksRemain ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddEffectStacksRemain(builder, "Poison", ColorPalette.Warning, 3);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddEffectStacksRemain should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region FormatDamageDisplayColored Tests

        private static void TestFormatDamageDisplayColored()
        {
            Console.WriteLine("\n--- Testing FormatDamageDisplayColored ---");

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

                var (damageText, rollInfo) = DamageFormatter.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 1.0, 1.0, 0, 10, 1);

                TestBase.AssertTrue(damageText != null && damageText.Count > 0,
                    "FormatDamageDisplayColored should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(rollInfo != null && rollInfo.Count > 0,
                    "FormatDamageDisplayColored should return roll info",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with combo amplifier
                var (damageText2, rollInfo2) = DamageFormatter.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 2.0, 1.0, 0, 10, 1);

                TestBase.AssertTrue(damageText2 != null && damageText2.Count > 0,
                    "FormatDamageDisplayColored with combo should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with multi-hit
                var (damageText3, rollInfo3) = DamageFormatter.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 1.0, 1.0, 0, 10, 3);

                TestBase.AssertTrue(damageText3 != null && damageText3.Count > 0,
                    "FormatDamageDisplayColored with multi-hit should return damage text",
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
    }
}
