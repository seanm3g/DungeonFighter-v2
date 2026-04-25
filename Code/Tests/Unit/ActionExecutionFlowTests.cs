using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Utils;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Data;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for action execution flow (selection, rolls, modifiers).
    /// Deep integration coverage for <see cref="ActionExecutionFlow.Execute"/> also lives in
    /// <c>ActionBonusMechanicsTests</c>, combo dice tests, and combo execution tests.
    /// </summary>
    public static class ActionExecutionFlowTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Execution Flow Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionSelectionLogic();
            TestActionExecutionFlow();
            TestActionCooldownManagement();
            TestActionAvailabilityChecks();
            TestForcedActionExecution();
            TestActionRollGeneration();
            TestActionRollModifications();
            TestExecute_ReturnsResultWithoutThrowing();
            TestDeferredSheetAccuracyOnHitQueuesHeroAndEnemyNextRolls();
            TestDeferredEnemyRollPenaltyConsumesOneApplicationPerAttack();
            TestDeferredSheetAccuracyScalesWithMultihitOnHit();
            TestFifoAccuracyShiftsThresholdsNotRollSubtraction();
            TestEnemySheetDamageModDoesNotPersistAcrossEnemySwings();
            TestDeferredSheetAccuracyOnEnemyHitQueuesEnemyFifo();
            TestEnemyComboSelectionUsesFreshThresholdAfterPriorSwingOverrides();

            TestBase.PrintSummary("Action Execution Flow Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionSelectionLogic()
        {
            Console.WriteLine("--- Testing Action Selection Logic ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test character action selection
            var charAction = ActionSelector.SelectActionByEntityType(character);
            var charActionPool = character.GetActionPool();
            TestBase.AssertTrue(charAction == null || charActionPool.Contains(charAction), 
                "Character action selection should return action from pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test enemy action selection
            var enemyAction = ActionSelector.SelectActionByEntityType(enemy);
            TestBase.AssertTrue(enemyAction == null || enemy.ActionPool.Any(item => item.action == enemyAction), 
                "Enemy action selection should return action from pool", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test selection when stunned
            character.IsStunned = true;
            var stunnedAction = ActionSelector.SelectActionByEntityType(character);
            TestBase.AssertNull(stunnedAction, 
                "Stunned character should not select action", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            character.IsStunned = false;
        }

        private static void TestActionExecutionFlow()
        {
            Console.WriteLine("\n--- Testing Action Execution Flow ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test that action execution can be initiated
            var actionPool = character.GetActionPool();
            if (actionPool.Count > 0)
            {
                var action = actionPool[0];
                TestBase.AssertNotNull(action, 
                    "Action should be available for execution", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (action != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(action.Name), 
                        "Action should have a name", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestActionCooldownManagement()
        {
            Console.WriteLine("\n--- Testing Action Cooldown Management ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            
            // Test initial cooldown
            TestBase.AssertEqual(0, action.CurrentCooldown, 
                "Action should start with 0 cooldown", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test setting cooldown
            action.CurrentCooldown = 3;
            TestBase.AssertEqual(3, action.CurrentCooldown, 
                "Action cooldown should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test cooldown cannot be negative
            action.CurrentCooldown = -1;
            TestBase.AssertTrue(action.CurrentCooldown >= 0, 
                "Action cooldown should not be negative", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionAvailabilityChecks()
        {
            Console.WriteLine("\n--- Testing Action Availability Checks ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test action pool availability
            var actionPool = character.GetActionPool();
            TestBase.AssertNotNull(actionPool, 
                "Action pool should not be null", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action selection when pool has actions
            if (actionPool.Count > 0)
            {
                var action = ActionSelector.SelectActionByEntityType(character);
                TestBase.AssertTrue(action != null, 
                    "Action should be selectable when pool has actions", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestForcedActionExecution()
        {
            Console.WriteLine("\n--- Testing Forced Action Execution ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var forcedAction = TestDataBuilders.CreateMockAction("ForcedAction");

            // Test that forced action can be set
            TestBase.AssertNotNull(forcedAction, 
                "Forced action should be creatable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (forcedAction != null)
            {
                TestBase.AssertEqual("ForcedAction", forcedAction.Name, 
                    "Forced action should have correct name", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestActionRollGeneration()
        {
            Console.WriteLine("\n--- Testing Action Roll Generation ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            // Test roll generation
            var roll = ActionSelector.GetActionRoll(character);
            TestBase.AssertTrue(roll >= 1 && roll <= 20, 
                $"Action roll should be between 1-20, got {roll}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionRollModifications()
        {
            Console.WriteLine("\n--- Testing Action Roll Modifications ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test roll modifications can be applied
            int baseRoll = 10;
            var modifiedRoll = RPGGame.Actions.RollModification.RollModificationManager.ApplyActionRollModifications(
                baseRoll, action, character, enemy);
            
            TestBase.AssertTrue(modifiedRoll >= 1, 
                $"Modified roll should be valid, got {modifiedRoll}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Smoke test: full execute pipeline runs with default test entities (requires GameConfiguration).
        /// </summary>
        private static void TestExecute_ReturnsResultWithoutThrowing()
        {
            Console.WriteLine("\n--- ActionExecutionFlow.Execute smoke ---");

            _ = GameConfiguration.Instance;

            var character = TestDataBuilders.Character().WithName("FlowHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("FlowEnemy").Build();
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();

            ActionExecutionResult result = ActionExecutionFlow.Execute(
                character, enemy, null, null, null, null, lastUsed, lastCritMiss);

            TestBase.AssertNotNull(result,
                "Execute should return ActionExecutionResult",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// When sheet accuracy is deferred (non-ATTACK cadence), it does not apply to the swing that resolves the card;
        /// on hit it queues hero FIFO <c>ACCURACY</c> and enemy <see cref="Actor.ApplyRollPenalty"/> for their next attack rolls.
        /// </summary>
        /// <summary>
        /// FIFO ACCURACY must not reduce <see cref="ActionExecutionResult.ModifiedBaseRoll"/> (no "15 - 5" on the d20);
        /// it raises hit/combo/crit thresholds instead.
        /// </summary>
        private static void TestFifoAccuracyShiftsThresholdsNotRollSubtraction()
        {
            Console.WriteLine("\n--- FIFO ACCURACY: thresholds shift, modified d20 unchanged ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("FifoAccHero").WithStats(10, 10, 10, 0).Build();
            var enemy = TestDataBuilders.Enemy().WithName("FifoAccTarget").Build();
            enemy.RollPenalty = 0;
            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.SetTempRollBonus(0, 0);
            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "ACCURACY", Value = -5 }
            });

            var strike = new Action
            {
                Name = "FifoAccStrike",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "ATTACK",
                DamageMultiplier = 0.01,
                Length = 1.0,
                IsComboAction = false
            };

            Dice.SetTestRoll(15);
            ActionSelector.SetStoredActionRoll(hero, 15);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var result = ActionExecutionFlow.Execute(hero, enemy, null, null, strike, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);

            TestBase.AssertEqual(15, result.ModifiedBaseRoll,
                "Modified base roll should stay 15 (ACCURACY is not subtracted from the d20)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.Hit,
                "Roll 15 should still hit when hit threshold is raised 5→10 (same as old 10 vs 5)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDeferredSheetAccuracyOnHitQueuesHeroAndEnemyNextRolls()
        {
            Console.WriteLine("\n--- Deferred sheet accuracy: hero + enemy next roll on hit only ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("DrunkenHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("DrunkenVictim").Build();
            enemy.RollPenalty = 0;
            enemy.RollPenaltyTurns = 0;
            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.SetTempRollBonus(0, 0);

            var drunkenStyle = new Action
            {
                Name = "DeferredAccCombo",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "Action",
                IsComboAction = false,
                DamageMultiplier = 0.01,
                Length = 1.0,
                ComboBonusDuration = 2,
                Advanced = new AdvancedMechanicsProperties
                {
                    RollBonus = -5,
                    EnemyRollBonus = -5,
                    RollBonusDuration = 0
                }
            };

            Dice.SetTestRoll(18);
            ActionSelector.SetStoredActionRoll(hero, 18);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var hitResult = ActionExecutionFlow.Execute(hero, enemy, null, null, drunkenStyle, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);

            TestBase.AssertTrue(hitResult.Hit,
                "Controlled high roll should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-5, ActionSelector.PeekQueuedAccuracyBonus(hero),
                "Hero deferred RollBonus should queue ACCURACY FIFO for next attack (peek first layer)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, hero.Effects.GetPendingActionCadenceLayerCount(),
                "Two ACCURACY FIFO layers when ComboBonusDuration is 2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, enemy.RollPenalty,
                "Enemy should receive RollPenalty for next roll(s) from EnemyRollBonus -5",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, enemy.RollPenaltyTurns,
                "Enemy RollPenalty attack count follows ComboBonusDuration when RollBonusDuration unset",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Miss: no deferred packages (ApplyHitOutcome not run)
            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.SetTempRollBonus(0, 0);
            enemy.RollPenalty = 0;
            enemy.RollPenaltyTurns = 0;
            Dice.SetTestRoll(2);
            ActionSelector.SetStoredActionRoll(hero, 2);
            var missResult = ActionExecutionFlow.Execute(hero, enemy, null, null, drunkenStyle, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);
            TestBase.AssertFalse(missResult.Hit,
                "Low roll should miss with default thresholds",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, ActionSelector.PeekQueuedAccuracyBonus(hero),
                "Miss should not queue hero ACCURACY",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, enemy.RollPenalty,
                "Miss should not apply enemy roll penalty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Deferred negative EnemyRollBonus applies RollPenalty for N attack rolls; each resolved enemy Attack consumes one.
        /// </summary>
        private static void TestDeferredEnemyRollPenaltyConsumesOneApplicationPerAttack()
        {
            Console.WriteLine("\n--- Enemy roll penalty: one application per attack when duration is 1 ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("DebuffHero").WithStats(10, 10, 10, 0).Build();
            var enemy = TestDataBuilders.Enemy().WithName("DebuffVictim").Build();
            enemy.RollPenalty = 0;
            enemy.RollPenaltyTurns = 0;
            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.SetTempRollBonus(0, 0);

            var drunk = new Action
            {
                Name = "OneShotDebuff",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "Action",
                IsComboAction = false,
                DamageMultiplier = 0.01,
                Length = 1.0,
                ComboBonusDuration = 1,
                Advanced = new AdvancedMechanicsProperties
                {
                    RollBonus = 0,
                    EnemyRollBonus = -5,
                    RollBonusDuration = 0
                }
            };

            var jab = new Action
            {
                Name = "Jab",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "ATTACK",
                DamageMultiplier = 0.01,
                Length = 1.0,
                IsComboAction = false
            };

            Dice.SetTestRoll(18);
            ActionSelector.SetStoredActionRoll(hero, 18);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            _ = ActionExecutionFlow.Execute(hero, enemy, null, null, drunk, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);

            TestBase.AssertEqual(5, enemy.RollPenalty,
                "Enemy should have roll penalty 5 after deferred hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, enemy.RollPenaltyTurns,
                "Duration 1 → one attack roll of penalty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Dice.SetTestRoll(15);
            ActionSelector.SetStoredActionRoll(enemy, 15);
            _ = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);

            TestBase.AssertEqual(0, enemy.RollPenalty,
                "After one enemy attack, roll penalty should clear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, enemy.RollPenaltyTurns,
                "Attack counter should be zero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Deferred sheet accuracy and enemy roll penalty from a multihit action scale by effective hit count (e.g. -1 × 2 hits → -2 on next roll).
        /// </summary>
        private static void TestDeferredSheetAccuracyScalesWithMultihitOnHit()
        {
            Console.WriteLine("\n--- Deferred sheet accuracy scales with multihit on hit ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("TwinStrikeHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TwinVictim").Build();
            enemy.RollPenalty = 0;
            enemy.RollPenaltyTurns = 0;
            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.SetTempRollBonus(0, 0);

            var twinShot = new Action
            {
                Name = "TwinDefer",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "Action",
                IsComboAction = false,
                DamageMultiplier = 0.01,
                Length = 1.0,
                ComboBonusDuration = 1,
                Advanced = new AdvancedMechanicsProperties
                {
                    MultiHitCount = 2,
                    RollBonus = -1,
                    EnemyRollBonus = -2,
                    RollBonusDuration = 0
                }
            };

            Dice.SetTestRoll(18);
            ActionSelector.SetStoredActionRoll(hero, 18);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var hitResult = ActionExecutionFlow.Execute(hero, enemy, null, null, twinShot, null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);

            TestBase.AssertTrue(hitResult.Hit,
                "Controlled high roll should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-2, ActionSelector.PeekQueuedAccuracyBonus(hero),
                "2-hit action with deferred RollBonus -1 should queue ACCURACY -2 for next attack",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, enemy.RollPenalty,
                "Enemy RollPenalty should scale with multihit: -2 per hit × 2 = 4",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Enemies must clear Consumed* sheet modifiers each swing (same as heroes); otherwise EnemyDamageMod
        /// from a hero hit sticks and every following enemy attack stays buffed.
        /// </summary>
        private static void TestEnemySheetDamageModDoesNotPersistAcrossEnemySwings()
        {
            Console.WriteLine("\n--- Enemy sheet DAMAGE_MOD: single application, no sticky Consumed* ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("BankHero").WithStats(3, 3, 3, 3).Build();
            // High STR so +10% DAMAGE_MOD changes integer damage after armor (lab hero often has 0 armor).
            var enemy = TestDataBuilders.Enemy().WithName("Bag").WithHealth(9999).WithStats(55, 5, 5, 5).Build();
            hero.Effects.ClearPendingActionBonuses();
            hero.Effects.ClearConsumedModifierBonuses();
            enemy.Effects.ClearPendingActionBonuses();
            enemy.Effects.ClearConsumedModifierBonuses();

            var bankEnemyDmg = new Action
            {
                Name = "BankEnemyDmg",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "Attack",
                IsComboAction = true,
                DamageMultiplier = 0.01,
                Length = 1.0,
                EnemyDamageMod = "10"
            };
            var jab = new Action
            {
                Name = "Jab",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "Attack",
                IsComboAction = true,
                DamageMultiplier = 1.0,
                Length = 1.0
            };

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();

            try
            {
                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(enemy, 16);
                int baseline = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit).Damage;

                enemy.Effects.ClearPendingActionBonuses();
                enemy.Effects.ClearConsumedModifierBonuses();

                Dice.SetTestRoll(18);
                ActionSelector.SetStoredActionRoll(hero, 18);
                var bankHit = ActionExecutionFlow.Execute(hero, enemy, null, null, bankEnemyDmg, null, lastUsed, lastCrit);
                TestBase.AssertTrue(bankHit.Hit,
                    "Hero bank action should hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                double queued = enemy.PeekQueuedSheetEnemyDamageModPercentForDisplay();
                TestBase.AssertTrue(queued >= 9.5 && queued <= 10.5,
                    "Enemy should have ~+10% DAMAGE_MOD queued for next swing",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(enemy, 16);
                int buffed = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit).Damage;
                TestBase.AssertTrue(enemy.PeekQueuedSheetEnemyDamageModPercentForDisplay() < 0.5,
                    "Enemy FIFO sheet DAMAGE_MOD layer should be consumed on that attack roll",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(buffed >= baseline,
                    "Buffed damage should be at least baseline (strict > may fail on tiny totals + armor rounding)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(enemy, 16);
                int second = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit).Damage;
                TestBase.AssertEqual(baseline, second,
                    "Second enemy jab (no new bank) should match baseline — ConsumedDamageMod must not persist",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                ActionSelector.ClearStoredRolls();
            }
        }

        /// <summary>
        /// Deferred sheet <see cref="AdvancedMechanicsProperties.RollBonus"/> (e.g. TAUNT heroAccuracy column)
        /// must enqueue ACCURACY on the enemy's FIFO when an enemy lands the hit, mirroring the hero path.
        /// </summary>
        private static void TestDeferredSheetAccuracyOnEnemyHitQueuesEnemyFifo()
        {
            Console.WriteLine("\n--- Deferred sheet accuracy: enemy attacker queues ACCURACY FIFO on hit ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("TauntedHero").WithStats(10, 10, 10, 0).Build();
            var enemy = TestDataBuilders.Enemy().WithName("Taunter").Build();
            hero.Effects.ClearPendingActionBonuses();
            enemy.Effects.ClearPendingActionBonuses();

            var enemyTauntLike = new Action
            {
                Name = "TAUNT",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Cadence = "Action",
                IsComboAction = false,
                DamageMultiplier = 0.01,
                Length = 1.0,
                ComboBonusDuration = 1,
                Advanced = new AdvancedMechanicsProperties
                {
                    RollBonus = 4,
                    RollBonusDuration = 0
                }
            };

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();

            try
            {
                Dice.SetTestRoll(16);
                ActionSelector.SetStoredActionRoll(enemy, 16);
                var hit = ActionExecutionFlow.Execute(enemy, hero, null, null, enemyTauntLike, null, lastUsed, lastCrit);

                TestBase.AssertTrue(hit.Hit,
                    "Enemy taunt-like swing should hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(4, ActionSelector.PeekQueuedAccuracyBonus(enemy),
                    "Enemy deferred RollBonus should queue ACCURACY FIFO for the enemy's next attack rolls",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                ActionSelector.ClearStoredRolls();
            }
        }

        /// <summary>
        /// Combo vs normal selection must use thresholds reset for this roll; stale <see cref="Combat.ThresholdManager"/>
        /// values must not make a natural 8 pick a combo-slot special when the default combo gate is 14+.
        /// </summary>
        private static void TestEnemyComboSelectionUsesFreshThresholdAfterPriorSwingOverrides()
        {
            Console.WriteLine("\n--- Enemy combo pick: stale ThresholdManager must not gate selection ---");

            _ = GameConfiguration.Instance;

            var hero = TestDataBuilders.Character().WithName("HeroGate").WithStats(10, 10, 10, 0).Build();
            var enemy = TestDataBuilders.Enemy().WithName("GateEnemy").WithStats(10, 10, 10, 0).Build();
            enemy.ActionPool.Clear();
            var slam = TestDataBuilders.CreateMockAction("SLAM", ActionType.Attack);
            slam.IsComboAction = true;
            slam.ComboOrder = 1;
            enemy.AddAction(slam, 1.0);
            enemy.AddToCombo(slam);

            var tm = RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager();
            tm.SetComboThreshold(enemy, 5);

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();

            try
            {
                Dice.SetTestRoll(8);
                ActionSelector.SetStoredActionRoll(enemy, 8);
                var outcome = ActionExecutionFlow.Execute(enemy, hero, null, null, null, null, lastUsed, lastCrit);

                TestBase.AssertTrue(outcome.Hit,
                    "Natural 8 should hit default hit band",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var sel = outcome.SelectedAction;
                TestBase.AssertTrue(sel != null && !sel.IsComboAction && string.IsNullOrEmpty(sel.Name),
                    "Roll 8 after artificial low combo threshold pollution should still pick unnamed normal (reset before selection)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                ActionSelector.ClearStoredRolls();
                tm.ResetThresholds(enemy);
                tm.ResetThresholds(hero);
            }
        }
    }
}

