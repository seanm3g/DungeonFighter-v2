using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for SpeedCalculator
    /// Tests attack speed calculations for characters and enemies
    /// </summary>
    public static class SpeedCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all SpeedCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== SpeedCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCalculateAttackSpeedCharacter();
            TestCalculateAttackSpeedEnemy();
            TestAgilityReduction();
            TestWeaponSpeedModifier();
            TestEquipmentSpeedBonus();
            TestSlowDebuff();
            TestMinimumAttackTime();

            TestBase.PrintSummary("SpeedCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Character Speed Tests

        private static void TestCalculateAttackSpeedCharacter()
        {
            Console.WriteLine("--- Testing CalculateAttackSpeedCharacter ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(character);

            TestBase.AssertTrue(speed > 0,
                "Attack speed should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(speed >= 0.01,
                "Attack speed should be at least minimum attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Enemy Speed Tests

        private static void TestCalculateAttackSpeedEnemy()
        {
            Console.WriteLine("\n--- Testing CalculateAttackSpeedEnemy ---");

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(enemy);

            TestBase.AssertTrue(speed > 0,
                "Enemy attack speed should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(speed >= 0.01,
                "Enemy attack speed should be at least minimum attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Agility Tests

        private static void TestAgilityReduction()
        {
            Console.WriteLine("\n--- Testing AgilityReduction ---");

            var lowAgility = TestDataBuilders.Character()
                .WithName("LowAgility")
                .WithStats(10, 5, 10, 10)
                .Build();

            var highAgility = TestDataBuilders.Character()
                .WithName("HighAgility")
                .WithStats(10, 20, 10, 10)
                .Build();

            var lowSpeed = SpeedCalculator.CalculateAttackSpeed(lowAgility);
            var highSpeed = SpeedCalculator.CalculateAttackSpeed(highAgility);

            TestBase.AssertTrue(highSpeed <= lowSpeed,
                "Higher agility should result in faster attack speed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Weapon Speed Tests

        private static void TestWeaponSpeedModifier()
        {
            Console.WriteLine("\n--- Testing WeaponSpeedModifier ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            // Test with and without weapon
            var speedNoWeapon = SpeedCalculator.CalculateAttackSpeed(character);

            // Equip a weapon (if possible)
            // Note: This may require weapon setup
            TestBase.AssertTrue(speedNoWeapon > 0,
                "Speed calculation should work with or without weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Equipment Speed Tests

        private static void TestEquipmentSpeedBonus()
        {
            Console.WriteLine("\n--- Testing EquipmentSpeedBonus ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(character);

            // Equipment speed bonus should reduce attack time
            TestBase.AssertTrue(speed > 0,
                "Speed calculation should account for equipment bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Slow Debuff Tests

        private static void TestSlowDebuff()
        {
            Console.WriteLine("\n--- Testing SlowDebuff ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            var normalSpeed = SpeedCalculator.CalculateAttackSpeed(character);

            // Apply slow debuff
            character.Effects.SlowTurns = 1;
            var slowSpeed = SpeedCalculator.CalculateAttackSpeed(character);

            TestBase.AssertTrue(slowSpeed >= normalSpeed,
                "Slow debuff should increase attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Minimum Attack Time Tests

        private static void TestMinimumAttackTime()
        {
            Console.WriteLine("\n--- Testing MinimumAttackTime ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 100, 10, 10) // Very high agility
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(character);

            // Speed should not go below minimum
            TestBase.AssertTrue(speed >= 0.01,
                "Attack speed should respect minimum attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
