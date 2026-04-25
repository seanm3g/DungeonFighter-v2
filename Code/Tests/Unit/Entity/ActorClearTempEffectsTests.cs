using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Ensures <see cref="Actor.ClearAllTempEffects"/> resets all combat-carried status fields
    /// (including advanced stack effects) and that <see cref="CharacterFacade.ClearAllTempEffects"/> matches.
    /// </summary>
    public static class ActorClearTempEffectsTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActorClearTempEffects Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCharacterClearAllTempEffects_ClearsAdvancedActorFields();
            TestCharacterFacadeClearAllTempEffects_MatchesCharacterPath();

            Console.WriteLine($"\nActorClearTempEffects: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed.\n");
        }

        private static void TestCharacterClearAllTempEffects_ClearsAdvancedActorFields()
        {
            var c = new Character("ClearTest", 1);
            c.PoisonPercentOfMaxHealth = 3;
            c.BurnIntensity = 2;
            c.IsStunned = true;
            c.StunTurnsRemaining = 2;
            c.HasCriticalMissPenalty = true;
            c.CriticalMissPenaltyTurns = 1;
            c.VulnerabilityStacks = 2;
            c.VulnerabilityTurns = 3;
            c.TemporaryHP = 10;
            c.TemporaryHPTurns = 2;
            c.IsConfused = true;
            c.ConfusionTurns = 2;
            c.ConfusionChance = 0.25;
            c.Effects.ApplySlow(0.5, 2);

            c.ClearAllTempEffects();

            TestBase.AssertTrue(c.PoisonPercentOfMaxHealth == 0, "Poison cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, c.BurnIntensity, "Burn cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(c.IsStunned, "Stun cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(c.HasCriticalMissPenalty, "Crit miss penalty cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNull(c.VulnerabilityStacks, "Vulnerability stacks cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, c.VulnerabilityTurns, "Vulnerability turns cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNull(c.TemporaryHP, "Temp HP cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(c.IsConfused, "Confusion cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(c.ConfusionChance == 0, "Confusion chance cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, c.Effects.SlowMultiplier, "Slow cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, c.Effects.SlowTurns, "Slow turns cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCharacterFacadeClearAllTempEffects_MatchesCharacterPath()
        {
            var c = new Character("FacadeClear", 1);
            c.HardenStacks = 1;
            c.HardenTurns = 2;
            c.Facade.ClearAllTempEffects();
            TestBase.AssertNull(c.HardenStacks, "Facade clear removes harden stacks", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, c.HardenTurns, "Facade clear removes harden turns", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
