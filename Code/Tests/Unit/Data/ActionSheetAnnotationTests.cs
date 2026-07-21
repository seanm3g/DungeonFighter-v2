using System;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionSheetAnnotationTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionSheetAnnotation Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestCadenceList_HasFourUncompactedScopes(ref testsRun, ref testsPassed, ref testsFailed);
            TestMechanicDescription_HeroDamageNotSwingDamage(ref testsRun, ref testsPassed, ref testsFailed);
            TestAllowedCadences_FullWords(ref testsRun, ref testsPassed, ref testsFailed);
            TestHeaderNote_OnKillScopeExplainsCadences(ref testsRun, ref testsPassed, ref testsFailed);
            TestHeaderNote_MechanicsColumn(ref testsRun, ref testsPassed, ref testsFailed);
            TestHoverNote_IncludesRedundancyHint(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("ActionSheetAnnotation Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestCadenceList_HasFourUncompactedScopes(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestCadenceList_HasFourUncompactedScopes));
            TestBase.AssertEqual(4, CadenceScopeDescriptions.All.Length, "four cadences", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                CadenceScopeDescriptions.GetDetail("TURN").IndexOf("turn", StringComparison.OrdinalIgnoreCase) >= 0,
                "TURN detail",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                CadenceScopeDescriptions.GetDetail("ACTION").IndexOf("strip", StringComparison.OrdinalIgnoreCase) >= 0
                || CadenceScopeDescriptions.GetDetail("ACTION").IndexOf("bank", StringComparison.OrdinalIgnoreCase) >= 0,
                "ACTION detail",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                CadenceScopeDescriptions.GetDetail("FIGHT").Length > 40,
                "FIGHT uncompacted",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                CadenceScopeDescriptions.GetDetail("DUNGEON").Length > 40,
                "DUNGEON uncompacted",
                ref testsRun, ref testsPassed, ref testsFailed);
            string combined = CadenceScopeDescriptions.CombinedAuthoringNote();
            TestBase.AssertTrue(combined.Contains("TURN") && combined.Contains("FIGHT"), "combined note", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestMechanicDescription_HeroDamageNotSwingDamage(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestMechanicDescription_HeroDamageNotSwingDamage));
            string d = ActionMechanicDescriptions.GetDescription("hero_next_action_damage");
            TestBase.AssertTrue(d.IndexOf("DAMAGE MOD", StringComparison.OrdinalIgnoreCase) >= 0
                || d.IndexOf("next", StringComparison.OrdinalIgnoreCase) >= 0,
                "describes next-action damage",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestAllowedCadences_FullWords(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestAllowedCadences_FullWords));
            string dice = ActionMechanicDescriptions.FormatAllowedCadences("hero_accuracy");
            TestBase.AssertTrue(dice.Contains("TURN"), "TURN spelled out", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!dice.Contains("T/") && !dice.Equals("T", StringComparison.Ordinal), "not compacted T", ref testsRun, ref testsPassed, ref testsFailed);
            string next = ActionMechanicDescriptions.FormatAllowedCadences("hero_next_action_damage");
            TestBase.AssertTrue(next.Contains("ACTION"), "ACTION cadence", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestHeaderNote_OnKillScopeExplainsCadences(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeaderNote_OnKillScopeExplainsCadences));
            TestBase.AssertTrue(
                ActionSheetHeaderNotes.TryGetNote("TRIGGERS", "ON KILL SCOPE", out string note),
                "has ON KILL SCOPE note",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(note.Contains("FIGHT") && note.Contains("TURN"), "lists cadences", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ActionSheetHeaderNotes.TryGetNote("TRIGGERS", "ON KILL →", out string arrow),
                "has → note",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(arrow.IndexOf("pointer", StringComparison.OrdinalIgnoreCase) >= 0
                || arrow.IndexOf("magnitude", StringComparison.OrdinalIgnoreCase) >= 0,
                "→ explains pointers",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestHeaderNote_MechanicsColumn(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeaderNote_MechanicsColumn));
            TestBase.AssertTrue(
                ActionSheetHeaderNotes.TryGetNote(null, "MECHANICS", out string note),
                "MECHANICS note",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(note.IndexOf("MECHANIC_LIST", StringComparison.OrdinalIgnoreCase) >= 0, "points at list", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestHoverNote_IncludesRedundancyHint(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHoverNote_IncludesRedundancyHint));
            string note = ActionMechanicDescriptions.BuildHoverNote("hero_next_action_damage");
            TestBase.AssertTrue(note.IndexOf("DAMAGE(%)", StringComparison.OrdinalIgnoreCase) >= 0
                || note.IndexOf("swing", StringComparison.OrdinalIgnoreCase) >= 0
                || note.IndexOf("not the main", StringComparison.OrdinalIgnoreCase) >= 0,
                "redundancy vs swing damage",
                ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
