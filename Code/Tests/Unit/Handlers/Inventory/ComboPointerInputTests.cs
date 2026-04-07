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
            TestRemove(ref run, ref passed, ref failed);
            TestInvalid(ref run, ref passed, ref failed);

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
    }
}
