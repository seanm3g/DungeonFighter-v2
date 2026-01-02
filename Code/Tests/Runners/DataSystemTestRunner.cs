using System;
using RPGGame;
using RPGGame.Tests.Unit.Data;
using RPGGame.Tests.Unit.Data.Validation;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Data system tests
    /// </summary>
    public static class DataSystemTestRunner
    {
        /// <summary>
        /// Runs all Data system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  DATA SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            ActionLoaderTests.RunAllTests();
            Console.WriteLine();
            JsonLoaderTests.RunAllTests();
            Console.WriteLine();
            LootGeneratorTests.RunAllTests();
            Console.WriteLine();
            ItemGeneratorTests.RunAllTests();
            Console.WriteLine();
            LootBonusApplierTests.RunAllTests();
            Console.WriteLine();
            LootDataCacheTests.RunAllTests();
            Console.WriteLine();
            EnemyLoaderTests.RunAllTests();
            Console.WriteLine();
            RoomLoaderTests.RunAllTests();
            Console.WriteLine();
            ColorConfigurationLoaderTests.RunAllTests();
            Console.WriteLine();
            ActionDescriptionEnhancerTests.RunAllTests();
            Console.WriteLine();
            LootTierCalculatorTests.RunAllTests();
            Console.WriteLine();
            LootContextTests.RunAllTests();
            Console.WriteLine();
            LootRarityProcessorTests.RunAllTests();
            Console.WriteLine();
            GameDataValidatorTests.RunAllTests();
            Console.WriteLine();
            ActionDataValidatorTests.RunAllTests();
        }
    }
}
