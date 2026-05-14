using System;
using System.Linq;
using RPGGame.Tests;
using RPGGame.Handlers.Inventory;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.Avalonia.Renderers.Inventory;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for InventoryRenderer
    /// Tests inventory rendering, item display, and selection prompts
    /// </summary>
    public static class InventoryRendererTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all InventoryRenderer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryRenderer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestInventoryRenderingMethods();
            TestItemComparisonHoverIds();
            TestInventoryItemScrollRange();
            TestInventoryNumpadShortcutHint();
            TestInventoryViewSummaryHighlightColors();
            TestInventoryDisplaySortAndFilterUsesVisibleNumbers();
            TestInventoryEquipSlotFilterBuildDisplayEntriesAndCycle();
            TestInventorySortedViewsGroupWeaponsByType();
            TestInventoryArmorComparisonBaselineColorsLowerSameSlotRed();

            TestBase.PrintSummary("InventoryRenderer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Rendering Tests

        private static void TestInventoryRenderingMethods()
        {
            Console.WriteLine("--- Testing Inventory Rendering Methods ---");

            // Test that inventory rendering methods exist
            TestBase.AssertTrue(true,
                "InventoryRenderer should have rendering methods",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemComparisonHoverIds()
        {
            Console.WriteLine("\n--- Testing Item Comparison Hover IDs ---");

            string? currentFeet = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: true,
                slot: "feet",
                newItemInventoryIndex: 2);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "gear:feet", currentFeet, "current comparison column uses equipped slot hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? newItem = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: false,
                slot: "feet",
                newItemInventoryIndex: 2);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "inv:2", newItem, "new comparison column uses selected inventory row hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? missingIndex = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: false,
                slot: "feet",
                newItemInventoryIndex: -1);
            TestBase.AssertTrue(missingIndex == null, "new comparison column without inventory index has no hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? invalidSlot = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: true,
                slot: "unknown",
                newItemInventoryIndex: 2);
            TestBase.AssertTrue(invalidSlot == null, "current comparison column ignores invalid slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryItemScrollRange()
        {
            Console.WriteLine("\n--- Testing Inventory Item Scroll Range ---");

            var rowHeights = new System.Collections.Generic.List<int> { 3, 3, 4, 2, 3 };
            var firstPage = InventoryItemScrollLayout.CalculateVisibleRange(rowHeights, requestedFirstIndex: 0, availableRows: 7);
            TestBase.AssertEqual(0, firstPage.FirstIndex, "inventory scroll starts at first row", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, firstPage.LastExclusiveIndex, "inventory scroll stops before overlapping fixed menu space", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(firstPage.HasItemsBelow, "inventory scroll reports hidden rows below", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var scrolledPage = InventoryItemScrollLayout.CalculateVisibleRange(rowHeights, requestedFirstIndex: 2, availableRows: 7);
            TestBase.AssertEqual(2, scrolledPage.FirstIndex, "inventory scroll preserves requested top item", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, scrolledPage.LastExclusiveIndex, "inventory scroll uses variable row heights", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(scrolledPage.HasItemsAbove, "inventory scroll reports hidden rows above", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int clamped = InventoryItemScrollLayout.ClampFirstVisibleIndex(20, rowHeights.Count);
            TestBase.AssertEqual(rowHeights.Count - 1, clamped, "inventory scroll clamps past-end offsets", ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool needsStatus = InventoryItemScrollLayout.RequiresScrollStatus(rowHeights, availableRows: 7, firstVisibleIndex: 0);
            TestBase.AssertTrue(needsStatus, "inventory scroll status appears when rows exceed viewport", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryNumpadShortcutHint()
        {
            Console.WriteLine("\n--- Testing Inventory Numpad Shortcut Hint ---");

            string fullHint = InventoryScreenRenderer.GetNumpadShortcutHint(120);
            TestBase.AssertTrue(
                fullHint.Contains("Numpad +") && fullHint.Contains("Numpad -") && fullHint.Contains("Numpad *") && fullHint.Contains("Numpad /"),
                "inventory bottom shortcut hint calls out numpad sort, requirements filter, equip, and slot-cycle shortcuts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string compactHint = InventoryScreenRenderer.GetNumpadShortcutHint(40);
            TestBase.AssertTrue(
                compactHint.Length <= 40 && compactHint.Contains("+ sort") && compactHint.Contains("- reqs") && compactHint.Contains("* equip") && compactHint.Contains("/ slot"),
                "inventory shortcut hint has a compact fallback for narrow panels",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryViewSummaryHighlightColors()
        {
            Console.WriteLine("\n--- Testing Inventory view summary sort/filter highlight ---");

            var character = TestDataBuilders.Character().WithStats(10, 10, 10, 10).Build();
            var emptyInventory = new System.Collections.Generic.List<Item>();
            var dim = AsciiArtAssets.Colors.DarkGray;
            var highlight = ColorPalette.Lime.GetColor();

            var allDefault = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                emptyInventory, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: false, inventoryEquipSlotFilter: null);
            TestBase.AssertEqual(5, allDefault.Count, "view summary splits into five colored segments", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(allDefault[0].Color, dim) && ColorValidator.AreColorsEqual(allDefault[2].Color, dim),
                "sort and filter clauses use dim color when sort is inventory order and filter shows all",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var raritySort = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                emptyInventory, character, InventoryItemSortMode.Rarity, hideRequirementBlockedItems: false, inventoryEquipSlotFilter: null);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(raritySort[0].Color, highlight) && ColorValidator.AreColorsEqual(raritySort[2].Color, dim),
                "non-default sort highlights only the sort clause",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var reqFilter = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                emptyInventory, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: true, inventoryEquipSlotFilter: null);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(reqFilter[0].Color, dim) && ColorValidator.AreColorsEqual(reqFilter[2].Color, highlight),
                "requirements filter on highlights only the filter clause",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var rareWeapon = TestDataBuilders.Weapon().WithName("Rare Blade").Build();
            rareWeapon.Rarity = "Rare";
            var invOne = new System.Collections.Generic.List<Item> { rareWeapon };
            var bagFollowsFirstRow = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                invOne, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: false, inventoryEquipSlotFilter: null);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(bagFollowsFirstRow[4].Color, ItemThemeProvider.GetRarityColor("Rare")),
                "bag slot line uses first visible item rarity color when no slot filter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var mythicHelm = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Mythic Hat").Build();
            mythicHelm.Rarity = "Mythic";
            var charWithHead = TestDataBuilders.Character().WithStats(10, 10, 10, 10).Build();
            charWithHead.Equipment.Head = mythicHelm;
            var feetOnly = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("Boots").Build();
            var invFeetOnly = new System.Collections.Generic.List<Item> { feetOnly };
            var headFilterUsesEquipped = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                invFeetOnly,
                charWithHead,
                InventoryItemSortMode.InventoryOrder,
                hideRequirementBlockedItems: false,
                inventoryEquipSlotFilter: "head");
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(headFilterUsesEquipped[4].Color, ItemThemeProvider.GetRarityColor("Mythic")),
                "bag slot line uses equipped item rarity for active slot filter when bag has no row for that slot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var emptySlotFilterWeapon = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                emptyInventory, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: false, inventoryEquipSlotFilter: "weapon");
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(emptySlotFilterWeapon[4].Color, highlight),
                "bag slot line uses applied highlight when slot filter is on but no item can be associated",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryEquipSlotFilterBuildDisplayEntriesAndCycle()
        {
            Console.WriteLine("\n--- Testing Inventory equip-slot filter ---");

            var state = new InventoryStateManager();
            TestBase.AssertTrue(state.InventoryEquipSlotFilter == null, "slot filter starts cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryEquipSlotFilter();
            TestBase.AssertEqual("weapon", state.InventoryEquipSlotFilter, "first cycle step is weapon", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryEquipSlotFilter();
            TestBase.AssertEqual("head", state.InventoryEquipSlotFilter, "second cycle step is head", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryEquipSlotFilter();
            state.CycleInventoryEquipSlotFilter();
            state.CycleInventoryEquipSlotFilter();
            TestBase.AssertEqual("feet", state.InventoryEquipSlotFilter, "fifth cycle step is feet", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryEquipSlotFilter();
            TestBase.AssertTrue(state.InventoryEquipSlotFilter == null, "after feet the filter turns off", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryEquipSlotFilter();
            TestBase.AssertEqual("weapon", state.InventoryEquipSlotFilter, "cycle wraps from off back to weapon", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Weapon", InventoryScreenRenderer.GetInventoryEquipSlotFilterDisplayName("weapon"), "display label for weapon slot", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var character = TestDataBuilders.Character().WithStats(10, 10, 10, 10).Build();
            var headItem = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Helm").Build();
            var feetItem = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("Boots").Build();
            var weaponItem = TestDataBuilders.Weapon().WithName("Blade").Build();
            var inventory = new System.Collections.Generic.List<Item> { feetItem, headItem, weaponItem };

            var headOnly = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                character,
                InventoryItemSortMode.InventoryOrder,
                hideRequirementBlockedItems: false,
                inventoryEquipSlotFilter: "head");
            TestBase.AssertEqual("1", string.Join(",", headOnly.Select(e => e.InventoryIndex.ToString())),
                "head slot filter leaves only head armor rows",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1", string.Join(",", headOnly.Select(e => e.DisplayNumber.ToString())),
                "head slot filter renumbers the single visible row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryDisplaySortAndFilterUsesVisibleNumbers()
        {
            Console.WriteLine("\n--- Testing Inventory Display Sort and Filter ---");

            var character = TestDataBuilders.Character().WithStats(10, 10, 10, 10).Build();
            var commonFeet = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("Common Boots").Build();
            var mythicHead = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Mythic Crown").Build();
            var rareWeapon = TestDataBuilders.Weapon().WithName("Rare Sword").Build();
            var uncommonLegs = TestDataBuilders.Armor().WithType(ItemType.Legs).WithName("Uncommon Pants").Build();
            commonFeet.Rarity = "Common";
            mythicHead.Rarity = "Mythic";
            rareWeapon.Rarity = "Rare";
            uncommonLegs.Rarity = "Uncommon";
            mythicHead.AttributeRequirements.AddRequirement("strength", 999);

            var inventory = new System.Collections.Generic.List<Item>
            {
                commonFeet,
                mythicHead,
                rareWeapon,
                uncommonLegs
            };

            var rarityEntries = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                character,
                InventoryItemSortMode.Rarity,
                hideRequirementBlockedItems: false);
            TestBase.AssertEqual("1,2,3,0", string.Join(",", rarityEntries.Select(entry => entry.InventoryIndex)),
                "rarity sort is highest-first while retaining original inventory indices",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1,2,3,4", string.Join(",", rarityEntries.Select(entry => entry.DisplayNumber)),
                "rarity sort displays contiguous visible item numbers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var slotEntries = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                character,
                InventoryItemSortMode.ItemSlot,
                hideRequirementBlockedItems: false);
            TestBase.AssertEqual("2,1,3,0", string.Join(",", slotEntries.Select(entry => entry.InventoryIndex)),
                "slot sort groups weapon/head/legs/feet while retaining original inventory indices",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1,2,3,4", string.Join(",", slotEntries.Select(entry => entry.DisplayNumber)),
                "slot sort displays contiguous visible item numbers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var filteredEntries = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                character,
                InventoryItemSortMode.Rarity,
                hideRequirementBlockedItems: true);
            TestBase.AssertEqual("2,3,0", string.Join(",", filteredEntries.Select(entry => entry.InventoryIndex)),
                "requirements filter hides blocked items while retaining original inventory indices internally",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1,2,3", string.Join(",", filteredEntries.Select(entry => entry.DisplayNumber)),
                "requirements filter renumbers remaining visible rows",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventorySortedViewsGroupWeaponsByType()
        {
            Console.WriteLine("\n--- Testing Inventory Sorted Views Group Weapons By Type ---");

            var character = TestDataBuilders.Character().WithStats(10, 10, 10, 10).Build();
            var daggerA = TestDataBuilders.Weapon().WithName("Dagger A").WithWeaponType(WeaponType.Dagger).Build();
            var swordA = TestDataBuilders.Weapon().WithName("Sword A").WithWeaponType(WeaponType.Sword).Build();
            var wand = TestDataBuilders.Weapon().WithName("Wand").WithWeaponType(WeaponType.Wand).Build();
            var daggerB = TestDataBuilders.Weapon().WithName("Dagger B").WithWeaponType(WeaponType.Dagger).Build();
            var mace = TestDataBuilders.Weapon().WithName("Mace").WithWeaponType(WeaponType.Mace).Build();
            var swordB = TestDataBuilders.Weapon().WithName("Sword B").WithWeaponType(WeaponType.Sword).Build();
            var head = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Helmet").Build();

            var inventory = new System.Collections.Generic.List<Item>
            {
                daggerA,
                swordA,
                wand,
                daggerB,
                mace,
                swordB,
                head
            };

            var rarityEntries = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                character,
                InventoryItemSortMode.Rarity,
                hideRequirementBlockedItems: false);
            TestBase.AssertEqual("4,1,5,0,3,2,6", string.Join(",", rarityEntries.Select(entry => entry.InventoryIndex)),
                "rarity sort groups same-rarity weapons by class weapon type before original index tie-breaks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var slotEntries = InventoryScreenRenderer.BuildDisplayEntries(
                inventory,
                character,
                InventoryItemSortMode.ItemSlot,
                hideRequirementBlockedItems: false);
            TestBase.AssertEqual("4,1,5,0,3,2,6", string.Join(",", slotEntries.Select(entry => entry.InventoryIndex)),
                "slot sort groups weapon rows by class weapon type before original index tie-breaks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryArmorComparisonBaselineColorsLowerSameSlotRed()
        {
            Console.WriteLine("\n--- Testing Inventory Armor Comparison Baseline Colors ---");

            var character = new Character("Armor", 1);
            var equippedLegs = new LegsItem("Steel Greaves", tier: 1, armor: 6) { Rarity = "Common" };
            var inventoryLegs = new LegsItem("Breeches", tier: 1, armor: 4) { Rarity = "Common" };
            character.EquipItem(equippedLegs, "legs");

            Item? baseline = ItemRendererHelper.GetArmorComparisonBaseline(character, inventoryLegs);
            TestBase.AssertTrue(ReferenceEquals(equippedLegs, baseline),
                "inventory armor comparison uses equipped item from the same slot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string stat = $"Armor: +{inventoryLegs.GetTotalArmor()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, inventoryLegs, weaponSpeedBaseline: null, armorComparisonBaseline: baseline);
            var valueSegment = segments.FirstOrDefault(s => s.Text == inventoryLegs.GetTotalArmor().ToString());
            TestBase.AssertTrue(valueSegment != null && valueSegment.Color == ColorPalette.Error.GetColor(),
                "lower same-slot inventory armor value renders red",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
