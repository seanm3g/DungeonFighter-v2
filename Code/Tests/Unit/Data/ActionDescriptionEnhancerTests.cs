using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for ActionDescriptionEnhancer
    /// Tests action description enhancement with various modifiers
    /// </summary>
    public static class ActionDescriptionEnhancerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionDescriptionEnhancer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionDescriptionEnhancer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBasicDescription();
            TestRollBonus();
            TestDamageMultiplier();
            TestComboBonus();
            TestStatusEffects();
            TestMultiHit();
            TestSelfDamage();
            TestStatBonus();
            TestSpecialEffects();
            TestMultipleModifiers();
            TestEmptyDescription();

            TestBase.PrintSummary("ActionDescriptionEnhancer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Basic Tests

        private static void TestBasicDescription()
        {
            Console.WriteLine("--- Testing Basic Description ---");

            var data = new ActionData
            {
                Description = "Basic attack",
                RollBonus = 0,
                DamageMultiplier = 1.0
            };

            var result = ActionDescriptionEnhancer.EnhanceActionDescription(data);
            TestBase.AssertEqual("Basic attack", result,
                "Basic description without modifiers should remain unchanged",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEmptyDescription()
        {
            Console.WriteLine("\n--- Testing Empty Description ---");

            var data = new ActionData
            {
                Description = "",
                RollBonus = 5
            };

            var result = ActionDescriptionEnhancer.EnhanceActionDescription(data);
            TestBase.AssertTrue(result.Contains("Roll: +5"),
                "Empty description should still show modifiers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Modifier Tests

        private static void TestRollBonus()
        {
            Console.WriteLine("\n--- Testing Roll Bonus ---");

            // Positive roll bonus
            var data1 = new ActionData
            {
                Description = "Attack",
                RollBonus = 5
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("Roll: +5"),
                "Positive roll bonus should be formatted correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Negative roll bonus
            var data2 = new ActionData
            {
                Description = "Attack",
                RollBonus = -3
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertTrue(result2.Contains("Roll: -3"),
                "Negative roll bonus should be formatted correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Zero roll bonus (should not appear)
            var data3 = new ActionData
            {
                Description = "Attack",
                RollBonus = 0
            };
            var result3 = ActionDescriptionEnhancer.EnhanceActionDescription(data3);
            TestBase.AssertFalse(result3.Contains("Roll:"),
                "Zero roll bonus should not appear in description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDamageMultiplier()
        {
            Console.WriteLine("\n--- Testing Damage Multiplier ---");

            // Multiplier > 1.0
            var data1 = new ActionData
            {
                Description = "Attack",
                DamageMultiplier = 2.5
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("Damage: 2.5x"),
                "Damage multiplier > 1.0 should appear in description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Multiplier = 1.0 (should not appear)
            var data2 = new ActionData
            {
                Description = "Attack",
                DamageMultiplier = 1.0
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertFalse(result2.Contains("Damage:"),
                "Damage multiplier of 1.0 should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Multiplier < 1.0
            var data3 = new ActionData
            {
                Description = "Attack",
                DamageMultiplier = 0.5
            };
            var result3 = ActionDescriptionEnhancer.EnhanceActionDescription(data3);
            TestBase.AssertTrue(result3.Contains("Damage: 0.5x"),
                "Damage multiplier < 1.0 should appear in description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboBonus()
        {
            Console.WriteLine("\n--- Testing Combo Bonus ---");

            // Valid combo bonus
            var data1 = new ActionData
            {
                Description = "Attack",
                ComboBonusAmount = 5,
                ComboBonusDuration = 3
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("Combo: +5 for 3 turns"),
                "Combo bonus should appear with amount and duration",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Zero amount (should not appear)
            var data2 = new ActionData
            {
                Description = "Attack",
                ComboBonusAmount = 0,
                ComboBonusDuration = 3
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertFalse(result2.Contains("Combo:"),
                "Combo bonus with zero amount should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Zero duration (should not appear)
            var data3 = new ActionData
            {
                Description = "Attack",
                ComboBonusAmount = 5,
                ComboBonusDuration = 0
            };
            var result3 = ActionDescriptionEnhancer.EnhanceActionDescription(data3);
            TestBase.AssertFalse(result3.Contains("Combo:"),
                "Combo bonus with zero duration should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatusEffects()
        {
            Console.WriteLine("\n--- Testing Status Effects ---");

            var data = new ActionData
            {
                Description = "Attack",
                CausesBleed = true,
                CausesWeaken = true,
                CausesSlow = false,
                CausesPoison = true
            };

            var result = ActionDescriptionEnhancer.EnhanceActionDescription(data);
            TestBase.AssertTrue(result.Contains("Causes Bleed"),
                "Bleed status effect should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("Causes Weaken"),
                "Weaken status effect should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(result.Contains("Causes Slow"),
                "Slow status effect should not appear when false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("Causes Poison"),
                "Poison status effect should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHit()
        {
            Console.WriteLine("\n--- Testing Multi-Hit ---");

            // Multi-hit count > 1
            var data1 = new ActionData
            {
                Description = "Attack",
                MultiHitCount = 3
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("Multi-hit: 3 attacks"),
                "Multi-hit should appear with count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Multi-hit count = 1 (should not appear)
            var data2 = new ActionData
            {
                Description = "Attack",
                MultiHitCount = 1
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertFalse(result2.Contains("Multi-hit:"),
                "Multi-hit count of 1 should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSelfDamage()
        {
            Console.WriteLine("\n--- Testing Self-Damage ---");

            // Self-damage > 0
            var data1 = new ActionData
            {
                Description = "Attack",
                SelfDamagePercent = 25
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("Self-damage: 25%"),
                "Self-damage should appear with percentage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Self-damage = 0 (should not appear)
            var data2 = new ActionData
            {
                Description = "Attack",
                SelfDamagePercent = 0
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertFalse(result2.Contains("Self-damage:"),
                "Self-damage of 0% should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatBonus()
        {
            Console.WriteLine("\n--- Testing Stat Bonus ---");

            // Stat bonus with duration
            var data1 = new ActionData
            {
                Description = "Attack",
                StatBonus = 10,
                StatBonusType = "Strength",
                StatBonusDuration = 5
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("+10 Strength (5 turns)"),
                "Stat bonus should appear with type and duration",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Stat bonus with dungeon duration
            var data2 = new ActionData
            {
                Description = "Attack",
                StatBonus = 5,
                StatBonusType = "Agility",
                StatBonusDuration = -1
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertTrue(result2.Contains("+5 Agility (dungeon)"),
                "Stat bonus with -1 duration should show 'dungeon'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Stat bonus with empty type (should not appear)
            var data3 = new ActionData
            {
                Description = "Attack",
                StatBonus = 10,
                StatBonusType = "",
                StatBonusDuration = 5
            };
            var result3 = ActionDescriptionEnhancer.EnhanceActionDescription(data3);
            TestBase.AssertFalse(result3.Contains("+10"),
                "Stat bonus with empty type should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Stat bonus = 0 (should not appear)
            var data4 = new ActionData
            {
                Description = "Attack",
                StatBonus = 0,
                StatBonusType = "Strength",
                StatBonusDuration = 5
            };
            var result4 = ActionDescriptionEnhancer.EnhanceActionDescription(data4);
            TestBase.AssertFalse(result4.Contains("Strength"),
                "Stat bonus of 0 should not appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSpecialEffects()
        {
            Console.WriteLine("\n--- Testing Special Effects ---");

            // Skip next turn
            var data1 = new ActionData
            {
                Description = "Attack",
                SkipNextTurn = true
            };
            var result1 = ActionDescriptionEnhancer.EnhanceActionDescription(data1);
            TestBase.AssertTrue(result1.Contains("Skips next turn"),
                "Skip next turn effect should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Repeat last action
            var data2 = new ActionData
            {
                Description = "Attack",
                RepeatLastAction = true
            };
            var result2 = ActionDescriptionEnhancer.EnhanceActionDescription(data2);
            TestBase.AssertTrue(result2.Contains("Repeats last action"),
                "Repeat last action effect should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleModifiers()
        {
            Console.WriteLine("\n--- Testing Multiple Modifiers ---");

            var data = new ActionData
            {
                Description = "Powerful attack",
                RollBonus = 5,
                DamageMultiplier = 2.0,
                CausesBleed = true,
                MultiHitCount = 2,
                StatBonus = 10,
                StatBonusType = "Strength",
                StatBonusDuration = 3
            };

            var result = ActionDescriptionEnhancer.EnhanceActionDescription(data);
            TestBase.AssertTrue(result.Contains("Powerful attack"),
                "Base description should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("Roll: +5"),
                "Roll bonus should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("Damage: 2.0x"),
                "Damage multiplier should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("Causes Bleed"),
                "Status effect should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("Multi-hit: 2 attacks"),
                "Multi-hit should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains("+10 Strength (3 turns)"),
                "Stat bonus should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Contains(" | "),
                "Modifiers should be separated from description with ' | '",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
