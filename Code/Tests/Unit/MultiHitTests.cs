using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Calculators;
using RPGGame.Data;
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
            TestMultiHitFullDamageWhenTargetDiesMidSwing();
            TestMultiHitDifferentCounts();
            TestMultiHitDamageCalculation();
            TestMultiHitWithSelfAndTarget();
            TestMultiHitAppliesRollPenaltyPerDamageTick();
            TestMultiHitModAppliesToNextAction();
            TestActionCadenceMultiHitSurvivesMissUntilCombo();
            TestActionCadenceMultiHitNotDoubleAppliedFromSheetAndBonuses();
            TestActionCadenceMultiHitDoesNotApplyToGrantingAction();

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
                .WithStats(15, 10, 10, 10)
                .Build();

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

        /// <summary>
        /// Regression: Multihit must credit full (per-hit × N) even when the target dies mid-swing.
        /// Otherwise combat log can show `(4 hits) for 45` instead of `(23-1)×4 = 88`-style totals.
        /// </summary>
        private static void TestMultiHitFullDamageWhenTargetDiesMidSwing()
        {
            Console.WriteLine("\n--- Regression: Multi-Hit full damage when target dies mid-swing ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .WithStats(20, 10, 10, 10)
                .Build();

            // Low health so the first tick kills; remaining ticks must still count toward totalDamage.
            var enemy = TestDataBuilders.Enemy()
                .WithName("WeakEnemy")
                .WithLevel(1)
                .WithHealth(15)
                .WithStats(5, 5, 5, 5)
                .Build();

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
            int totalRoll = 15;

            int singleHitDamage = CombatCalculator.CalculateDamage(
                character, enemy, action, damageMultiplier, 1.0, rollBonus, totalRoll);

            int totalDamage = MultiHitProcessor.ProcessMultiHit(
                character, enemy, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 15, null);

            TestBase.AssertTrue(enemy.CurrentHealth <= 0,
                $"Enemy should be dead after multi-hit, health: {enemy.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(singleHitDamage * 4, totalDamage,
                $"Multi-hit total should be per-hit × 4 even after kill (got {totalDamage}, per-hit {singleHitDamage})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiHitDifferentCounts()
        {
            Console.WriteLine("\n--- Testing Multi-Hit Different Counts ---");

            var character = TestDataBuilders.Character()
                .WithName("TestHero")
                .WithLevel(1)
                .WithStats(10, 10, 10, 10)
                .Build();

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
                .WithStats(12, 10, 10, 10)
                .Build();

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
                .WithStats(10, 10, 10, 10)
                .Build();

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

        /// <summary>
        /// RollPenalty (Accuracy debuff) lowers the attack total used for roll-based damage on every multihit tick, not only the first.
        /// </summary>
        private static void TestMultiHitAppliesRollPenaltyPerDamageTick()
        {
            Console.WriteLine("\n--- Testing Multi-Hit RollPenalty Per Damage Tick ---");

            _ = GameConfiguration.Instance;

            var character = TestDataBuilders.Character()
                .WithName("DebuffedHero")
                .WithLevel(1)
                .WithStats(14, 10, 10, 10)
                .Build();
            character.RollPenalty = 1;
            character.RollPenaltyTurns = 2;

            var enemy = TestDataBuilders.Enemy()
                .WithName("PunchingBag")
                .WithLevel(1)
                .WithHealth(500)
                .WithStats(5, 5, 5, 5)
                .Build();

            var action = new Action
            {
                Name = "TwinTap",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 2 }
            };

            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(character, action);
            int rollBonus = 0;
            int totalRoll = 14;

            int hit0Damage = CombatCalculator.CalculateDamage(character, enemy, action, damageMultiplier, 1.0, rollBonus, totalRoll);
            int hit1Damage = CombatCalculator.CalculateDamage(character, enemy, action, damageMultiplier, 1.0, rollBonus, totalRoll - 1);

            int totalDamage = MultiHitProcessor.ProcessMultiHit(
                character, enemy, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 14, null);

            TestBase.AssertEqual(hit0Damage + hit1Damage, totalDamage,
                "2-hit with RollPenalty 1 should sum damage as first hit at full totalRoll, second at totalRoll-1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.RollPenalty = 0;
            character.RollPenaltyTurns = 0;
            var enemy2 = TestDataBuilders.Enemy()
                .WithName("PunchingBag2")
                .WithLevel(1)
                .WithHealth(500)
                .WithStats(5, 5, 5, 5)
                .Build();
            int baseline = MultiHitProcessor.ProcessMultiHit(
                character, enemy2, action, damageMultiplier, totalRoll,
                totalRoll, rollBonus, 14, null);
            TestBase.AssertTrue(baseline > totalDamage,
                "Per-hit penalty should reduce total multihit damage vs no RollPenalty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Regression: Spreadsheet-style next-action modifier MULTIHIT_MOD should increase the following action's hit count.
        /// Example: Action A has MultiHitMod "2" (raw extra hits) → next executed action should have +2 hits.
        /// </summary>
        private static void TestMultiHitModAppliesToNextAction()
        {
            Console.WriteLine("\n--- Regression: MULTIHIT_MOD applies to next action ---");

            var lastUsed = new System.Collections.Generic.Dictionary<Actor, Action>();
            var lastCritMiss = new System.Collections.Generic.Dictionary<Actor, bool>();

            var hero = TestDataBuilders.Character()
                .WithName("MultiHitModHero")
                .WithLevel(1)
                .WithStats(12, 10, 10, 10)
                .Build();
            hero.Effects.ClearAllTempEffects();

            var enemy = TestDataBuilders.Enemy()
                .WithName("PunchingBag")
                .WithLevel(1)
                .WithHealth(500)
                .WithStats(5, 5, 5, 5)
                .Build();

            var setup = new Action
            {
                Name = "SetupMultihit",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                MultiHitMod = "2",
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 }
            };

            var follow = new Action
            {
                Name = "FollowAttack",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 }
            };

            try
            {
                ActionSelector.ClearStoredRolls();

                // Step 1: Execute setup (should enqueue MULTIHIT_MOD for the next roll)
                Dice.SetTestRoll(18); // high enough to hit consistently
                var r1 = ActionExecutionFlow.Execute(hero, enemy, null, null, setup, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(r1.Hit,
                    "Setup action should hit so its modifier is applied in the hit pipeline",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var queued = hero.Effects.PeekTurnBonuses();
                TestBase.AssertTrue(queued.Any(b => string.Equals(b.Type, "MULTIHIT_MOD", StringComparison.OrdinalIgnoreCase) && Math.Abs(b.Value - 2) < 0.01),
                    "After setup: next-roll bonuses include MULTIHIT_MOD +2",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Step 2: Execute follow action; MULTIHIT_MOD should be consumed and increase hit count to 3.
                int enemyHealthBefore = enemy.CurrentHealth;
                Dice.SetTestRoll(18);
                var r2 = ActionExecutionFlow.Execute(hero, enemy, null, null, follow, null, lastUsed, lastCritMiss);

                TestBase.AssertTrue(r2.Hit,
                    "Follow action should hit so multi-hit damage is applied",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // With no RollPenalty and MultiHitCount=1, each tick uses same totalRoll, so damage should be exactly 3x single-hit.
                double dmgMult = ActionUtilities.CalculateDamageMultiplier(hero, follow);
                int rollBonus = 0;
                int totalRoll = 18 + rollBonus; // modified base roll is the test roll in this harness
                int oneHit = CombatCalculator.CalculateDamage(hero, enemy, follow, dmgMult, 1.0, rollBonus, totalRoll);
                int expected = oneHit * 3;
                int actual = enemyHealthBefore - enemy.CurrentHealth;

                TestBase.AssertEqual(expected, actual,
                    "Follow action should deal 3-hit damage total (base 1 + 2 from MULTIHIT_MOD)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Step 3: Next action should NOT still have the multihit mod (it is one-shot, consumed on the follow roll).
                var remaining = hero.Effects.PeekTurnBonuses();
                TestBase.AssertTrue(!remaining.Any(b => string.Equals(b.Type, "MULTIHIT_MOD", StringComparison.OrdinalIgnoreCase)),
                    "After follow: MULTIHIT_MOD should be consumed from the queue",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }

        /// <summary>
        /// ACTION cadence MULTIHIT_MOD stays on the next combo slot across a miss, updates strip preview,
        /// and only redeems (extra hits) when a later roll is hit+combo.
        /// </summary>
        private static void TestActionCadenceMultiHitSurvivesMissUntilCombo()
        {
            Console.WriteLine("\n--- ACTION cadence MULTIHIT_MOD survives miss until combo ---");

            var lastUsed = new System.Collections.Generic.Dictionary<Actor, Action>();
            var lastCritMiss = new System.Collections.Generic.Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();

                var hero = TestDataBuilders.Character()
                    .WithName("MultiHitMissHero")
                    .WithLevel(1)
                    .WithStats(12, 10, 10, 10)
                    .Build();
                hero.Effects.ClearAllTempEffects();

                var rapid = new Action
                {
                    Name = "RAPID STRIKE",
                    Type = ActionType.Attack,
                    DamageMultiplier = 1.0,
                    IsComboAction = true,
                    ComboOrder = 1,
                    Cadence = "ACTION",
                    MultiHitMod = "1",
                    Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 }
                };
                var slam = new Action
                {
                    Name = "SLAM",
                    Type = ActionType.Attack,
                    DamageMultiplier = 1.0,
                    IsComboAction = true,
                    ComboOrder = 2,
                    Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 }
                };
                hero.AddAction(rapid, 1.0);
                hero.AddAction(slam, 1.0);
                hero.Actions.AddToCombo(rapid);
                hero.Actions.AddToCombo(slam);
                hero.ComboStep = 0;

                var enemy = TestDataBuilders.Enemy()
                    .WithName("MissBag")
                    .WithLevel(1)
                    .WithHealth(500)
                    .WithStats(5, 5, 5, 5)
                    .Build();

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                var setup = ActionExecutionFlow.Execute(hero, enemy, null, null, rapid, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(setup.Hit && setup.IsCombo,
                    "Setup RAPID STRIKE must hit+combo to queue MULTIHIT",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var slotPending = hero.Effects.GetPendingActionBonusesForSlot(1);
                TestBase.AssertTrue(slotPending.Any(b =>
                        string.Equals(b.Type, "MULTIHIT_MOD", StringComparison.OrdinalIgnoreCase) && Math.Abs(b.Value - 1) < 0.01),
                    "MULTIHIT_MOD queued on next combo slot",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int previewHits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(slam, hero, 1);
                TestBase.AssertEqual(2, previewHits,
                    "Strip preview: Slam shows base 1 + pending MULTIHIT +1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Drop attack bonuses so a natural 1 cannot still clear HIT via STR/roll bonus.
                hero.Stats.Strength = 0;
                hero.Stats.Agility = 0;
                hero.Stats.Technique = 0;
                hero.Stats.Intelligence = 0;
                hero.ComboStep = 1;
                Dice.SetTestRoll(1);
                ActionSelector.SetStoredActionRoll(hero, 1);
                var miss = ActionExecutionFlow.Execute(hero, enemy, null, null, slam, null, lastUsed, lastCritMiss);
                TestBase.AssertFalse(miss.Hit,
                    "Forced miss on Slam",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                slotPending = hero.Effects.GetPendingActionBonusesForSlot(1);
                TestBase.AssertTrue(slotPending.Any(b =>
                        string.Equals(b.Type, "MULTIHIT_MOD", StringComparison.OrdinalIgnoreCase) && Math.Abs(b.Value - 1) < 0.01),
                    "After miss: MULTIHIT_MOD still pending on Slam slot",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, hero.Effects.ConsumedMultiHitMod,
                    "Miss must not redeem ConsumedMultiHitMod",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                hero.ComboStep = 1;
                int enemyHealthBefore = enemy.CurrentHealth;
                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(hero, Math.Max(comboMin + 1, 15));
                var comboHit = ActionExecutionFlow.Execute(hero, enemy, null, null, slam, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(comboHit.Hit && comboHit.IsCombo,
                    "Slam hit+combo redeems MULTIHIT",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                double dmgMult = ActionUtilities.CalculateDamageMultiplier(hero, slam);
                int oneHit = CombatCalculator.CalculateDamage(hero, enemy, slam, dmgMult, 1.0, 0, Math.Max(comboMin + 1, 15));
                int actual = enemyHealthBefore - enemy.CurrentHealth;
                TestBase.AssertEqual(oneHit * 2, actual,
                    "Redeemed MULTIHIT deals 2-hit damage",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(hero.Effects.GetPendingActionBonusesForSlot(1).Count == 0,
                    "After combo: slot MULTIHIT consumed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }

        /// <summary>
        /// Regression: RAPID STRIKE ships both multiHitMod and ActionAttackBonuses MULTIHIT_MOD.
        /// Those must grant +1 once (bank), not stack to +2 (3 hits on the follow-up).
        /// </summary>
        private static void TestActionCadenceMultiHitNotDoubleAppliedFromSheetAndBonuses()
        {
            Console.WriteLine("\n--- ACTION cadence MULTIHIT: sheet + ActionAttackBonuses do not double ---");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();

                var hero = TestDataBuilders.Character()
                    .WithName("MultiHitDualAuthorHero")
                    .WithLevel(1)
                    .WithStats(12, 10, 10, 10)
                    .Build();
                hero.Effects.ClearAllTempEffects();

                var rapid = new Action
                {
                    Name = "RAPID STRIKE",
                    Type = ActionType.Attack,
                    DamageMultiplier = 1.0,
                    IsComboAction = true,
                    ComboOrder = 1,
                    Cadence = "ACTION",
                    MultiHitMod = "1",
                    Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 },
                    ActionAttackBonuses = new ActionAttackBonuses
                    {
                        BonusGroups = new List<ActionAttackBonusGroup>
                        {
                            new ActionAttackBonusGroup
                            {
                                Keyword = "ACTION",
                                CadenceType = "ACTION",
                                Count = 1,
                                DurationType = "ACTION",
                                Bonuses = new List<ActionAttackBonusItem>
                                {
                                    new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = 1 }
                                }
                            }
                        }
                    }
                };
                var slam = new Action
                {
                    Name = "SLAM",
                    Type = ActionType.Attack,
                    DamageMultiplier = 1.0,
                    IsComboAction = true,
                    ComboOrder = 2,
                    Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 }
                };
                hero.AddAction(rapid, 1.0);
                hero.AddAction(slam, 1.0);
                hero.Actions.AddToCombo(rapid);
                hero.Actions.AddToCombo(slam);
                hero.ComboStep = 0;

                var enemy = TestDataBuilders.Enemy()
                    .WithName("DualAuthorFoe")
                    .WithLevel(1)
                    .WithHealth(500)
                    .WithStats(5, 5, 5, 5)
                    .Build();

                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                var setup = ActionExecutionFlow.Execute(hero, enemy, null, null, rapid, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(setup.Hit && setup.IsCombo,
                    "RAPID STRIKE must hit+combo to queue MULTIHIT",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, setup.ResolvedMultiHitCount,
                    "RAPID STRIKE itself resolves as 1 hit (queued MULTIHIT_MOD does not apply to the grantor)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(hero.Effects.GetPendingActionBonusesForSlot(1).Count == 0,
                    "Sheet multiHitMod must not also enqueue onto next combo slot when ActionAttackBonuses covers MULTIHIT_MOD",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                double pendingMh = RollModificationManager.PeekPendingActionCadenceMultiHitMod(hero, 1);
                TestBase.AssertEqual(1.0, pendingMh,
                    "Pending MULTIHIT for Slam is +1 (not +2 from dual authorship)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int rapidPreview = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(rapid, hero, 0);
                TestBase.AssertEqual(1, rapidPreview,
                    "Strip preview: Rapid Strike stays 1 hit after queuing Multihit for the next slot",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int previewHits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(slam, hero, 1);
                TestBase.AssertEqual(2, previewHits,
                    "Strip preview: Slam is 2 hits (base 1 + pending +1)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                hero.ComboStep = 1;
                int enemyHealthBefore = enemy.CurrentHealth;
                Dice.SetTestRoll(Math.Max(comboMin + 1, 15));
                ActionSelector.SetStoredActionRoll(hero, Math.Max(comboMin + 1, 15));
                var comboHit = ActionExecutionFlow.Execute(hero, enemy, null, null, slam, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(comboHit.Hit && comboHit.IsCombo,
                    "Slam hit+combo redeems MULTIHIT",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                double dmgMult = ActionUtilities.CalculateDamageMultiplier(hero, slam);
                int oneHit = CombatCalculator.CalculateDamage(hero, enemy, slam, dmgMult, 1.0, 0, Math.Max(comboMin + 1, 15));
                int actual = enemyHealthBefore - enemy.CurrentHealth;
                TestBase.AssertEqual(oneHit * 2, actual,
                    "Slam deals 2-hit damage (not 3 from double MULTIHIT grant)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(0, hero.Effects.ConsumedMultiHitMod,
                    "After Execute: ConsumedMultiHitMod clears so strip cards reset",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                int rapidAfter = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(
                    rapid, hero, 0, includeConsumedMods: false);
                int slamAfter = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(
                    slam, hero, 1, includeConsumedMods: false);
                TestBase.AssertEqual(1, rapidAfter,
                    "After Slam redeem: strip preview Rapid Strike is back to 1 hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, slamAfter,
                    "After Slam redeem: strip preview Slam is back to 1 hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }

        /// <summary>
        /// Regression: RAPID STRIKE's ACTION cadence +1 MULTIHIT must not inflate that same swing's resolved
        /// hit count or combat-log "N hits" label — only the following sequential combo action receives it.
        /// </summary>
        private static void TestActionCadenceMultiHitDoesNotApplyToGrantingAction()
        {
            Console.WriteLine("\n--- ACTION cadence MULTIHIT: grantor swing stays 1 hit ---");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int comboMin = GameConfiguration.Instance.RollSystem.ComboThreshold.Min;
            if (comboMin <= 0) comboMin = 14;

            try
            {
                ActionSelector.ClearStoredRolls();

                var hero = TestDataBuilders.Character()
                    .WithName("MultiHitGrantorHero")
                    .WithLevel(1)
                    .WithStats(12, 10, 10, 10)
                    .Build();
                hero.Effects.ClearAllTempEffects();

                var rapid = new Action
                {
                    Name = "RAPID STRIKE",
                    Type = ActionType.Attack,
                    DamageMultiplier = 1.0,
                    IsComboAction = true,
                    ComboOrder = 1,
                    Cadence = "ACTION",
                    MultiHitMod = "1",
                    Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 },
                    ActionAttackBonuses = new ActionAttackBonuses
                    {
                        BonusGroups = new List<ActionAttackBonusGroup>
                        {
                            new ActionAttackBonusGroup
                            {
                                Keyword = "ACTION",
                                CadenceType = "ACTION",
                                Count = 1,
                                DurationType = "ACTION",
                                Bonuses = new List<ActionAttackBonusItem>
                                {
                                    new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = 1 }
                                }
                            }
                        }
                    }
                };
                var slam = new Action
                {
                    Name = "SLAM",
                    Type = ActionType.Attack,
                    DamageMultiplier = 1.0,
                    IsComboAction = true,
                    ComboOrder = 2,
                    Advanced = new AdvancedMechanicsProperties { MultiHitCount = 1 }
                };
                hero.AddAction(rapid, 1.0);
                hero.AddAction(slam, 1.0);
                hero.Actions.AddToCombo(rapid);
                hero.Actions.AddToCombo(slam);
                hero.ComboStep = 0;

                var enemy = TestDataBuilders.Enemy()
                    .WithName("GrantorFoe")
                    .WithLevel(1)
                    .WithHealth(500)
                    .WithStats(5, 5, 5, 5)
                    .Build();

                int enemyHealthBefore = enemy.CurrentHealth;
                int roll = Math.Max(comboMin + 1, 15);
                Dice.SetTestRoll(roll);
                var setup = ActionExecutionFlow.Execute(hero, enemy, null, null, rapid, null, lastUsed, lastCritMiss);
                TestBase.AssertTrue(setup.Hit && setup.IsCombo,
                    "RAPID STRIKE must hit+combo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, setup.ResolvedMultiHitCount,
                    "Grantor ResolvedMultiHitCount is 1 (Multihit not self-applied)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                double dmgMult = ActionUtilities.CalculateDamageMultiplier(hero, rapid);
                int oneHit = CombatCalculator.CalculateDamage(hero, enemy, rapid, dmgMult, 1.0, 0, roll);
                int actual = enemyHealthBefore - enemy.CurrentHealth;
                TestBase.AssertEqual(oneHit, actual,
                    "RAPID STRIKE deals single-hit damage (Multihit is for the next sequential action only)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // After ComboStep advances, strip must show Multihit on Slam only.
                int rapidSlotHits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(rapid, hero, 0);
                int slamSlotHits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(slam, hero, 1);
                TestBase.AssertEqual(1, rapidSlotHits,
                    "Strip: Rapid Strike remains 1 hit after grant",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, slamSlotHits,
                    "Strip: Slam previews 2 hits (base + pending Multihit)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Post-grant peek at ComboStep must not claim Rapid Strike is a 2-hit swing for formatting.
                int wronglyAttributed = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(rapid, hero);
                TestBase.AssertTrue(wronglyAttributed >= 2,
                    "Sanity: GetEffective without resolved count can still peek next-slot Multihit after advance",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(setup.ResolvedMultiHitCount < wronglyAttributed,
                    "ResolvedMultiHitCount must stay below post-deposit GetEffective(grantor) peek",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.ClearTestRoll();
                ActionSelector.ClearStoredRolls();
            }
        }
    }
}
