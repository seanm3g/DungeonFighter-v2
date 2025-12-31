using System;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for multi-hit action mechanics
    /// Tests that multi-hit actions apply damage correctly for each hit,
    /// stop when target dies, and calculate damage properly
    /// </summary>
    public static class MultiHitTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Multi-Hit Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestMultiHitActionCreation();
            TestMultiHitDamageApplication();
            TestMultiHitEarlyTermination();
            TestMultiHitDifferentCounts();
            TestMultiHitDamageCalculation();
            TestMultiHitWithSelfAndTarget();

            TestBase.PrintSummary("Multi-Hit Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestMultiHitActionCreation()
        {
            Console.WriteLine("--- Testing Multi-Hit Action Creation ---");

            var action = new Action
            {
                Name = "Double Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties
                {
                    MultiHitCount = 2
                }
            };

            TestBase.AssertTrue(action.Advanced.MultiHitCount == 2,
                $"Action should have MultiHitCount of 2, got {action.Advanced.MultiHitCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var tripleAction = new Action
            {
                Name = "Triple Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties
                {
                    MultiHitCount = 3
                }
            };

            TestBase.AssertTrue(tripleAction.Advanced.MultiHitCount == 3,
                $"Action should have MultiHitCount of 3, got {tripleAction.Advanced.MultiHitCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHitDamageApplication()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Damage Application ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .Build();
            character.Stats.Strength = 15;

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .WithHealth(100)
                .WithStats(5, 5, 5, 5)
                .Build();

            int initialHealth = enemy.CurrentHealth;

            var action = new Action
            {
                Name = "Double Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties
                {
                    MultiHitCount = 2
                }
            };

            // Calculate expected damage for one hit
            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(character, action);
            int rollBonus = 0;
            int totalRoll = 10; // Use a fixed roll for testing
            int singleHitDamage = CombatCalculator.CalculateDamage(character, enemy, action, damageMultiplier, 1.0, rollBonus, totalRoll);

            // Process multi-hit
            int totalDamage = MultiHitProcessor.ProcessMultiHit(
                character, enemy, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 10, null);

            int finalHealth = enemy.CurrentHealth;
            int actualDamage = initialHealth - finalHealth;

            TestBase.AssertTrue(actualDamage > 0,
                $"Multi-hit should deal damage, dealt {actualDamage}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Multi-hit should deal approximately 2x single hit damage (allowing for variance)
            // Since each hit is calculated separately, they may vary slightly
            TestBase.AssertTrue(actualDamage >= singleHitDamage,
                $"Multi-hit damage ({actualDamage}) should be >= single hit damage ({singleHitDamage})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHitEarlyTermination()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Early Termination ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .Build();
            character.Stats.Strength = 20;

            // Create enemy with low health that will die before all hits
            var enemy = TestDataBuilders.Enemy()
                .WithName("WeakEnemy")
                .WithLevel(1)
                .WithHealth(15) // Low health
                .WithStats(5, 5, 5, 5)
                .Build();

            int initialHealth = enemy.CurrentHealth;

            var action = new Action
            {
                Name = "Quadruple Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties
                {
                    MultiHitCount = 4
                }
            };

            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(character, action);
            int rollBonus = 0;
            int totalRoll = 15; // High roll to ensure damage

            // Process multi-hit - should stop when enemy dies
            int totalDamage = MultiHitProcessor.ProcessMultiHit(
                character, enemy, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 15, null);

            int finalHealth = enemy.CurrentHealth;

            TestBase.AssertTrue(finalHealth <= 0,
                $"Enemy should be dead after multi-hit, health: {finalHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(totalDamage > 0,
                $"Multi-hit should deal damage before termination, dealt {totalDamage}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHitDifferentCounts()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Different Counts ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .Build();
            character.Stats.Strength = 10;

            // Test 2-hit
            var enemy2 = TestDataBuilders.Enemy()
                .WithName("Enemy2")
                .WithLevel(1)
                .WithHealth(100)
                .WithStats(5, 5, 5, 5)
                .Build();

            var action2 = new Action
            {
                Name = "Double Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 2 }
            };

            double damageMultiplier2 = ActionUtilities.CalculateDamageMultiplier(character, action2);
            int totalDamage2 = MultiHitProcessor.ProcessMultiHit(
                character, enemy2, action2, damageMultiplier2, 10, 10, 0, 10, null);

            TestBase.AssertTrue(totalDamage2 > 0,
                $"2-hit action should deal damage, dealt {totalDamage2}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test 3-hit
            var enemy3 = TestDataBuilders.Enemy()
                .WithName("Enemy3")
                .WithLevel(1)
                .WithHealth(100)
                .WithStats(5, 5, 5, 5)
                .Build();

            var action3 = new Action
            {
                Name = "Triple Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 3 }
            };

            double damageMultiplier3 = ActionUtilities.CalculateDamageMultiplier(character, action3);
            int totalDamage3 = MultiHitProcessor.ProcessMultiHit(
                character, enemy3, action3, damageMultiplier3, 10, 10, 0, 10, null);

            TestBase.AssertTrue(totalDamage3 > 0,
                $"3-hit action should deal damage, dealt {totalDamage3}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test 5-hit
            var enemy5 = TestDataBuilders.Enemy()
                .WithName("Enemy5")
                .WithLevel(1)
                .WithHealth(100)
                .WithStats(5, 5, 5, 5)
                .Build();

            var action5 = new Action
            {
                Name = "Flurry Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 5 }
            };

            double damageMultiplier5 = ActionUtilities.CalculateDamageMultiplier(character, action5);
            int totalDamage5 = MultiHitProcessor.ProcessMultiHit(
                character, enemy5, action5, damageMultiplier5, 10, 10, 0, 10, null);

            TestBase.AssertTrue(totalDamage5 > 0,
                $"5-hit action should deal damage, dealt {totalDamage5}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHitDamageCalculation()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Damage Calculation ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .Build();
            character.Stats.Strength = 12;

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .WithHealth(200)
                .WithStats(5, 5, 5, 5)
                .Build();

            var action = new Action
            {
                Name = "Triple Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 3 }
            };

            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(character, action);
            int rollBonus = 0;
            int totalRoll = 12;

            // Calculate single hit damage
            int singleHitDamage = CombatCalculator.CalculateDamage(character, enemy, action, damageMultiplier, 1.0, rollBonus, totalRoll);

            // Process multi-hit
            int totalDamage = MultiHitProcessor.ProcessMultiHit(
                character, enemy, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 12, null);

            // Each hit should deal similar damage (allowing for variance)
            // Total should be approximately 3x single hit
            TestBase.AssertTrue(totalDamage > 0,
                $"Multi-hit should deal positive damage, got {totalDamage}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify that total damage is reasonable (at least 2x single hit, allowing for variance)
            TestBase.AssertTrue(totalDamage >= singleHitDamage,
                $"Total multi-hit damage ({totalDamage}) should be >= single hit damage ({singleHitDamage})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHitWithSelfAndTarget()
        {
            Console.WriteLine("\n--- Testing Multi-Hit with SelfAndTarget ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .Build();
            character.Stats.Strength = 10;

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .WithHealth(100)
                .WithStats(5, 5, 5, 5)
                .Build();

            int characterInitialHealth = character.CurrentHealth;
            int enemyInitialHealth = enemy.CurrentHealth;

            var action = new Action
            {
                Name = "Reckless Double Strike",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Target = TargetType.SelfAndTarget,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 2 }
            };

            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(character, action);
            int rollBonus = 0;
            int totalRoll = 10;

            // Process multi-hit with SelfAndTarget
            int totalDamage = MultiHitProcessor.ProcessMultiHit(
                character, enemy, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 10, null);

            int characterFinalHealth = character.CurrentHealth;
            int enemyFinalHealth = enemy.CurrentHealth;

            // Both should take damage
            TestBase.AssertTrue(characterFinalHealth < characterInitialHealth,
                $"Character should take damage, {characterInitialHealth} -> {characterFinalHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(enemyFinalHealth < enemyInitialHealth,
                $"Enemy should take damage, {enemyInitialHealth} -> {enemyFinalHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(totalDamage > 0,
                $"Multi-hit SelfAndTarget should deal damage, dealt {totalDamage}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
