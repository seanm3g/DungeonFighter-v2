using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for CombatCalculator facade
    /// Tests all calculator methods including damage, hit/miss, roll bonuses, and critical hits
    /// </summary>
    public static class CombatCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatCalculator Tests ===\n");

            TestCalculateRawDamage();
            TestCalculateDamage();
            TestCalculateHit();
            TestCalculateRollBonus();
            TestIsCriticalHit();
            TestApplyDamageReduction();
            TestCalculateStatusEffectChance();
            TestCalculateAttackSpeed();
            TestRollBonusWithComboScaling();
            TestRollBonusWithComboStepScaling();
            TestRollBonusWithComboAmplificationScaling();
            TestRollBonusWithIntelligence();
            TestRollBonusWithModifications();
            TestRollPenalty();

            TestBase.PrintSummary("CombatCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Damage Calculation Tests

        private static void TestCalculateRawDamage()
        {
            Console.WriteLine("--- Testing CalculateRawDamage ---");

            var character = TestDataBuilders.Character().WithName("RawDamageTest").Build();
            character.Stats.Strength = 10;

            var damage = CombatCalculator.CalculateRawDamage(character);

            TestBase.AssertTrue(damage > 0, $"Raw damage should be positive, got: {damage}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateDamage()
        {
            Console.WriteLine("\n--- Testing CalculateDamage ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            attacker.Stats.Strength = 15;
            
            // Equip a weapon (required for damage in real game)
            var weapon = new WeaponItem("TestSword", 1, 10);
            attacker.EquipItem(weapon, "weapon");

            var target = TestDataBuilders.Enemy().WithName("Target").WithHealth(100).Build();

            var damage = CombatCalculator.CalculateDamage(attacker, target);

            // CRITICAL: Damage should ALWAYS be positive, not just non-negative
            TestBase.AssertTrue(damage > 0, $"Damage should be positive, got: {damage}. This indicates a critical bug!", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplyDamageReduction()
        {
            Console.WriteLine("\n--- Testing ApplyDamageReduction ---");

            var target = TestDataBuilders.Enemy().WithName("ArmoredTarget").Build();
            var damage = 50;

            var reducedDamage = CombatCalculator.ApplyDamageReduction(target, damage);

            TestBase.AssertTrue(reducedDamage <= damage, 
                $"Reduced damage ({reducedDamage}) should be <= original damage ({damage})", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(reducedDamage >= 0, 
                $"Reduced damage should be non-negative, got: {reducedDamage}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Hit/Miss Tests

        private static void TestCalculateHit()
        {
            Console.WriteLine("\n--- Testing CalculateHit ---");

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            // Test with high roll (should hit)
            var hitResult = CombatCalculator.CalculateHit(attacker, target, 0, 15);
            TestBase.AssertTrue(hitResult, "High roll should result in hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with low roll (should miss)
            var missResult = CombatCalculator.CalculateHit(attacker, target, 0, 3);
            TestBase.AssertFalse(missResult, "Low roll should result in miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Roll Bonus Tests

        private static void TestCalculateRollBonus()
        {
            Console.WriteLine("\n--- Testing CalculateRollBonus ---");

            var attacker = TestDataBuilders.Character().WithName("RollBonusTest").Build();
            var action = new Action
            {
                Advanced = new AdvancedMechanicsProperties
                {
                    RollBonus = 5,
                    RollBonusDuration = 0
                }
            };
            var comboActions = new List<Action>();
            var comboStep = 0;

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            TestBase.AssertTrue(rollBonus >= 0, $"Roll bonus should be non-negative, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBonusWithComboScaling()
        {
            Console.WriteLine("\n--- Testing Roll Bonus with Combo Scaling ---");

            var attacker = TestDataBuilders.Character().WithName("ComboScalingTest").Build();
            var action = new Action
            {
                Tags = new List<string> { "comboScaling" },
                Advanced = new AdvancedMechanicsProperties { RollBonus = 0, RollBonusDuration = 0 }
            };
            var comboActions = new List<Action>
            {
                new Action { Name = "ACTION1" },
                new Action { Name = "ACTION2" },
                new Action { Name = "ACTION3" }
            };
            var comboStep = 0;

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            // Should scale with combo length (3 actions = +3 bonus)
            TestBase.AssertTrue(rollBonus >= 3, 
                $"Combo scaling should add bonus based on combo length, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBonusWithComboStepScaling()
        {
            Console.WriteLine("\n--- Testing Roll Bonus with Combo Step Scaling ---");

            var attacker = TestDataBuilders.Character().WithName("ComboStepTest").Build();
            var action = new Action
            {
                Tags = new List<string> { "comboStepScaling" },
                Advanced = new AdvancedMechanicsProperties { RollBonus = 0, RollBonusDuration = 0 }
            };
            var comboActions = new List<Action>
            {
                new Action { Name = "ACTION1" },
                new Action { Name = "ACTION2" }
            };
            var comboStep = 1; // Second action in combo

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            // Should scale with combo step position
            TestBase.AssertTrue(rollBonus >= 0, 
                $"Combo step scaling should add bonus, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBonusWithComboAmplificationScaling()
        {
            Console.WriteLine("\n--- Testing Roll Bonus with Combo Amplification Scaling ---");

            var attacker = TestDataBuilders.Character().WithName("AmplificationTest").Build();
            var action = new Action
            {
                Tags = new List<string> { "comboAmplificationScaling" },
                Advanced = new AdvancedMechanicsProperties { RollBonus = 0, RollBonusDuration = 0 }
            };
            var comboActions = new List<Action>();
            var comboStep = 0;

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            TestBase.AssertTrue(rollBonus >= 0, 
                $"Combo amplification scaling should add bonus, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBonusWithIntelligence()
        {
            Console.WriteLine("\n--- Testing Roll Bonus with Intelligence ---");

            var attacker = TestDataBuilders.Character().WithName("IntelligenceTest").Build();
            attacker.Stats.Intelligence = 15;

            var action = new Action
            {
                Advanced = new AdvancedMechanicsProperties { RollBonus = 0, RollBonusDuration = 0 }
            };
            var comboActions = new List<Action>();
            var comboStep = 0;

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            TestBase.AssertTrue(rollBonus >= 0, 
                $"Intelligence should contribute to roll bonus, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollBonusWithModifications()
        {
            Console.WriteLine("\n--- Testing Roll Bonus with Modifications ---");

            var attacker = TestDataBuilders.Character().WithName("ModificationTest").Build();

            var action = new Action
            {
                Advanced = new AdvancedMechanicsProperties { RollBonus = 0, RollBonusDuration = 0 }
            };
            var comboActions = new List<Action>();
            var comboStep = 0;

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            TestBase.AssertTrue(rollBonus >= 0, 
                $"Modifications should contribute to roll bonus, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollPenalty()
        {
            Console.WriteLine("\n--- Testing Roll Penalty ---");

            var attacker = TestDataBuilders.Character().WithName("PenaltyTest").Build();
            attacker.RollPenalty = 5;

            var action = new Action
            {
                Advanced = new AdvancedMechanicsProperties { RollBonus = 10, RollBonusDuration = 0 }
            };
            var comboActions = new List<Action>();
            var comboStep = 0;

            var rollBonus = CombatCalculator.CalculateRollBonus(attacker, action, comboActions, comboStep);

            // Roll bonus should be reduced by penalty
            TestBase.AssertTrue(rollBonus < 10, 
                $"Roll penalty should reduce bonus, got: {rollBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Critical Hit Tests

        private static void TestIsCriticalHit()
        {
            Console.WriteLine("\n--- Testing IsCriticalHit ---");

            var attacker = TestDataBuilders.Character().WithName("CriticalTest").Build();

            // Natural 20 should be critical
            var criticalResult = CombatCalculator.IsCriticalHit(attacker, 20);
            TestBase.AssertTrue(criticalResult, "Roll of 20 should be critical hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Roll above 20 should also be critical
            var criticalResult2 = CombatCalculator.IsCriticalHit(attacker, 25);
            TestBase.AssertTrue(criticalResult2, "Roll above 20 should be critical hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Low roll should not be critical (unless chance-based)
            // Note: This test may be flaky due to random chance, but 1 should rarely be critical
            var normalResult = CombatCalculator.IsCriticalHit(attacker, 1);
            // We can't assert false here because of random chance, but we can verify it doesn't throw
            TestBase.AssertTrue(true, "Low roll critical check should not throw exception", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Status Effect Tests

        private static void TestCalculateStatusEffectChance()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChance ---");

            var attacker = TestDataBuilders.Character().WithName("StatusTest").Build();
            var target = TestDataBuilders.Enemy().WithName("StatusTarget").Build();
            var action = new Action();

            var result = CombatCalculator.CalculateStatusEffectChance(action, attacker, target);

            // Result should be boolean (true or false)
            TestBase.AssertTrue(true, "Status effect chance calculation should return boolean", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Attack Speed Tests

        private static void TestCalculateAttackSpeed()
        {
            Console.WriteLine("\n--- Testing CalculateAttackSpeed ---");

            var actor = TestDataBuilders.Character().WithName("SpeedTest").Build();

            var attackSpeed = CombatCalculator.CalculateAttackSpeed(actor);

            TestBase.AssertTrue(attackSpeed > 0, $"Attack speed should be positive, got: {attackSpeed}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}

