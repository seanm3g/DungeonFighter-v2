using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ClassActionsSheetConverterTests
    {
        public static void RunAllTests()
        {
            int run = 0, pass = 0, fail = 0;
            Console.WriteLine("=== ClassActionsSheetConverter Tests ===\n");

            ParseHeadersAndRows(ref run, ref pass, ref fail);
            ParseGoogleStyleTierClassActions(ref run, ref pass, ref fail);
            PunchSheetLabelMapsToPunchHard(ref run, ref pass, ref fail);
            ClassActionsTierLadderLesserThroughAbyssal(ref run, ref pass, ref fail);
            ParseDuoClassExpandsToTwoPaths(ref run, ref pass, ref fail);
            TierZeroAndMinPoints(ref run, ref pass, ref fail);
            BuildPushRows(ref run, ref pass, ref fail);

            TestBase.PrintSummary("ClassActionsSheetConverter Tests", run, pass, fail);
        }

        private static void ParseHeadersAndRows(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ParseHeadersAndRows));
            const string csv = "Class Level,Class,Action\n1,Barbarian,FOLLOW THROUGH\n0,Warrior,TAUNT\n";
            var cfg = ClassActionsSheetConverter.ParseCsvToConfig(csv);
            TestBase.AssertEqual(2, cfg.Rules.Count, "row count", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cfg.Rules.Any(r => r.ActionName == "FOLLOW THROUGH" && r.Tier == 1 && r.ClassKey == "Barbarian"),
                "barbarian row", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cfg.Rules.Any(r => r.ActionName == "TAUNT" && r.Tier == 0), "warrior taunt", ref run, ref pass, ref fail);
        }

        private static void ParseGoogleStyleTierClassActions(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ParseGoogleStyleTierClassActions));
            const string csv = """
TIER,CLASS,ACTIONS
Lesser,Barbarian,["PUNCH","KICK"]
""";
            var cfg = ClassActionsSheetConverter.ParseCsvToConfig(csv);
            TestBase.AssertEqual(2, cfg.Rules.Count, "two actions from JSON array", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cfg.Rules.All(r => r.Tier == 1 && r.ClassKey == "Barbarian"), "lesser→tier 1", ref run, ref pass, ref fail);
        }

        private static void PunchSheetLabelMapsToPunchHard(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(PunchSheetLabelMapsToPunchHard));
            const string csv = """
TIER,CLASS,ACTIONS
Lesser,Barbarian,PUNCH
""";
            var cfg = ClassActionsSheetConverter.ParseCsvToConfig(csv);
            TestBase.AssertTrue(cfg.Rules.Count == 1, "one rule", ref run, ref pass, ref fail);
            if (cfg.Rules.Count > 0)
                TestBase.AssertEqual("PUNCH HARD", cfg.Rules[0].ActionName, "sheet PUNCH → Actions.json name", ref run, ref pass, ref fail);
        }

        private static void ClassActionsTierLadderLesserThroughAbyssal(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ClassActionsTierLadderLesserThroughAbyssal));
            var words = new[] { ("Lesser", 1), ("Blooded", 2), ("Dread", 3), ("Abyssal", 4) };
            foreach (var (word, expectTier) in words)
            {
                string csv = $"TIER,CLASS,ACTIONS\n{word},Barbarian,[\"T{expectTier}\"]\n";
                var cfg = ClassActionsSheetConverter.ParseCsvToConfig(csv);
                TestBase.AssertTrue(cfg.Rules.Count == 1, $"{word}: one rule", ref run, ref pass, ref fail);
                if (cfg.Rules.Count > 0)
                    TestBase.AssertEqual(expectTier, cfg.Rules[0].Tier, $"{word}→tier {expectTier}", ref run, ref pass, ref fail);
            }
        }

        private static void ParseDuoClassExpandsToTwoPaths(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ParseDuoClassExpandsToTwoPaths));
            const string csv = """
TIER,CLASS,ACTIONS
Blooded,Warbrute,["JAB"]
""";
            var cfg = ClassActionsSheetConverter.ParseCsvToConfig(csv);
            TestBase.AssertEqual(2, cfg.Rules.Count, "duo → Mace + Sword", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cfg.Rules.Any(r => r.ClassKey == "Mace" && r.ActionName == "JAB"), "mace", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cfg.Rules.Any(r => r.ClassKey == "Sword" && r.ActionName == "JAB"), "sword", ref run, ref pass, ref fail);
        }

        private static void TierZeroAndMinPoints(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TierZeroAndMinPoints));
            var pres = new ClassPresentationConfig().EnsureNormalized();
            var r0 = new ClassActionUnlockRule { Tier = 0, ClassKey = "Warrior", ActionName = "TAUNT" };
            TestBase.AssertTrue(ClassActionsUnlockConfig.IsRuleUnlocked(r0, 1, pres), "tier 0 at 1 pt", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!ClassActionsUnlockConfig.IsRuleUnlocked(r0, 0, pres), "tier 0 at 0 pt", ref run, ref pass, ref fail);

            var rMin = new ClassActionUnlockRule { Tier = 1, ClassKey = "Barbarian", ActionName = "BERSERK", MinClassPoints = 3 };
            TestBase.AssertTrue(!ClassActionsUnlockConfig.IsRuleUnlocked(rMin, 2, pres), "min blocks at 2", ref run, ref pass, ref fail);
            TestBase.AssertTrue(ClassActionsUnlockConfig.IsRuleUnlocked(rMin, 3, pres), "min allows at 3", ref run, ref pass, ref fail);
        }

        private static void BuildPushRows(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(BuildPushRows));
            var cfg = new ClassActionsUnlockConfig
            {
                Rules =
                {
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Warrior", ActionName = "TAUNT" },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Barbarian", ActionName = "BERSERK", MinClassPoints = 3 }
                }
            };
            var rows = ClassActionsSheetConverter.BuildPushValueRows(cfg);
            TestBase.AssertTrue(rows.Count == 3, "header + 2 rows", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Class Level", rows[0][0]?.ToString() ?? "", "header col0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("TAUNT", rows[1][2]?.ToString() ?? "", "row1 action", ref run, ref pass, ref fail);
            TestBase.AssertEqual("3", rows[2][3]?.ToString() ?? "", "row2 min pts", ref run, ref pass, ref fail);
        }
    }
}
