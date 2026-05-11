using System;
using System.Collections.Generic;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Ensures the Items settings tab uses the same Weapons.json repair path as <see cref="JsonLoader"/>.
    /// </summary>
    public static class ItemsDataServiceTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemsDataService Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWeaponsJsonNormalizationAllowsStringAttributeRequirements();
            TestStarterFlagHelpers();
            TestWeaponActionsSummaryUsesWeaponTypeRules();

            TestBase.PrintSummary("ItemsDataService Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestWeaponsJsonNormalizationAllowsStringAttributeRequirements()
        {
            Console.WriteLine("--- Testing Weapons.json normalization before deserialize (sheet-style TECH) ---");

            const string raw =
                """[{"Type":"Dagger","Name":"Razor","Tier":1,"attributeRequirements":"TECH","requirement value":15,"baseDamage":2,"attackSpeed":0.82,"damageBonusMin":1,"damageBonusMax":4}]""";

            try
            {
                var normalized = GameDataJsonNormalizer.NormalizeForGameDataFile(GameConstants.WeaponsJson, raw);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<WeaponData>>(normalized, options);

                bool ok = list is [{ AttributeRequirements: { } reqs }]
                    && reqs.TryGetValue("technique", out int v)
                    && v == 15;

                TestBase.AssertTrue(ok,
                    "Normalized weapons row should deserialize with technique requirement 15",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Unexpected failure: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestStarterFlagHelpers()
        {
            Console.WriteLine("\n--- Testing item settings starter flag helpers ---");

            var starter = ItemsDataCoordinator.TagsListWithStarterFlag("foo, starter", true);
            bool hasStarter = starter != null && GameDataTagHelper.HasTag(starter, StarterCatalogItems.StarterTag);
            TestBase.AssertTrue(hasStarter,
                "Starter checkbox should persist starter tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var notStarter = ItemsDataCoordinator.TagsListWithStarterFlag("foo, starter", false);
            bool removedStarter = notStarter != null &&
                !GameDataTagHelper.HasTag(notStarter, StarterCatalogItems.StarterTag) &&
                GameDataTagHelper.HasTag(notStarter, "foo");
            TestBase.AssertTrue(removedStarter,
                "Clearing starter checkbox should remove starter tag while keeping other tags",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponActionsSummaryUsesWeaponTypeRules()
        {
            Console.WriteLine("\n--- Testing item settings weapon action summary ---");

            var summary = ItemsDataCoordinator.FormatActionsSummaryForWeaponType("Sword");
            TestBase.AssertTrue(summary.Contains("STRIKE", StringComparison.OrdinalIgnoreCase),
                $"Sword action summary should include STRIKE; summary: {summary}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
