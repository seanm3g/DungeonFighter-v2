using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class GameDataTagHelperTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameDataTagHelper Tests ===\n");
            int run = 0, pass = 0, fail = 0;

            ParseCommaSeparatedTrimsAndSplits(ref run, ref pass, ref fail);
            ParseCommaSeparatedSemicolon(ref run, ref pass, ref fail);
            ParseCommaSeparatedPipeSplits(ref run, ref pass, ref fail);
            ParseCommaSeparatedDistinctCaseInsensitive(ref run, ref pass, ref fail);
            HasEnvironmentTagCaseInsensitive(ref run, ref pass, ref fail);
            HasEnvironmentTagFalseWhenMissing(ref run, ref pass, ref fail);
            HasTagCaseInsensitive(ref run, ref pass, ref fail);
            HasTagFalseWhenMissing(ref run, ref pass, ref fail);

            TestBase.PrintSummary("GameDataTagHelper Tests", run, pass, fail);
        }

        private static void ParseCommaSeparatedTrimsAndSplits(ref int run, ref int pass, ref int fail)
        {
            run++;
            var list = GameDataTagHelper.ParseCommaSeparatedTags("  a , b ; c  ");
            if (list.Count == 3 && list[0] == "a" && list[1] == "b" && list[2] == "c")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL ParseCommaSeparatedTrimsAndSplits: got [{string.Join("|", list)}]");
            }
        }

        private static void ParseCommaSeparatedSemicolon(ref int run, ref int pass, ref int fail)
        {
            run++;
            var list = GameDataTagHelper.ParseCommaSeparatedTags("x;y");
            if (list.SequenceEqual(new[] { "x", "y" }))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL ParseCommaSeparatedSemicolon");
            }
        }

        private static void ParseCommaSeparatedPipeSplits(ref int run, ref int pass, ref int fail)
        {
            run++;
            var list = GameDataTagHelper.ParseCommaSeparatedTags("a|b|c");
            if (list.SequenceEqual(new[] { "a", "b", "c" }))
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL ParseCommaSeparatedPipeSplits: [{string.Join("|", list)}]");
            }
        }

        private static void ParseCommaSeparatedDistinctCaseInsensitive(ref int run, ref int pass, ref int fail)
        {
            run++;
            var list = GameDataTagHelper.ParseCommaSeparatedTags("Boss, boss, BOSS");
            if (list.Count == 1 && list[0] == "Boss")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL ParseCommaSeparatedDistinctCaseInsensitive: count={list.Count}");
            }
        }

        private static void HasEnvironmentTagCaseInsensitive(ref int run, ref int pass, ref int fail)
        {
            run++;
            if (GameDataTagHelper.HasEnvironmentTag(new[] { "debuff", "Environment", "aoe" }))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL HasEnvironmentTagCaseInsensitive");
            }
        }

        private static void HasEnvironmentTagFalseWhenMissing(ref int run, ref int pass, ref int fail)
        {
            run++;
            if (!GameDataTagHelper.HasEnvironmentTag(new[] { "enemy", "attack" }) &&
                !GameDataTagHelper.HasEnvironmentTag(null))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL HasEnvironmentTagFalseWhenMissing");
            }
        }

        private static void HasTagCaseInsensitive(ref int run, ref int pass, ref int fail)
        {
            run++;
            if (GameDataTagHelper.HasTag(new[] { "melee", "Starter", "aoe" }, "starter"))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL HasTagCaseInsensitive");
            }
        }

        private static void HasTagFalseWhenMissing(ref int run, ref int pass, ref int fail)
        {
            run++;
            if (!GameDataTagHelper.HasTag(new[] { "melee" }, "starter") &&
                !GameDataTagHelper.HasTag(null, "starter") &&
                !GameDataTagHelper.HasTag(new[] { "x" }, ""))
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL HasTagFalseWhenMissing");
            }
        }
    }
}
