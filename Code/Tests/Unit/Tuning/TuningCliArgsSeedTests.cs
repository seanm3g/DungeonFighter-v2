using System;
using RPGGame.Tests;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tests.Unit.Tuning
{
    /// <summary>--seed parsing: in-range seeds unchanged, script-built shard seeds past int.MaxValue no longer drop to unseeded.</summary>
    public static class TuningCliArgsSeedTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TuningCliArgs Seed Tests ===\n");
            _run = _pass = _fail = 0;

            TestInRangeSeedsUnchanged();
            TestLargeSeedsFoldDeterministically();
            TestMissingAndInvalidSeed();

            TestBase.PrintSummary("TuningCliArgs Seed Tests", _run, _pass, _fail);
        }

        private static int? Seed(string value) => TuningCliArgs.GetSeedFlag(new[] { "DUNGEONSIM", "--seed", value });

        private static void TestInRangeSeedsUnchanged()
        {
            Console.WriteLine("--- In-range seeds unchanged ---");
            TestBase.AssertEqual(1010008001, Seed("1010008001") ?? -1, "dungeon-sim seed 1010, L8, shard 1", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(int.MaxValue, Seed(int.MaxValue.ToString()) ?? -1, "int.MaxValue", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(7, TuningCliArgs.GetSeedFlag(new[] { "X", "--seed=7" }) ?? -1, "--seed=7 form", ref _run, ref _pass, ref _fail);
        }

        private static void TestLargeSeedsFoldDeterministically()
        {
            Console.WriteLine("--- Large seeds fold, stay distinct ---");
            int? a = Seed("4040008000");   // dungeon-sim --seed 4040, L8, shard 0 (past int.MaxValue)
            int? b = Seed("4040008001");
            TestBase.AssertTrue(a.HasValue && b.HasValue, "large seeds are seeded, not null", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(a ?? -1, Seed("4040008000") ?? -2, "same value folds the same way", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(a != b, "neighbouring shard seeds stay distinct", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue((a ?? -1) >= 0, "folded seed is non-negative", ref _run, ref _pass, ref _fail);
        }

        private static void TestMissingAndInvalidSeed()
        {
            Console.WriteLine("--- Missing / invalid seed ---");
            TestBase.AssertTrue(TuningCliArgs.GetSeedFlag(new[] { "DUNGEONSIM", "--level", "8" }) == null, "no --seed means unseeded", ref _run, ref _pass, ref _fail);
            bool threw = false;
            try { Seed("abc"); } catch (ArgumentException) { threw = true; }
            TestBase.AssertTrue(threw, "non-integer --seed throws instead of running unseeded", ref _run, ref _pass, ref _fail);
        }
    }
}
