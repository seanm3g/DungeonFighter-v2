using System;
using System.Collections.Generic;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Enemy archetype combat multipliers: fast archetypes swing in less time, and
    /// <c>enemySystem.archetypeCombat</c> overrides the default attack time / damage.
    /// </summary>
    public static class ArchetypeCombatTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ArchetypeCombat Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFastArchetypesHaveShorterAttackTime();
            TestPatchOverridesDamageAndAttackTime();

            Console.WriteLine($"\nArchetypeCombat: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed.\n");
        }

        private static void TestFastArchetypesHaveShorterAttackTime()
        {
            var saved = GameConfiguration.Instance.EnemySystem.ArchetypeCombat;
            try
            {
                GameConfiguration.Instance.EnemySystem.ArchetypeCombat = new Dictionary<string, ArchetypeCombatConfig>(StringComparer.OrdinalIgnoreCase);
                foreach (var fast in new[] { EnemyArchetype.Assassin, EnemyArchetype.Acrobat, EnemyArchetype.Trickster, EnemyArchetype.Duelist })
                {
                    double t = ArchetypeManager.GetArchetypeAttackTimeMultiplier(ArchetypeManager.GetArchetypeProfile(fast));
                    TestBase.AssertTrue(t < 1.0, $"{fast} attacks faster than baseline (attack time {t:F2} < 1)",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                double berserker = ArchetypeManager.GetArchetypeDamageMultiplier(ArchetypeManager.GetArchetypeProfile(EnemyArchetype.Berserker));
                TestBase.AssertEqual(1.2, berserker, "Berserker default damage multiplier is 1.2",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameConfiguration.Instance.EnemySystem.ArchetypeCombat = saved;
            }
        }

        private static void TestPatchOverridesDamageAndAttackTime()
        {
            var saved = GameConfiguration.Instance.EnemySystem.ArchetypeCombat;
            try
            {
                GameConfiguration.Instance.EnemySystem.ArchetypeCombat = new Dictionary<string, ArchetypeCombatConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sage"] = new ArchetypeCombatConfig { Damage = 1.6 },
                    ["Assassin"] = new ArchetypeCombatConfig { AttackTime = 0.5 }
                };
                var sage = ArchetypeManager.GetArchetypeProfile(EnemyArchetype.Sage);
                TestBase.AssertEqual(1.6, ArchetypeManager.GetArchetypeDamageMultiplier(sage), "Override sets Sage damage (case-insensitive key)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.0, ArchetypeManager.GetArchetypeAttackTimeMultiplier(sage), "Sage attack time keeps its default when not overridden",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var assassin = ArchetypeManager.GetArchetypeProfile(EnemyArchetype.Assassin);
                TestBase.AssertEqual(0.5, ArchetypeManager.GetArchetypeAttackTimeMultiplier(assassin), "Override sets Assassin attack time",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.0, ArchetypeManager.GetArchetypeDamageMultiplier(assassin), "Assassin damage keeps its default when not overridden",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                GameConfiguration.Instance.EnemySystem.ArchetypeCombat = saved;
            }
        }
    }
}
