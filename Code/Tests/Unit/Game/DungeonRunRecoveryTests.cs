using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Full heal + cleanup when leaving a dungeon early or after a successful clear.
    /// </summary>
    public static class DungeonRunRecoveryTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonRunRecovery Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRestore_HealsDamagedPlayerToFull();
            TestRestore_NullPlayer_ReturnsZero();
            TestRestore_AlreadyFull_ReturnsZero();

            TestBase.PrintSummary("DungeonRunRecovery Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRestore_HealsDamagedPlayerToFull()
        {
            Console.WriteLine("--- RestoreAfterLeavingDungeon heals to full ---");

            var player = new Character("RecoveryHero", 3);
            int max = player.GetEffectiveMaxHealth();
            player.CurrentHealth = Math.Max(1, max / 4);
            player.IsStunned = true;

            int restored = DungeonRunRecovery.RestoreAfterLeavingDungeon(player);

            TestBase.AssertTrue(restored > 0,
                $"Should report restored HP (got {restored})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(max, player.CurrentHealth,
                "Player should be at full effective max HP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!player.IsStunned,
                "Temp combat effects should be cleared",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRestore_NullPlayer_ReturnsZero()
        {
            Console.WriteLine("\n--- RestoreAfterLeavingDungeon null player ---");

            int restored = DungeonRunRecovery.RestoreAfterLeavingDungeon(null);
            TestBase.AssertEqual(0, restored,
                "Null player should restore 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRestore_AlreadyFull_ReturnsZero()
        {
            Console.WriteLine("\n--- RestoreAfterLeavingDungeon already full ---");

            var player = new Character("FullHero", 2);
            player.CurrentHealth = player.GetEffectiveMaxHealth();

            int restored = DungeonRunRecovery.RestoreAfterLeavingDungeon(player);
            TestBase.AssertEqual(0, restored,
                "Already-full player should restore 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(player.GetEffectiveMaxHealth(), player.CurrentHealth,
                "Already-full player should stay full",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
