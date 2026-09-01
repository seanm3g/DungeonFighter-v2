using System;
using System.Linq;
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
            TestAffixCatalogDistribution();

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

        #region Affix catalog distribution

        /// <summary>
        /// Guards against sheet/settings overwrites that flood Modifications.json with one prefix
        /// (e.g. every row becoming Caustic) and against empty suffix catalogs.
        /// </summary>
        private static void TestAffixCatalogDistribution()
        {
            Console.WriteLine("\n--- Testing Affix Catalog Distribution ---");
            TestBase.SetCurrentTestName(nameof(TestAffixCatalogDistribution));

            try
            {
                var cache = LootDataCache.Load();
                TestBase.AssertNotNull(cache, "cache loaded", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (cache == null)
                    return;

                TestBase.AssertTrue(cache.Modifications.Count >= 10,
                    $"Modifications catalog should have variety (got {cache.Modifications.Count})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var modNameGroups = cache.Modifications
                    .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                    .GroupBy(m => m.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                int maxDup = modNameGroups.Count > 0 ? modNameGroups[0].Count : 0;
                string topName = modNameGroups.Count > 0 ? modNameGroups[0].Name : "(none)";
                TestBase.AssertTrue(maxDup <= 2,
                    $"No modification name should dominate the catalog (top '{topName}' x{maxDup})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var uncommonAdjectives = cache.Modifications
                    .Where(m => m.GetPrefixCategory() == ModificationPrefixCategory.Adjective)
                    .Where(m => string.Equals(
                        string.IsNullOrWhiteSpace(m.ItemRank) ? "Common" : m.ItemRank.Trim(),
                        "Uncommon",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                TestBase.AssertTrue(uncommonAdjectives.Count >= 3,
                    $"Uncommon adjective pool should share several names (got {uncommonAdjectives.Count}: {string.Join(", ", uncommonAdjectives)})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(cache.StatBonuses.Count >= 20,
                    $"StatBonuses catalog should have variety (got {cache.StatBonuses.Count})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var suffixNameGroups = cache.StatBonuses
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.Count())
                    .DefaultIfEmpty(0)
                    .Max();

                TestBase.AssertTrue(suffixNameGroups <= 2,
                    $"No StatBonus name should dominate the catalog (max duplicates {suffixNameGroups})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Affix catalog distribution should not throw: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
