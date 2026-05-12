using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Handlers.Inventory
{
    public static class InventoryAutoEquipEmptySlotsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Inventory auto-equip empty slots Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestShortcutFillsEmptySlotsWithoutReplacingGear(ref run, ref passed, ref failed);
            TestBlockedItemsDoNotEquip(ref run, ref passed, ref failed);
            TestEquipSelectionKeepsSortedFilteredOriginalItemNumbers(ref run, ref passed, ref failed);

            TestBase.PrintSummary("Inventory auto-equip empty slots Tests", run, passed, failed);
        }

        private static void TestShortcutFillsEmptySlotsWithoutReplacingGear(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var character = TestDataBuilders.Character().WithName("AutoEquip").Build();
            var equippedWeapon = TestDataBuilders.Weapon().WithName("Held Sword").Build();
            character.EquipItem(equippedWeapon, "weapon");

            var blockedHead = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Too Heavy Helm").Build();
            blockedHead.AttributeRequirements.AddRequirement("strength", 999);
            var goodHead = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Ready Helm").Build();
            var body = TestDataBuilders.Armor().WithType(ItemType.Chest).WithName("Ready Armor").Build();
            var spareWeapon = TestDataBuilders.Weapon().WithName("Bag Sword").Build();

            character.Inventory.Add(blockedHead);
            character.Inventory.Add(goodHead);
            character.Inventory.Add(body);
            character.Inventory.Add(spareWeapon);
            sm.SetCurrentPlayer(character);
            sm.TransitionToState(GameState.Inventory);

            string lastMessage = "";
            int refreshes = 0;
            var handler = new InventoryMenuHandler(sm, null);
            handler.ShowMessageEvent += msg => lastMessage = msg;
            handler.ShowInventoryEvent += () => refreshes++;

            handler.HandleMenuInput("*");

            TestBase.AssertTrue(
                ReferenceEquals(character.Weapon, equippedWeapon)
                && ReferenceEquals(character.Head, goodHead)
                && ReferenceEquals(character.Body, body)
                && character.Inventory.Contains(blockedHead)
                && character.Inventory.Contains(spareWeapon)
                && !character.Inventory.Contains(goodHead)
                && !character.Inventory.Contains(body),
                "Numpad shortcut fills empty compatible slots and leaves occupied weapon alone",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                refreshes == 1 && lastMessage.Contains("Auto-equipped", StringComparison.Ordinal)
                && lastMessage.Contains("Head: Ready Helm", StringComparison.Ordinal)
                && lastMessage.Contains("Body: Ready Armor", StringComparison.Ordinal),
                "Auto-equip reports equipped slots and refreshes inventory",
                ref run, ref passed, ref failed);
        }

        private static void TestBlockedItemsDoNotEquip(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var character = TestDataBuilders.Character().WithName("AutoBlocked").Build();
            var blockedFeet = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("Impossible Boots").Build();
            blockedFeet.AttributeRequirements.AddRequirement("agility", 999);
            character.Inventory.Add(blockedFeet);
            sm.SetCurrentPlayer(character);
            sm.TransitionToState(GameState.Inventory);

            string lastMessage = "";
            var handler = new InventoryMenuHandler(sm, null);
            handler.ShowMessageEvent += msg => lastMessage = msg;

            handler.HandleMenuInput("*");

            TestBase.AssertTrue(
                character.Feet == null && character.Inventory.Contains(blockedFeet),
                "Auto-equip skips items blocked by attribute requirements",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                lastMessage.Contains("blocked by requirements", StringComparison.OrdinalIgnoreCase),
                "Auto-equip explains when matching items are requirement-blocked",
                ref run, ref passed, ref failed);
        }

        private static void TestEquipSelectionKeepsSortedFilteredOriginalItemNumbers(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var character = TestDataBuilders.Character()
                .WithName("SortedEquip")
                .WithStats(10, 10, 10, 10)
                .Build();

            var commonFeet = TestDataBuilders.Armor().WithType(ItemType.Feet).WithName("Common Boots").Build();
            commonFeet.Rarity = "Common";
            var blockedMythicHead = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Mythic Crown").Build();
            blockedMythicHead.Rarity = "Mythic";
            blockedMythicHead.AttributeRequirements.AddRequirement("strength", 999);
            var rareWeapon = TestDataBuilders.Weapon().WithName("Rare Sword").Build();
            rareWeapon.Rarity = "Rare";

            character.Inventory.Add(commonFeet);          // option [1]
            character.Inventory.Add(blockedMythicHead);  // option [2], hidden by requirements filter
            character.Inventory.Add(rareWeapon);          // option [3], first visible after rarity sort + filter
            sm.SetCurrentPlayer(character);
            sm.TransitionToState(GameState.Inventory);

            string lastMessage = "";
            var handler = new InventoryMenuHandler(sm, null);
            handler.ShowMessageEvent += msg => lastMessage = msg;

            handler.HandleMenuInput("+"); // Rarity sort
            handler.HandleMenuInput("-"); // Hide unmet requirements
            handler.HandleMenuInput("1"); // Equip item prompt
            handler.HandleMenuInput("2"); // Hidden item should not leave the prompt

            TestBase.AssertTrue(
                character.Head == null
                && character.Inventory.Contains(blockedMythicHead)
                && lastMessage.Contains("current inventory filter", StringComparison.OrdinalIgnoreCase),
                "Equip selection rejects items hidden by the active requirements filter",
                ref run, ref passed, ref failed);

            handler.HandleMenuInput("3"); // Original inventory number for Rare Sword, not display position 1
            handler.HandleMenuInput("2"); // Equip new item from comparison

            TestBase.AssertTrue(
                ReferenceEquals(character.Weapon, rareWeapon)
                && !character.Inventory.Contains(rareWeapon)
                && character.Inventory.Contains(commonFeet)
                && character.Inventory.Contains(blockedMythicHead),
                "Equip selection uses original inventory item numbers after sort/filter",
                ref run, ref passed, ref failed);
        }
    }
}
