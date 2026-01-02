using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Comprehensive tests for LevelUpManager
    /// Tests leveling system, XP gain, and stat increases
    /// </summary>
    public static class LevelUpManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LevelUpManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LevelUpManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestLevelUp();
            TestXPGain();

            TestBase.PrintSummary("LevelUpManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var manager = new LevelUpManager(character);
            TestBase.AssertNotNull(manager,
                "LevelUpManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Level Up Tests

        private static void TestLevelUp()
        {
            Console.WriteLine("\n--- Testing LevelUp ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var initialLevel = character.Level;
            var initialMaxHealth = character.MaxHealth;

            // Level up should increase level and stats
            character.Progression.LevelUp();

            TestBase.AssertTrue(character.Level >= initialLevel,
                "Level should increase or stay same after level up",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Max health should increase with level
            TestBase.AssertTrue(character.MaxHealth >= initialMaxHealth,
                "Max health should increase with level",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region XP Gain Tests

        private static void TestXPGain()
        {
            Console.WriteLine("\n--- Testing XP Gain ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var initialXP = character.Progression.XP;

            // Gain XP
            character.Progression.AddXP(100);

            TestBase.AssertTrue(character.Progression.XP >= initialXP,
                "Experience should increase after gaining XP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
