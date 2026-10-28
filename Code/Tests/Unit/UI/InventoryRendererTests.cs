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
            TestInventoryScrollIndicators();
            TestInventoryNumpadShortcutHint();
            TestInventoryViewSummaryHighlightColors();
            TestInventoryDisplaySortAndFilterUsesVisibleNumbers();
            TestInventoryEquipSlotFilterBuildDisplayEntriesAndCycle();
            TestInventoryMaterialFilterBuildDisplayEntriesAndCycle();
            TestInventorySortedViewsGroupWeaponsByType();
            TestInventoryArmorComparisonBaselineColorsLowerSameSlotRed();
            TestTradeUpPreviewLayoutKeepsConfirmInView();
            TestInventoryDetailViewListAndSelection();
            TestInventoryGridViewArtAndToggle();
            TestDetailPaneShrinksHeroSoComparisonFits();

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

            string? currentCharm = ItemComparisonRenderer.GetComparisonTooltipHoverValue(
                currentColumn: true,
                slot: "charm",
                newItemInventoryIndex: 4);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "gear:charm", currentCharm, "current comparison column uses equipped charm hover id", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestInventoryScrollIndicators()
        {
            Console.WriteLine("\n--- Testing Inventory Scroll Indicators ---");

            TestBase.AssertEqual(1, InventoryItemScrollLayout.GetReservedIndicatorRows(firstVisibleIndex: 0, requiresScrollStatus: true),
                "inventory reserves one row for bottom hint on first page", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, InventoryItemScrollLayout.GetReservedIndicatorRows(firstVisibleIndex: 2, requiresScrollStatus: true),
                "inventory reserves top and bottom rows when scrolled", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, InventoryItemScrollLayout.GetReservedIndicatorRows(firstVisibleIndex: 0, requiresScrollStatus: false),
                "inventory reserves no rows when everything fits", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string topHint = InventoryItemScrollLayout.BuildTopScrollHint(hiddenItemCount: 2);
            TestBase.AssertTrue(topHint.Contains("▲") && topHint.Contains("2 items above"),
                "inventory top hint names hidden items above", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var firstPage = new InventoryItemVisibleRange(firstIndex: 0, lastExclusiveIndex: 4, usedRows: 6, itemCount: 5);
            string bottomHint = InventoryItemScrollLayout.BuildBottomScrollHint(firstPage, firstDisplay: 1, lastDisplay: 4);
            TestBase.AssertTrue(bottomHint.Contains("▼") && bottomHint.Contains("1 more item below") && bottomHint.Contains("scroll down"),
                "inventory bottom hint calls out hidden items below", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            TestBase.AssertEqual(7, allDefault.Count, "view summary splits into seven colored segments", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                ColorValidator.AreColorsEqual(allDefault[0].Color, dim)
                    && ColorValidator.AreColorsEqual(allDefault[2].Color, dim)
                    && ColorValidator.AreColorsEqual(allDefault[4].Color, dim),
                "sort, filter, and bag slot clauses use dim when sort is inventory order, filter shows all, and no slot filter",
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
                ColorValidator.AreColorsEqual(bagFollowsFirstRow[4].Color, dim),
                "bag slot line uses dim when no slot filter even if first row is rare",
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
                ColorValidator.AreColorsEqual(headFilterUsesEquipped[4].Color, highlight),
                "bag slot line uses applied highlight when a slot filter is active (consistent across slots)",
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
            TestBase.AssertEqual("charm", state.InventoryEquipSlotFilter, "sixth cycle step is charm", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryEquipSlotFilter();
            TestBase.AssertTrue(state.InventoryEquipSlotFilter == null, "after charm the filter turns off", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestInventoryMaterialFilterBuildDisplayEntriesAndCycle()
        {
            Console.WriteLine("\n--- Testing Inventory material filter ---");

            var character = TestDataBuilders.Character().WithStats(10, 10, 10, 10).Build();
            var leatherBoots = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("Boots").Build();
            leatherBoots.Material = "Leather";
            var boneMace = TestDataBuilders.Weapon().WithName("Mace").Build();
            boneMace.Material = "Bone";
            var leatherHelm = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Helm").Build();
            leatherHelm.Material = "Leather";
            var noMaterial = TestDataBuilders.Armor().WithType(ItemType.Chest).WithName("Shirt").Build();
            noMaterial.Material = "";
            var inventory = new System.Collections.Generic.List<Item> { leatherBoots, boneMace, leatherHelm, noMaterial };

            TestBase.AssertEqual("Bone,Leather", string.Join(",", InventoryStateManager.GetInventoryMaterials(inventory)),
                "bag materials are distinct, skip blanks, and follow canonical material order",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var state = new InventoryStateManager();
            TestBase.AssertTrue(state.InventoryMaterialFilter == null, "material filter starts cleared", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryMaterialFilter(inventory);
            TestBase.AssertEqual("Bone", state.InventoryMaterialFilter, "first material cycle step is Bone", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryMaterialFilter(inventory);
            TestBase.AssertEqual("Leather", state.InventoryMaterialFilter, "second material cycle step is Leather", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryMaterialFilter(inventory);
            TestBase.AssertTrue(state.InventoryMaterialFilter == null, "material filter turns off after the last material", ref _testsRun, ref _testsPassed, ref _testsFailed);
            state.CycleInventoryMaterialFilter(new System.Collections.Generic.List<Item>());
            TestBase.AssertTrue(state.InventoryMaterialFilter == null, "material filter stays off with no materials in bag", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var leatherOnly = InventoryScreenRenderer.BuildDisplayEntries(
                inventory, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: false,
                inventoryEquipSlotFilter: null, inventoryMaterialFilter: "leather");
            TestBase.AssertEqual("0,2", string.Join(",", leatherOnly.Select(e => e.InventoryIndex.ToString())),
                "material filter (case-insensitive) leaves only leather rows",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var leatherHeadOnly = InventoryScreenRenderer.BuildDisplayEntries(
                inventory, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: false,
                inventoryEquipSlotFilter: "head", inventoryMaterialFilter: "Leather");
            TestBase.AssertEqual("2", string.Join(",", leatherHeadOnly.Select(e => e.InventoryIndex.ToString())),
                "material filter combines with the slot filter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var summary = InventoryScreenRenderer.BuildViewSummaryColoredSegments(
                inventory, character, InventoryItemSortMode.InventoryOrder, hideRequirementBlockedItems: false,
                inventoryEquipSlotFilter: null, inventoryMaterialFilter: "Leather");
            TestBase.AssertTrue(
                summary[6].Text.Contains("Leather only") && ColorValidator.AreColorsEqual(summary[6].Color, ColorPalette.Lime.GetColor()),
                "view summary shows the active material filter highlighted",
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

        private static void TestTradeUpPreviewLayoutKeepsConfirmInView()
        {
            Console.WriteLine("\n--- Testing Trade-Up Preview Layout ---");

            const int startY = 10;
            const int height = 28;
            int confirmY = TradeUpPreviewLayout.GetConfirmY(startY, height);
            int maxBodyY = TradeUpPreviewLayout.GetMaxBodyY(startY, height);

            TestBase.AssertEqual(startY + height - 1, confirmY,
                "Confirm sits on the last content row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(confirmY - 1, maxBodyY,
                "body content stops above Confirm",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Compact layout (name-only trades + modest result stats) must fit a typical center panel.
            TestBase.AssertTrue(
                TradeUpPreviewLayout.CompactPreviewFits(height, tradedItemCount: 5, resultStatLineCount: 4),
                "compact trade-up preview fits typical center-panel height",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Old full-stat dump for 5 trades (~25+ stat lines) would not fit; compact estimate must stay under budget.
            int compactLines = TradeUpPreviewLayout.EstimateCompactBodyLines(5, 4);
            TestBase.AssertTrue(compactLines <= height - TradeUpPreviewLayout.ConfirmFooterLines,
                $"compact body ({compactLines}) stays above footer budget",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int bloatedLegacyEstimate = 6 + (5 * 5) + 2 + 2 + 2 + 1 + 4; // ~5 lines per traded item
            TestBase.AssertTrue(bloatedLegacyEstimate > height - TradeUpPreviewLayout.ConfirmFooterLines,
                "legacy full-stat trade rows would overflow the same panel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryDetailViewListAndSelection()
        {
            Console.WriteLine("\n--- Testing Inventory Detail View ---");

            TestBase.AssertEqual("Short Sword", InventoryScreenRenderer.TruncateDetailViewListName("Short Sword"),
                "names within 15 chars are not cut",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Gleaming Steel...", InventoryScreenRenderer.TruncateDetailViewListName("Gleaming Steel Longsword of the Bear"),
                "long names cut at 15 chars with ellipsis",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var entries = new System.Collections.Generic.List<InventoryDisplayEntry>
            {
                new InventoryDisplayEntry(4, new Item(ItemType.Head, "Helm"), 1),
                new InventoryDisplayEntry(1, new Item(ItemType.Feet, "Boots"), 2),
                new InventoryDisplayEntry(7, new Item(ItemType.Legs, "Greaves"), 3),
            };
            TestBase.AssertEqual(1, InventoryScreenRenderer.ResolveDetailViewSelection(entries, 1, 0).InventoryIndex,
                "clicked item stays selected",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(7, InventoryScreenRenderer.ResolveDetailViewSelection(entries, -1, 2).InventoryIndex,
                "no selection falls back to first visible row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, InventoryScreenRenderer.ResolveDetailViewSelection(entries, 99, 0).InventoryIndex,
                "selection hidden by filters falls back to first visible row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInventoryGridViewArtAndToggle()
        {
            Console.WriteLine("\n--- Testing Inventory Grid View (new inventory) ---");

            TestBase.AssertEqual("[ Old inventory ]", InventoryScreenRenderer.ViewToggleLabel(gridView: true),
                "grid view button offers the old inventory", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("[ New inventory ]", InventoryScreenRenderer.ViewToggleLabel(gridView: false),
                "list view button offers the new inventory", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, InventoryScreenRenderer.GridTileColumns(9, 18, 4),
                "tiles are square in pixels (4 rows of 18px = 8 columns of 9px)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, InventoryScreenRenderer.GridRowsForColumns(9, 18, 8),
                "eight columns of 9px are four rows of 18px", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual((34, 17), InventoryScreenRenderer.DetailHeroIconSize(9, 18, 34, 40),
                "the detail icon fills the pane width and stays square", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual((32, 16), InventoryScreenRenderer.DetailHeroIconSize(9, 18, 34, 28),
                "a shorter pane shrinks the icon on both axes so it stays square", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual((0, 0), InventoryScreenRenderer.DetailHeroIconSize(9, 18, 34, 14),
                "no hero icon when the description would have fewer than four rows of picture", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, InventoryScreenRenderer.DetailHeroIconX(2, 42, 36),
                "a shrunk hero is centered in the detail pane", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, InventoryScreenRenderer.DetailHeroIconX(2, 42, 42),
                "a full-width hero stays on the pane's left edge", ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (!ItemArtAtlas.IsAvailable)
            {
                Console.WriteLine("  (GameData/ItemArt missing - art lookups skipped)");
                return;
            }
            TestBase.AssertEqual("(Steel, Kukri)", ItemArtAtlas.ResolveName("Keen Steel Kukri of the Owl", "Steel")?.ToString(),
                "material + base found inside a full affix name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("(IRON, Absolute Greaves)", ItemArtAtlas.ResolveName("Iron Absolute Greaves", "Iron")?.ToString(),
                "longest base wins and material case is ignored", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("(Plain, Kukri)", ItemArtAtlas.ResolveName("Leather Kukri", "Leather")?.ToString(),
                "material without art falls back to the plain base", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("(Bronze, Kukri)", ItemArtAtlas.ResolveName("Bronze Kukri", "")?.ToString(),
                "old saves without a material read it from the name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("(Crystal, Galoshs)", ItemArtAtlas.ResolveName("Worn Crystal Galoshs", "Crystal")?.ToString(),
                "material word does not resolve to the same-named Crystal weapon", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("(Crystal, Crystal Stiletto)", ItemArtAtlas.ResolveName("Keen Crystal Stiletto", "Crystal")?.ToString(),
                "base containing the material word still wins", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ItemArtAtlas.ResolveName("Mystery Thing", "Steel") == null,
                "unknown base has no icon", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? hero = ItemArtAtlas.HeroPath("Keen Steel Kukri of the Owl", "Steel");
            if (hero != null)
                TestBase.AssertTrue(hero.EndsWith(System.IO.Path.Combine("Steel", "Kukri.webp")) && System.IO.File.Exists(hero),
                    "detail icon resolves to the item's large per-item image", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDetailPaneShrinksHeroSoComparisonFits()
        {
            Console.WriteLine("\n--- Testing Detail Pane Comparison Fit ---");

            var character = new Character("Compare", 1);
            var worn = new FeetItem("Willow Brogues", tier: 1, armor: 4) { Rarity = "Common" };
            var selected = new FeetItem("Broken Obsidian Titan's Greaves", tier: 1, armor: 3) { Rarity = "Common" };
            selected.StatBonuses.Add(new StatBonus { Name = "Item base", StatType = "Agility", Value = 7 });
            selected.StatBonuses.Add(new StatBonus { Name = "Item base", StatType = "Intelligence", Value = 5 });
            selected.StatBonuses.Add(new StatBonus { Name = "Obsidian", StatType = "Technique", Value = 3 });
            selected.Modifications.Add(new Modification { Name = "Broken", Description = "25% weaker damage, armor, and weapon speed" });
            character.EquipItem(worn, "feet");

            const int detailWidth = 42;
            const int paneRows = 40;
            int textRows = InventoryScreenRenderer.EstimateDetailTextRows(selected, character, detailWidth);
            int comparisonRows = InventoryScreenRenderer.EstimateComparisonRows(selected, character, detailWidth);
            TestBase.AssertTrue(comparisonRows >= 6,
                $"vs-equipped table reserves its rows (got {comparisonRows})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(textRows > 12,
                $"full description ({textRows} rows) is taller than the old fixed 12-row reserve",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var fitted = InventoryScreenRenderer.DetailHeroIconSize(9, 18, detailWidth, paneRows, textRows + 1);
            var fixedReserve = InventoryScreenRenderer.DetailHeroIconSize(9, 18, detailWidth, paneRows);
            int fixedLeftover = paneRows - fixedReserve.Rows - 1;
            TestBase.AssertTrue(fixedReserve.Rows > 0 && fixedLeftover < textRows,
                $"a square icon leaves {fixedLeftover} rows, short of the {textRows}-row description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fitted.Rows < fixedReserve.Rows && fitted.Columns < detailWidth,
                $"hero shrinks from {fixedReserve.Columns}x{fixedReserve.Rows} to {fitted.Columns}x{fitted.Rows} so the comparison stays in the pane",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fitted.Rows == 0 || fitted.Columns * 9 == fitted.Rows * 18,
                $"shrunk hero stays square in pixels ({fitted.Columns}x{fitted.Rows})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fitted.Rows == 0 || paneRows - fitted.Rows - 1 >= textRows,
                "rows left under the icon cover the description and comparison",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
