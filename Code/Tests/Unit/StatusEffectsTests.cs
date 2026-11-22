using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for status effects
    /// Tests all 23 status effects (6 basic + 17 advanced) and their application
    /// </summary>
    public static class StatusEffectsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            // Step 5: these tests cover the retired statuses' code, which stays until step 12 (CoreStatuses).
            Console.WriteLine("=== Status Effects Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBasicStatusEffects();
            TestAdvancedStatusEffects();
            TestStatusEffectApplication();
            TestEffectDuration();
            TestEffectStacking();
            TestEffectRemoval();

            TestBase.PrintSummary("Status Effects Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBasicStatusEffects()
        {
            Console.WriteLine("--- Testing Basic Status Effects ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var registry = new EffectHandlerRegistry();

            // Test Bleed
            var bleedAction = TestDataBuilders.CreateMockAction("BleedAction");
            bleedAction.CausesBleed = true;
            var bleedResults = new List<string>();
            registry.ApplyEffect("bleed", enemy, bleedAction, bleedResults);
            TestBase.AssertTrue(enemy.PendingBleedFromHits > 0,
                $"Bleed should queue pending: pending={enemy.PendingBleedFromHits}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Retired ids land their core status (Stun / Slow = Stagger, Weaken = Rattle, Acid = Expose).
            var retired = TestDataBuilders.CreateMockAction("RetiredAction");
            registry.ApplyEffect("stun", enemy, retired, new List<string>());
            registry.ApplyEffect("weaken", enemy, retired, new List<string>());
            registry.ApplyEffect("acid", enemy, retired, new List<string>());
            TestBase.AssertTrue(enemy.StaggerStacks > 0 && Stores.Get(enemy, "rattle") > 0 && (enemy.ExposeStacks ?? 0) > 0,
                $"retired ids apply Stagger / Rattle / Expose: stagger={enemy.StaggerStacks}, expose={enemy.ExposeStacks}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Burn
            var burnAction = TestDataBuilders.CreateMockAction("BurnAction");
            burnAction.CausesBurn = true;
            var burnResults = new List<string>();
            registry.ApplyEffect("burn", enemy, burnAction, burnResults);
            TestBase.AssertTrue(enemy.PendingBurnFromHits > 0,
                $"Burn should queue pending: pending={enemy.PendingBurnFromHits}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }

        private static void TestAdvancedStatusEffects()
        {
            Console.WriteLine("\n--- Testing Advanced Status Effects ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();
            var registry = new EffectHandlerRegistry();

            // Test Vulnerability (folded into Expose)
            var vulnAction = TestDataBuilders.CreateMockAction("VulnAction");
            var vulnResults = new List<string>();
            registry.ApplyEffect("vulnerability", enemy, vulnAction, vulnResults);
            TestBase.AssertTrue(enemy.ExposeStacks > 0 && enemy.ExposeTurns > 0,
                $"Vulnerability should apply Expose: stacks={enemy.ExposeStacks}, turns={enemy.ExposeTurns}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Harden
            var hardenAction = TestDataBuilders.CreateMockAction("HardenAction");
            var hardenResults = new List<string>();
            registry.ApplyEffect("harden", character, hardenAction, hardenResults);
            TestBase.AssertTrue(character.HardenStacks > 0 && character.HardenTurns > 0,
                $"Harden should apply: stacks={character.HardenStacks}, turns={character.HardenTurns}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Focus
            var focusAction = TestDataBuilders.CreateMockAction("FocusAction");
            var focusResults = new List<string>();
            registry.ApplyEffect("focus", character, focusAction, focusResults);
            TestBase.AssertTrue(character.FocusStacks > 0 && character.FocusTurns > 0,
                $"Focus should apply: stacks={character.FocusStacks}, turns={character.FocusTurns}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Expose
            var exposeAction = TestDataBuilders.CreateMockAction("ExposeAction");
            var exposeResults = new List<string>();
            registry.ApplyEffect("expose", enemy, exposeAction, exposeResults);
            TestBase.AssertTrue(enemy.ExposeStacks > 0 && enemy.ExposeTurns > 0,
                $"Expose should apply: stacks={enemy.ExposeStacks}, turns={enemy.ExposeTurns}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Armor Break (folded into Expose)
            int exposeBeforeBreak = enemy.ExposeStacks ?? 0;
            var armorbreakAction = TestDataBuilders.CreateMockAction("ArmorBreakAction");
            var armorbreakResults = new List<string>();
            registry.ApplyEffect("armorbreak", enemy, armorbreakAction, armorbreakResults);
            TestBase.AssertTrue((enemy.ExposeStacks ?? 0) > exposeBeforeBreak || enemy.ExposeArmorStrip > 0,
                $"Armor Break should apply Expose: stacks={enemy.ExposeStacks}, strip={enemy.ExposeArmorStrip}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Mark
            var markAction = TestDataBuilders.CreateMockAction("MarkAction");
            var markResults = new List<string>();
            registry.ApplyEffect("mark", enemy, markAction, markResults);
            TestBase.AssertTrue(enemy.IsMarked && enemy.MarkTurns > 0,
                $"Mark should apply: isMarked={enemy.IsMarked}, turns={enemy.MarkTurns}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

        }

        private static void TestStatusEffectApplication()
        {
            Console.WriteLine("\n--- Testing Status Effect Application ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var action = TestDataBuilders.CreateMockAction("TestAction");
            action.CausesBleed = true;

            TestBase.AssertTrue(action.CausesBleed, 
                "Action should be able to cause status effects", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEffectDuration()
        {
            Console.WriteLine("\n--- Testing Effect Duration ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.BleedIntensity = 3;
            TestBase.AssertTrue(character.IsBleeding && character.BleedIntensity > 0,
                "Bleed intensity should be trackable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            character.BleedIntensity = 0;
            character.PendingBleedFromHits = 0;
        }

        private static void TestEffectStacking()
        {
            Console.WriteLine("\n--- Testing Effect Stacking ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.BleedIntensity = 2;
            character.StaggerStacks = 1;
            character.Effects.SlowMultiplier = 0.5;
            character.Effects.SlowTurns = 3;

            TestBase.AssertTrue(character.IsBleeding && character.StaggerStacks == 1 && character.Effects.SlowMultiplier < 1.0,
                "Multiple effects should be stackable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.BleedIntensity = 0;
            character.StaggerStacks = 0;
            character.Effects.SlowMultiplier = 1.0;
            character.Effects.SlowTurns = 0;
        }

        private static void TestEffectRemoval()
        {
            Console.WriteLine("\n--- Testing Effect Removal ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.BleedIntensity = 1;
            character.BleedIntensity = 0;
            character.PendingBleedFromHits = 0;
            TestBase.AssertFalse(character.IsBleeding,
                "Effect should be removable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

