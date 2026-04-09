using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for CombatEffectsSimplified — application and turn processing of status effects.
    /// </summary>
    public static class CombatEffectsSimplifiedTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatEffectsSimplified tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatEffectsSimplified Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyStatusEffects_PoisonFromAction_RegistersOnTarget();
            TestProcessStatusEffects_NoPoisonOrBurn_ReturnsZeroTotalDamage();
            TestCanEntityAct_NotStunned_ReturnsTrue();
            TestCanEntityAct_Stunned_ReturnsFalse();
            TestCalculateEffectDuration_RespectsMinimumOneTurn();

            TestBase.PrintSummary("CombatEffectsSimplified Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestApplyStatusEffects_PoisonFromAction_RegistersOnTarget()
        {
            Console.WriteLine("--- ApplyStatusEffects: poison action applies via registry ---");

            var attacker = TestDataBuilders.Character().WithName("AllyAttacker").Build();
            var target = TestDataBuilders.Enemy().WithName("PoisonTarget").Build();
            var action = TestDataBuilders.CreateMockAction("VenomStrike", ActionType.Attack);
            action.CausesPoison = true;

            var results = new List<string>();
            bool applied = CombatEffectsSimplified.ApplyStatusEffects(action, attacker, target, results);

            TestBase.AssertTrue(target.PoisonStacks > 0 || applied,
                "Poison-from-action path should apply stacks or set effectsApplied",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestProcessStatusEffects_NoPoisonOrBurn_ReturnsZeroTotalDamage()
        {
            Console.WriteLine("--- ProcessStatusEffects: clean actor yields no damage ---");

            var actor = TestDataBuilders.Character().WithName("CleanActor").Build();
            actor.PoisonStacks = 0;
            actor.BurnStacks = 0;

            var results = new List<string>();
            int total = CombatEffectsSimplified.ProcessStatusEffects(actor, results);

            TestBase.AssertEqual(0, total,
                "No poison/burn stacks should produce zero processed damage total",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCanEntityAct_NotStunned_ReturnsTrue()
        {
            Console.WriteLine("--- CanEntityAct: unstunned actor can act ---");

            var actor = TestDataBuilders.Character().WithName("Ready").Build();
            actor.IsStunned = false;
            var results = new List<string>();

            bool can = CombatEffectsSimplified.CanEntityAct(actor, results);

            TestBase.AssertTrue(can, "Unstunned actor should be able to act",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCanEntityAct_Stunned_ReturnsFalse()
        {
            Console.WriteLine("--- CanEntityAct: stunned actor cannot act ---");

            var actor = TestDataBuilders.Character().WithName("StunnedHero").Build();
            actor.IsStunned = true;
            var results = new List<string>();

            bool can = CombatEffectsSimplified.CanEntityAct(actor, results);

            TestBase.AssertTrue(!can, "Stunned actor should not act",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(results.Count > 0, "Stun path should add a message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateEffectDuration_RespectsMinimumOneTurn()
        {
            Console.WriteLine("--- CalculateEffectDuration: minimum 1 turn ---");

            var attacker = TestDataBuilders.Character().WithName("Caster").Build();
            var target = TestDataBuilders.Enemy().WithName("Victim").Build();

            int d = CombatEffectsSimplified.CalculateEffectDuration(1, attacker, target);

            TestBase.AssertTrue(d >= 1, $"Duration should be at least 1, got {d}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
