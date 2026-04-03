using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Handlers.Inventory;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Handlers.Inventory
{
    /// <summary>
    /// Tests for <see cref="ComboReorderer.ApplyReorderMove"/>.
    /// </summary>
    public static class ComboReordererMoveTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboReorderer.ApplyReorderMove Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestMoveFirstToLastMatchesApplyReorder(ref run, ref passed, ref failed);
            TestMoveNoOpReturnsFalse(ref run, ref passed, ref failed);
            TestMoveMiddleSlot(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ComboReorderer.ApplyReorderMove Tests", run, passed, failed);
        }

        private static void TestMoveFirstToLastMatchesApplyReorder(ref int run, ref int passed, ref int failed)
        {
            var character = TestDataBuilders.Character().WithName("ReorderMove").Build();
            var a1 = TestDataBuilders.CreateMockAction("A1");
            var a2 = TestDataBuilders.CreateMockAction("A2");
            var a3 = TestDataBuilders.CreateMockAction("A3");
            foreach (var a in new[] { a1, a2, a3 })
            {
                a.IsComboAction = true;
                character.AddAction(a, 1.0);
            }

            character.AddToCombo(a1);
            character.AddToCombo(a2);
            character.AddToCombo(a3);
            var snapshot = character.GetComboActions();

            TestBase.AssertTrue(ComboReorderer.ApplyReorderMove(character, snapshot, 0, 2),
                "ApplyReorderMove 0 to 2 succeeds", ref run, ref passed, ref failed);

            var names = character.GetComboActions().Select(a => a.Name).ToList();

            var character2 = TestDataBuilders.Character().WithName("ReorderMoveStr").Build();
            var b1 = TestDataBuilders.CreateMockAction("A1");
            var b2 = TestDataBuilders.CreateMockAction("A2");
            var b3 = TestDataBuilders.CreateMockAction("A3");
            foreach (var a in new[] { b1, b2, b3 })
            {
                a.IsComboAction = true;
                character2.AddAction(a, 1.0);
            }

            character2.AddToCombo(b1);
            character2.AddToCombo(b2);
            character2.AddToCombo(b3);
            var snap2 = character2.GetComboActions();
            TestBase.AssertTrue(ComboReorderer.ApplyReorder(character2, "231", snap2),
                "ApplyReorder with permutation 231 succeeds", ref run, ref passed, ref failed);
            var names2 = character2.GetComboActions().Select(a => a.Name).ToList();
            TestBase.AssertTrue(names.Count == names2.Count && names.SequenceEqual(names2),
                "ApplyReorderMove matches ApplyReorder permutation 231 (same final order)", ref run, ref passed, ref failed);
        }

        private static void TestMoveNoOpReturnsFalse(ref int run, ref int passed, ref int failed)
        {
            var character = TestDataBuilders.Character().WithName("NoOp").Build();
            var a1 = TestDataBuilders.CreateMockAction("X1");
            a1.IsComboAction = true;
            character.AddAction(a1, 1.0);
            character.AddToCombo(a1);
            var snap = new List<Action>(character.GetComboActions());

            bool ok = ComboReorderer.ApplyReorderMove(character, snap, 0, 0);
            TestBase.AssertTrue(!ok, "Same index from/to returns false", ref run, ref passed, ref failed);
        }

        private static void TestMoveMiddleSlot(ref int run, ref int passed, ref int failed)
        {
            var character = TestDataBuilders.Character().WithName("Middle").Build();
            var a1 = TestDataBuilders.CreateMockAction("M1");
            var a2 = TestDataBuilders.CreateMockAction("M2");
            var a3 = TestDataBuilders.CreateMockAction("M3");
            foreach (var a in new[] { a1, a2, a3 })
            {
                a.IsComboAction = true;
                character.AddAction(a, 1.0);
            }

            character.AddToCombo(a1);
            character.AddToCombo(a2);
            character.AddToCombo(a3);
            var snapshot = character.GetComboActions();

            TestBase.AssertTrue(ComboReorderer.ApplyReorderMove(character, snapshot, 2, 0),
                "Move index 2 to 0 succeeds", ref run, ref passed, ref failed);
            var names = character.GetComboActions().Select(a => a.Name).ToList();

            var character2 = TestDataBuilders.Character().WithName("MiddleStr").Build();
            var b1 = TestDataBuilders.CreateMockAction("M1");
            var b2 = TestDataBuilders.CreateMockAction("M2");
            var b3 = TestDataBuilders.CreateMockAction("M3");
            foreach (var a in new[] { b1, b2, b3 })
            {
                a.IsComboAction = true;
                character2.AddAction(a, 1.0);
            }

            character2.AddToCombo(b1);
            character2.AddToCombo(b2);
            character2.AddToCombo(b3);
            var snap2 = character2.GetComboActions();
            TestBase.AssertTrue(ComboReorderer.ApplyReorder(character2, "312", snap2),
                "ApplyReorder 312 matches move 2 to 0", ref run, ref passed, ref failed);
            var names2 = character2.GetComboActions().Select(a => a.Name).ToList();
            TestBase.AssertTrue(names.SequenceEqual(names2),
                "ApplyReorderMove 2 to 0 matches ApplyReorder 312", ref run, ref passed, ref failed);
        }
    }
}
