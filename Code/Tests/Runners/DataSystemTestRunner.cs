using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests.Unit.Config;
using RPGGame.Tests.Unit.Data;
using RPGGame.Tests.Unit.Data.Validation;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Data system tests
    /// </summary>
    public static class DataSystemTestRunner
    {
        private static readonly (string Name, System.Action Execute)[] Suites =
        {
            ("ActionLoader", () => ActionLoaderTests.RunAllTests()),
            ("ActionDataToSpreadsheetJsonConverter", () => ActionDataToSpreadsheetJsonConverterTests.RunAllTests()),
            ("SpreadsheetActionDataSheetRowSerializer", () => SpreadsheetActionDataSheetRowSerializerTests.RunAllTests()),
            ("SheetsPushConfig", () => SheetsPushConfigTests.RunAllTests()),
            ("PatchProfileService", () => PatchProfileServiceTests.RunAllTests()),
            ("BalancePatchMetadata", () => BalancePatchMetadataTests.RunAllTests()),
            ("GeneralSettingsStore", () => GeneralSettingsStoreTests.RunAllTests()),
            ("SheetsPushUtilities", () => SheetsPushUtilitiesTests.RunAllTests()),
            ("JsonArraySheetConverter", () => JsonArraySheetConverterTests.RunAllTests()),
            ("GameDataTagHelper", () => GameDataTagHelperTests.RunAllTests()),
            ("ClassPresentationSheetConverter", () => ClassPresentationSheetConverterTests.RunAllTests()),
            ("ClassActionsSheetConverter", () => ClassActionsSheetConverterTests.RunAllTests()),
            ("GoogleSheetsUrlHelper", () => GoogleSheetsUrlHelperTests.RunAllTests()),
            ("SheetsCsvFetch", () => SheetsCsvFetchTests.RunAllTests()),
            ("SheetsPushPreflight", () => SheetsPushPreflightTests.RunAllTests()),
            ("JsonLoader", () => JsonLoaderTests.RunAllTests()),
            ("LootGenerator", () => LootGeneratorTests.RunAllTests()),
            ("RoomSearchConsumable", () => RPGGame.Tests.Unit.World.RoomSearchConsumableTests.RunAllTests()),
            ("LootItemSelector", () => LootItemSelectorTests.RunAllTests()),
            ("ItemGenerator", () => ItemGeneratorTests.RunAllTests()),
            ("WeaponTypeFromCatalog", () => WeaponTypeFromCatalogTests.RunAllTests()),
            ("StarterCatalogItems", () => StarterCatalogItemsTests.RunAllTests()),
            ("LootBonusApplier", () => LootBonusApplierTests.RunAllTests()),
            ("ItemPrefixHelper", () => ItemPrefixHelperTests.RunAllTests()),
            ("ItemGetTotalArmorNullSafety", () => ItemGetTotalArmorNullSafetyTests.RunAllTests()),
            ("ItemGenerationLabService", () => ItemGenerationLabServiceTests.RunAllTests()),
            ("ItemGenerationBatchStatistics", () => ItemGenerationBatchStatisticsTests.RunAllTests()),
            ("LootDataCache", () => LootDataCacheTests.RunAllTests()),
            ("EnemyLoader", () => EnemyLoaderTests.RunAllTests()),
            ("EnemySpawnFilter", () => EnemySpawnFilterTests.RunAllTests()),
            ("EnemyRarityReward", () => EnemyRarityRewardTests.RunAllTests()),
            ("RoomLoader", () => RoomLoaderTests.RunAllTests()),
            ("ColorConfigurationLoader", () => ColorConfigurationLoaderTests.RunAllTests()),
            ("ActionDescriptionEnhancer", () => ActionDescriptionEnhancerTests.RunAllTests()),
            ("LootTierCalculator", () => LootTierCalculatorTests.RunAllTests()),
            ("LootContext", () => LootContextTests.RunAllTests()),
            ("LootRarityProcessor", () => LootRarityProcessorTests.RunAllTests()),
            ("GameDataValidator", () => GameDataValidatorTests.RunAllTests()),
            ("ActionDataValidator", () => ActionDataValidatorTests.RunAllTests()),
        };

        public static IReadOnlyList<FilteredTestRunner.TestSuiteEntry> GetSuiteEntries()
        {
            var entries = new List<FilteredTestRunner.TestSuiteEntry>(Suites.Length);
            foreach (var (name, execute) in Suites)
                entries.Add(new FilteredTestRunner.TestSuiteEntry("data", name, execute));
            return entries;
        }

        public static void RunAllTests() => RunFiltered(null);

        public static void RunFiltered(string? filter)
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  DATA SYSTEM TEST SUITE");
            if (!string.IsNullOrWhiteSpace(filter))
                Console.WriteLine($"  Filter: {filter}");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            foreach (var (name, execute) in Suites)
            {
                if (!TestRunFilter.Matches(name, filter))
                    continue;
                execute();
                Console.WriteLine();
            }
        }
    }
}
