using System;
using RPGGame.Handlers.Inventory;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 2.3 (D5.5, D5.8): no gear changes and no strip edits inside a dungeon.</summary>
    public static class DungeonGearLockTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonGearLock Tests ===\n");
            _run = _passed = _failed = 0;

            var sm = new GameStateManager();
            var hero = TestDataBuilders.Character().WithName("Locked In").Build();
            var helm = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Run Helm").Build();
            hero.EquipItem(helm, "head");
            var spare = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Spare Helm").Build();
            sm.SetCurrentPlayer(hero);
            sm.CurrentInventory.Add(spare);
            sm.TransitionToState(GameState.Inventory);
            var handler = new InventoryItemActionHandler(sm, null, new InventoryStateManager());
            string? said = null;
            handler.ShowMessageEvent += m => said = m;

            sm.SetCurrentDungeon(new Dungeon("Test Crypt", 1, 1, "Crypt"));
            TestBase.AssertTrue(handler.UnequipSlot("head", refreshInventoryScreen: false) == null && ReferenceEquals(hero.Head, helm),
                "Unequip is refused inside a dungeon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(InventoryItemActionHandler.GearLockedMessage, said, "It says why", ref _run, ref _passed, ref _failed);
            bool equipped = handler.ConfirmEquipItem(sm.CurrentInventory.IndexOf(spare), "head", equipNew: true, refreshInventoryScreen: false, announce: false, out string? why);
            TestBase.AssertTrue(!equipped && ReferenceEquals(hero.Head, helm), $"Equip is refused inside a dungeon ({why})", ref _run, ref _passed, ref _failed);

            sm.SetCurrentDungeon(null);
            bool after = handler.ConfirmEquipItem(sm.CurrentInventory.IndexOf(spare), "head", equipNew: true, refreshInventoryScreen: false, announce: false, out _);
            TestBase.AssertTrue(after && ReferenceEquals(hero.Head, spare), "Out of the dungeon, equipping works again", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("DungeonGearLock Tests", _run, _passed, _failed);
        }
    }
}
