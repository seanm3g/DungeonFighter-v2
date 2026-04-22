using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Handlers.Inventory
{
    /// <summary>
    /// Tests for <see cref="InventoryMenuHandler.TryHandleStripRightClickRemove"/> and
    /// <see cref="GameCoordinator.TryHandleInventoryStripRightClickRemove"/>.
    /// </summary>
    public static class InventoryMenuStripRightClickTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Inventory strip right-click (sequence remove) Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestRemovesFirstSlot(ref run, ref passed, ref failed);
            TestBlockedDuringEquipPrompt(ref run, ref passed, ref failed);
            TestInvalidIndexReturnsFalse(ref run, ref passed, ref failed);
            TestGameCoordinatorDelegates(ref run, ref passed, ref failed);

            TestBase.PrintSummary("Inventory strip right-click Tests", run, passed, failed);
        }

        private static void ClearCombo(Character c)
        {
            var list = c.GetComboActions();
            while (list.Count > 0)
                c.RemoveFromCombo(list[0], ignoreWeaponRequirement: true);
        }

        private static void TestRemovesFirstSlot(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var c = TestDataBuilders.Character().WithName("StripRm").Build();
            var junk = TestDataBuilders.Item().WithName("Junk").Build();
            c.Inventory.Add(junk);

            var a1 = TestDataBuilders.CreateMockAction("Z1");
            var a2 = TestDataBuilders.CreateMockAction("Z2");
            foreach (var a in new[] { a1, a2 })
            {
                a.IsComboAction = true;
                c.AddAction(a, 1.0);
            }

            ClearCombo(c);
            c.AddToCombo(a1);
            c.AddToCombo(a2);

            sm.SetCurrentPlayer(c);
            sm.TransitionToState(GameState.Inventory);

            var handler = new InventoryMenuHandler(sm, null);
            bool ok = handler.TryHandleStripRightClickRemove(0);
            var names = c.GetComboActions().Select(a => a.Name).ToList();

            TestBase.AssertTrue(ok && names.Count == 1 && string.Equals(names[0], "Z2", StringComparison.Ordinal),
                "Strip right-click removes slot 0", ref run, ref passed, ref failed);
        }

        private static void TestBlockedDuringEquipPrompt(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var c = TestDataBuilders.Character().WithName("StripBlock").Build();
            c.Inventory.Add(TestDataBuilders.Item().WithName("BlockItem").Build());

            var a1 = TestDataBuilders.CreateMockAction("B1");
            var a2 = TestDataBuilders.CreateMockAction("B2");
            foreach (var a in new[] { a1, a2 })
            {
                a.IsComboAction = true;
                c.AddAction(a, 1.0);
            }

            ClearCombo(c);
            c.AddToCombo(a1);
            c.AddToCombo(a2);

            sm.SetCurrentPlayer(c);
            sm.TransitionToState(GameState.Inventory);

            var handler = new InventoryMenuHandler(sm, null);
            handler.HandleMenuInput("1"); // equip flow → waiting for item index

            int before = c.GetComboActions().Count;
            bool blocked = !handler.TryHandleStripRightClickRemove(0);
            int after = c.GetComboActions().Count;

            TestBase.AssertTrue(blocked && before == 2 && after == 2,
                "Strip remove ignored during equip item selection", ref run, ref passed, ref failed);

            handler.HandleMenuInput("0"); // cancel equip prompt
        }

        private static void TestInvalidIndexReturnsFalse(ref int run, ref int passed, ref int failed)
        {
            var sm = new GameStateManager();
            var c = TestDataBuilders.Character().WithName("StripBadIdx").Build();
            var a1 = TestDataBuilders.CreateMockAction("Q1");
            a1.IsComboAction = true;
            c.AddAction(a1, 1.0);
            ClearCombo(c);
            c.AddToCombo(a1);

            sm.SetCurrentPlayer(c);
            sm.TransitionToState(GameState.Inventory);

            var handler = new InventoryMenuHandler(sm, null);
            TestBase.AssertTrue(!handler.TryHandleStripRightClickRemove(99),
                "Out-of-range index returns false", ref run, ref passed, ref failed);
        }

        private static void TestGameCoordinatorDelegates(ref int run, ref int passed, ref int failed)
        {
            var game = new GameCoordinator();
            var sm = game.StateManager;
            var c = TestDataBuilders.Character().WithName("CoordStrip").Build();
            var x = TestDataBuilders.CreateMockAction("X9");
            x.IsComboAction = true;
            c.AddAction(x, 1.0);
            ClearCombo(c);
            c.AddToCombo(x);

            sm.SetCurrentPlayer(c);
            sm.TransitionToState(GameState.Inventory);

            TestBase.AssertTrue(game.TryHandleInventoryStripRightClickRemove(0) && c.GetComboActions().Count == 0,
                "GameCoordinator.TryHandleInventoryStripRightClickRemove delegates and clears lone slot", ref run, ref passed, ref failed);
        }
    }
}
