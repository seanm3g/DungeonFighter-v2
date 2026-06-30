using System;

using System.Collections.Generic;

using RPGGame;

using RPGGame.Actions.Execution;

using RPGGame.Combat.Events;

using RPGGame.Data;

using RPGGame.Tests;



namespace RPGGame.Tests.Unit

{

    public static class ActionEffectTargetTests

    {

        private static int _testsRun;

        private static int _testsPassed;

        private static int _testsFailed;



        public static void RunAllTests()

        {

            Console.WriteLine("=== Action Effect Target Tests ===\n");

            _testsRun = 0;

            _testsPassed = 0;

            _testsFailed = 0;



            TestHardenAppliesToAttackerNotEnemy();

            TestSelfTargetHealAppliesToHero();

            TestEnemyTargetHealAppliesToEnemy();

            TestLifestealHealsAttackerAfterDamage();

            TestSelfTargetBuffDoesNotDamageAttacker();

            TestLegacyAreaOfEffectMapsToEnvironment();

            TestSelfTargetSheetEffectRoutesToAttacker();



            TestBase.PrintSummary("Action Effect Target Tests", _testsRun, _testsPassed, _testsFailed);

        }



        private static void TestHardenAppliesToAttackerNotEnemy()

        {

            Console.WriteLine("--- Harden applies to attacker ---");



            var hero = TestDataBuilders.Character().WithName("Hero").Build();

            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            var action = TestDataBuilders.CreateMockAction("HARDEN");

            action.Target = TargetType.Self;

            action.CausesHarden = true;



            var results = new List<string>();

            var hitEvent = new CombatEvent(CombatEventType.ActionHit, hero)

            {

                Target = enemy,

                IsCritical = true

            };



            CombatEffectsSimplified.ApplyStatusEffects(action, hero, enemy, results, hitEvent);



            TestBase.AssertTrue(hero.HardenTurns > 0 || (hero.HardenStacks ?? 0) > 0,

                "Harden should apply to the attacker",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue((enemy.HardenStacks ?? 0) == 0 && enemy.HardenTurns == 0,

                "Harden should not apply to the enemy target",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }



        private static void TestSelfTargetHealAppliesToHero()

        {

            Console.WriteLine("\n--- Self-target heal applies to hero ---");



            var hero = TestDataBuilders.Character().WithName("Hero").Build();

            hero.CurrentHealth = 50;

            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            var action = TestDataBuilders.CreateMockAction("REST");

            action.Type = ActionType.Heal;

            action.Target = TargetType.Self;

            action.Advanced.HealAmount = 10;



            var lastUsed = new Dictionary<Actor, Action>();

            var lastCritMiss = new Dictionary<Actor, bool>();

            ActionSelector.SetStoredActionRoll(hero, 15);



            var result = ActionExecutionFlow.Execute(

                hero, enemy, null, null, action, null, lastUsed, lastCritMiss);



            TestBase.AssertTrue(result.Hit, "Self heal should hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hero.CurrentHealth > 50,

                "Self-target heal should restore hero HP",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }



        private static void TestEnemyTargetHealAppliesToEnemy()

        {

            Console.WriteLine("\n--- Enemy-target heal applies to enemy ---");



            var hero = TestDataBuilders.Character().WithName("Hero").Build();

            var enemy = TestDataBuilders.Enemy().WithName("Enemy").WithHealth(100).Build();

            enemy.CurrentHealth = 40;

            var action = TestDataBuilders.CreateMockAction("MEND FOE");

            action.Type = ActionType.Heal;

            action.Target = TargetType.SingleTarget;

            action.Advanced.HealAmount = 15;



            var lastUsed = new Dictionary<Actor, Action>();

            var lastCritMiss = new Dictionary<Actor, bool>();

            ActionSelector.SetStoredActionRoll(hero, 15);



            var result = ActionExecutionFlow.Execute(

                hero, enemy, null, null, action, null, lastUsed, lastCritMiss);



            TestBase.AssertTrue(result.Hit, "Enemy-target heal should hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(enemy.CurrentHealth > 40,

                "Enemy-target heal should restore enemy HP",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }



        private static void TestLifestealHealsAttackerAfterDamage()

        {

            Console.WriteLine("\n--- Lifesteal heals attacker after damage ---");



            var hero = TestDataBuilders.Character().WithName("Hero").Build();

            hero.CurrentHealth = 50;

            var enemy = TestDataBuilders.Enemy().WithName("Enemy").WithHealth(200).Build();

            var action = TestDataBuilders.CreateMockAction("DRAIN");

            action.Type = ActionType.Attack;

            action.Target = TargetType.SingleTarget;

            action.DamageMultiplier = 1.0;

            action.Advanced.LifestealPercent = 0.5;



            var lastUsed = new Dictionary<Actor, Action>();

            var lastCritMiss = new Dictionary<Actor, bool>();

            ActionSelector.SetStoredActionRoll(hero, 18);



            int heroHealthBefore = hero.CurrentHealth;

            var result = ActionExecutionFlow.Execute(

                hero, enemy, null, null, action, null, lastUsed, lastCritMiss);



            TestBase.AssertTrue(result.Hit, "Attack should hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(result.Damage > 0, "Attack should deal damage", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(hero.CurrentHealth > heroHealthBefore,

                "Lifesteal should heal attacker after dealing damage",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }



        private static void TestSelfTargetBuffDoesNotDamageAttacker()

        {

            Console.WriteLine("\n--- Self-target buff does not damage attacker ---");



            var hero = TestDataBuilders.Character().WithName("Hero").Build();

            hero.CurrentHealth = 100;

            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            var action = TestDataBuilders.CreateMockAction("HARDEN");

            action.Type = ActionType.Buff;

            action.Target = TargetType.Self;

            action.DamageMultiplier = 0;

            action.CausesHarden = true;



            var lastUsed = new Dictionary<Actor, Action>();

            var lastCritMiss = new Dictionary<Actor, bool>();

            ActionSelector.SetStoredActionRoll(hero, 16);



            var result = ActionExecutionFlow.Execute(

                hero, enemy, null, null, action, null, lastUsed, lastCritMiss);



            TestBase.AssertTrue(result.Hit, "Buff should hit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(100, hero.CurrentHealth,

                "Buff with zero damage should not reduce attacker HP",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(0, result.Damage,

                "Buff should not deal damage",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }



        private static void TestLegacyAreaOfEffectMapsToEnvironment()

        {

            Console.WriteLine("\n--- Legacy AreaOfEffect JSON maps to Environment ---");



            var data = new ActionData { Name = "BLAST", TargetType = "AreaOfEffect" };

            var action = ActionDataToActionMapper.CreateAction(data);



            TestBase.AssertTrue(action.Target == TargetType.Environment,

                "Legacy AreaOfEffect target type should map to Environment",

                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }



        private static void TestSelfTargetSheetEffectRoutesToAttacker()

        {

            Console.WriteLine("\n--- Self-target sheet effect list routes to attacker ---");



            var hero = TestDataBuilders.Character().WithName("Hero").Build();

            var enemy = TestDataBuilders.Enemy().WithName("Enemy").Build();

            var action = TestDataBuilders.CreateMockAction("GUARD");

            action.Advanced.SelfTargetEffects = new List<string> { "harden" };



            var results = new List<string>();

            var hitEvent = new CombatEvent(CombatEventType.ActionHit, hero)

            {

                Target = enemy,

                IsCritical = true

            };



            CombatEffectsSimplified.ApplyStatusEffects(action, hero, enemy, results, hitEvent);



            TestBase.AssertTrue(hero.HardenTurns > 0 || (hero.HardenStacks ?? 0) > 0,
                "Self-target harden should apply to attacker",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue((enemy.HardenStacks ?? 0) == 0 && enemy.HardenTurns == 0,
                "Self-target harden should not apply to enemy",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }

    }

}


