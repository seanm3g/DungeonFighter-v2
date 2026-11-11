using System;
using RPGGame.Handlers.Inventory;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Handlers.Inventory
{
    /// <summary>
    /// Tests for <see cref="ComboPointerInput.TryParse"/> (mouse tokens for the actions workspace).
    /// </summary>
    public static class ComboPointerInputTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ComboPointerInput.TryParse Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestBack(ref run, ref passed, ref failed);
            TestAddAll(ref run, ref passed, ref failed);
            TestReorder(ref run, ref passed, ref failed);
            TestPool(ref run, ref passed, ref failed);
            TestInvPool(ref run, ref passed, ref failed);
            TestRemove(ref run, ref passed, ref failed);
            TestInvalid(ref run, ref passed, ref failed);
            TestInsertAndMove(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ComboPointerInput.TryParse Tests", run, passed, failed);
        }

        private static void TestBack(ref int run, ref int passed, ref int failed)
        {
            bool ok = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}back", out var k, out var i);
            TestBase.AssertTrue(ok && k == ComboPointerInput.Kind.Back && i == -1,
                "cpi:back parses", ref run, ref passed, ref failed);
        }

        private static void TestAddAll(ref int run, ref int passed, ref int failed)
        {
            bool ok = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}addall", out var k, out _);
            TestBase.AssertTrue(ok && k == ComboPointerInput.Kind.AddAll,
                "cpi:addall parses", ref run, ref passed, ref failed);
        }

        private static void TestReorder(ref int run, ref int passed, ref int failed)
        {
            bool ok = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}reorder", out var k, out _);
            TestBase.AssertTrue(ok && k == ComboPointerInput.Kind.Reorder,
                "cpi:reorder parses", ref run, ref passed, ref failed);
        }

        private static void TestPool(ref int run, ref int passed, ref int failed)
        {
            bool ok = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}pool:3", out var k, out var i);
            TestBase.AssertTrue(ok && k == ComboPointerInput.Kind.PoolAdd && i == 3,
                "cpi:pool:3 parses index 3", ref run, ref passed, ref failed);
        }

        private static void TestInvPool(ref int run, ref int passed, ref int failed)
        {
            bool ok = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}invpool:2", out var k, out var i);
            TestBase.AssertTrue(ok && k == ComboPointerInput.Kind.InvPoolEquip && i == 2,
                "cpi:invpool:2 parses flat index 2", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}invpool:-1", out _, out _),
                "cpi:invpool:-1 rejected", ref run, ref passed, ref failed);
        }

        private static void TestRemove(ref int run, ref int passed, ref int failed)
        {
            bool ok = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}rm:0", out var k, out var i);
            TestBase.AssertTrue(ok && k == ComboPointerInput.Kind.SequenceRemove && i == 0,
                "cpi:rm:0 parses", ref run, ref passed, ref failed);
        }

        private static void TestInvalid(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(!ComboPointerInput.TryParse("nope", out _, out _),
                "non-prefix fails", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}pool:abc", out _, out _),
                "cpi:pool:abc fails", ref run, ref passed, ref failed);
        }

        private static void TestInsertAndMove(ref int run, ref int passed, ref int failed)
        {
            bool ins = ComboPointerInput.TryParse(ComboPointerInput.Insert(3, 1), out var k1, out int a1, out int b1);
            TestBase.AssertTrue(ins && k1 == ComboPointerInput.Kind.PoolInsert && a1 == 3 && b1 == 1,
                "cpi:insert:3:1 parses", ref run, ref passed, ref failed);
            bool mv = ComboPointerInput.TryParse(ComboPointerInput.Move(0, 2), out var k2, out int a2, out int b2);
            TestBase.AssertTrue(mv && k2 == ComboPointerInput.Kind.SequenceMove && a2 == 0 && b2 == 2,
                "cpi:move:0:2 parses", ref run, ref passed, ref failed);
            bool rm = ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}rm:4", out var k3, out int a3, out int b3);
            TestBase.AssertTrue(rm && k3 == ComboPointerInput.Kind.SequenceRemove && a3 == 4 && b3 == -1,
                "4-arg parse keeps single-index tokens", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ComboPointerInput.TryParse($"{ComboPointerInput.Prefix}move:1", out _, out _, out _),
                "cpi:move with one number fails", ref run, ref passed, ref failed);
            bool rep = ComboPointerInput.TryParse(ComboPointerInput.Replace(5, 2), out var k4, out int a4, out int b4);
            TestBase.AssertTrue(rep && k4 == ComboPointerInput.Kind.PoolReplace && a4 == 5 && b4 == 2,
                "cpi:replace:5:2 parses", ref run, ref passed, ref failed);
            bool sw = ComboPointerInput.TryParse(ComboPointerInput.Swap(1, 3), out var k5, out int a5, out int b5);
            TestBase.AssertTrue(sw && k5 == ComboPointerInput.Kind.SequenceSwap && a5 == 1 && b5 == 3,
                "cpi:swap:1:3 parses", ref run, ref passed, ref failed);
        }
    }
}
