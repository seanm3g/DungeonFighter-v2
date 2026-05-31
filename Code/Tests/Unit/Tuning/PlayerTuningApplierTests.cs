using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class PlayerTuningApplierTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== PlayerTuningApplier Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestComputeMaxHealthFromTuning_UsesPlayerBaseHealthAtLevel1();
            TestApplyToCurrentPlayer_UpdatesMaxHealthAfterTuningChange();

            TestBase.PrintSummary("PlayerTuningApplier Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestComputeMaxHealthFromTuning_UsesPlayerBaseHealthAtLevel1()
        {
            Console.WriteLine("--- ComputeMaxHealthFromTuning uses PlayerBaseHealth at level 1 ---");
            var cfg = GameConfiguration.Instance;
            int savedBase = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 100;
                var character = new Character("TestHero", level: 1);
                int computed = PlayerTuningApplier.ComputeMaxHealthFromTuning(character);
                TestBase.AssertEqual(100, computed, "level-1 max health follows tuning base", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = savedBase;
            }
        }

        private static void TestApplyToCurrentPlayer_UpdatesMaxHealthAfterTuningChange()
        {
            Console.WriteLine("--- ApplyToCurrentPlayer updates max health after tuning change ---");
            var cfg = GameConfiguration.Instance;
            int savedBase = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 60;
                var character = new Character("TestHero", level: 1);
                TestBase.AssertEqual(60, character.MaxHealth, "initial max health is 60", ref _testsRun, ref _testsPassed, ref _testsFailed);

                cfg.Character.PlayerBaseHealth = 100;
                PlayerTuningApplier.ApplyToCurrentPlayer(character);
                TestBase.AssertEqual(100, character.MaxHealth, "max health updated to 100 after apply", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(100, character.CurrentHealth, "current health healed to new max", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = savedBase;
            }
        }
    }
}
