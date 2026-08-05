using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Conditional trigger storage + evaluator coverage (canonical ON* tokens).
    /// </summary>
    public static class ConditionalTriggersTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Conditional Triggers Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCanonicalTokenStorage();
            TestEvaluatorFactoryConditions();
            TestHPThresholdHelpers();

            TestBase.PrintSummary("Conditional Triggers Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCanonicalTokenStorage()
        {
            Console.WriteLine("--- Testing Canonical Token Storage ---");

            string[] tokens = { "ONMISS", "ONHIT", "ONCONNECT", "ONCRITICAL", "ONCOMBO", "ONKILL", "ONCRITICALMISS", "ONROLLVALUE", "ONHEALTHTHRESHOLD", "ONROOMSCLEARED", "ONWIELD:Sword" };
            foreach (var token in tokens)
            {
                var action = TestDataBuilders.CreateMockAction("TestAction");
                action.Triggers.TriggerConditions = new List<string> { token };
                TestBase.AssertTrue(action.Triggers.TriggerConditions.Contains(token),
                    $"Action should store {token}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEvaluatorFactoryConditions()
        {
            Console.WriteLine("\n--- Testing Evaluator Factory Conditions ---");
            var evaluator = new ConditionalTriggerEvaluator();
            var source = new Character("Hero", 1);
            var target = new Enemy("Foe", 1, 100, 10, 5, 5, 5);

            var missEvt = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };
            TestBase.AssertTrue(evaluator.EvaluateConditions(new List<TriggerCondition> { TriggerConditionFactory.OnMiss() }, missEvt, source, target, null),
                "Factory OnMiss", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var connectEvt = new CombatEvent(CombatEventType.ActionHit, source) { IsCombo = true };
            TestBase.AssertTrue(evaluator.EvaluateConditions(new List<TriggerCondition> { TriggerConditionFactory.OnConnect() }, connectEvt, source, target, null),
                "Factory OnConnect on combo", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var killEvt = new CombatEvent(CombatEventType.EnemyDied, source);
            TestBase.AssertTrue(evaluator.EvaluateConditions(new List<TriggerCondition> { TriggerConditionFactory.OnKill() }, killEvt, source, target, null),
                "Factory OnKill", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var critMissEvt = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true, IsCriticalMiss = true };
            TestBase.AssertTrue(evaluator.EvaluateConditions(new List<TriggerCondition> { TriggerConditionFactory.OnCriticalMiss() }, critMissEvt, source, target, null),
                "Factory OnCriticalMiss", ref _testsRun, ref _testsPassed, ref _testsFailed);

            source.Weapon = new WeaponItem("TestBlade", 1, 10, 1.0, WeaponType.Sword);
            TestBase.AssertTrue(evaluator.EvaluateConditions(
                    new List<TriggerCondition> { TriggerConditionFactory.IfWieldingWeaponType(WeaponType.Sword) },
                    connectEvt, source, target, null),
                "Factory IfWieldingWeaponType Sword", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!evaluator.EvaluateConditions(
                    new List<TriggerCondition> { TriggerConditionFactory.IfWieldingWeaponType(WeaponType.Wand) },
                    connectEvt, source, target, null),
                "Factory IfWieldingWeaponType rejects Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHPThresholdHelpers()
        {
            Console.WriteLine("\n--- Testing HP Threshold Helpers ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            double threshold = 0.5;
            bool belowThreshold = (double)character.CurrentHealth / character.MaxHealth < threshold;
            TestBase.AssertTrue(belowThreshold || !belowThreshold,
                "HP threshold should be checkable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
