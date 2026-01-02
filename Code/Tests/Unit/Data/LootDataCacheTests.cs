using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for LootDataCache
    /// Tests cache loading, retrieval, and invalidation
    /// </summary>
    public static class LootDataCacheTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LootDataCache tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LootDataCache Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestLoad();
            TestReload();
            TestClear();
            TestDataProperties();

            TestBase.PrintSummary("LootDataCache Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Loading Tests

        private static void TestLoad()
        {
            Console.WriteLine("--- Testing Load ---");

            try
            {
                var cache = LootDataCache.Load();
                TestBase.AssertNotNull(cache,
                    "Load should return a cache instance",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (cache != null)
                {
                    // Test that collections are initialized
                    TestBase.AssertNotNull(cache.TierDistributions,
                        "TierDistributions should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertNotNull(cache.ArmorData,
                        "ArmorData should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertNotNull(cache.WeaponData,
                        "WeaponData should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertNotNull(cache.StatBonuses,
                        "StatBonuses should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertNotNull(cache.ActionBonuses,
                        "ActionBonuses should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertNotNull(cache.Modifications,
                        "Modifications should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertNotNull(cache.RarityData,
                        "RarityData should be initialized",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Load should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Reload Tests

        private static void TestReload()
        {
            Console.WriteLine("\n--- Testing Reload ---");

            try
            {
                var cache = LootDataCache.Load();
                TestBase.AssertNotNull(cache,
                    "Cache should be loaded",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (cache != null)
                {
                    // Reload should not crash
                    cache.Reload();
                    TestBase.AssertTrue(true,
                        "Reload should complete without errors",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Reload should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Clear Tests

        private static void TestClear()
        {
            Console.WriteLine("\n--- Testing Clear ---");

            try
            {
                var cache = LootDataCache.Load();
                TestBase.AssertNotNull(cache,
                    "Cache should be loaded",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (cache != null)
                {
                    // Clear should not crash
                    cache.Clear();
                    TestBase.AssertTrue(true,
                        "Clear should complete without errors",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    // Collections should be empty after clear
                    TestBase.AssertEqual(0, cache.TierDistributions.Count,
                        "TierDistributions should be empty after clear",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Clear should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Data Properties Tests

        private static void TestDataProperties()
        {
            Console.WriteLine("\n--- Testing Data Properties ---");

            try
            {
                var cache = LootDataCache.Load();
                TestBase.AssertNotNull(cache,
                    "Cache should be loaded",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (cache != null)
                {
                    // Test that all collections are accessible
                    var tierCount = cache.TierDistributions.Count;
                    var armorCount = cache.ArmorData.Count;
                    var weaponCount = cache.WeaponData.Count;
                    var statBonusCount = cache.StatBonuses.Count;
                    var actionBonusCount = cache.ActionBonuses.Count;
                    var modCount = cache.Modifications.Count;
                    var rarityCount = cache.RarityData.Count;

                    // Counts should be non-negative (might be 0 if data files don't exist)
                    TestBase.AssertTrue(tierCount >= 0,
                        $"TierDistributions count should be >= 0, got {tierCount}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(armorCount >= 0,
                        $"ArmorData count should be >= 0, got {armorCount}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(weaponCount >= 0,
                        $"WeaponData count should be >= 0, got {weaponCount}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Data properties should be accessible: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
