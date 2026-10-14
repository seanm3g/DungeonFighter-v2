using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Combat.Formatting;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    public static class EnvironmentRollModifierTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _run = _pass = _fail = 0;
            TestElegantBoostsRoll(ref _run, ref _pass, ref _fail);
            TestDilapidatedReducesRoll(ref _run, ref _pass, ref _fail);
            TestCancelledTagsDoNotAffectRoll(ref _run, ref _pass, ref _fail);
            TestClampDoesNotAffectRoll(ref _run, ref _pass, ref _fail);
            TestActionBlockLineOnlyWhenShiftLands(ref _run, ref _pass, ref _fail);
            testsRun += _run;
            testsPassed += _pass;
            testsFailed += _fail;
        }

        private static Environment MakeRoom(params string[] tags)
        {
            var room = new Environment("Test", "desc", true, "Forest");
            room.SetTags(tags);
            return room;
        }

        private static void TestElegantBoostsRoll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestElegantBoostsRoll));
            var room = MakeRoom("elegant");
            var hero = new Character("Hero", 5);
            int result = EnvironmentRollModifier.ApplyStructureRollShift(room, hero, 10);
            TestBase.AssertEqual(15, result, "elegant adds dungeon level", ref run, ref pass, ref fail);
        }

        private static void TestDilapidatedReducesRoll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestDilapidatedReducesRoll));
            var room = MakeRoom("dilapidated");
            var hero = new Character("Hero", 5);
            int result = EnvironmentRollModifier.ApplyStructureRollShift(room, hero, 10);
            TestBase.AssertEqual(5, result, "dilapidated subtracts dungeon level", ref run, ref pass, ref fail);
        }

        private static void TestCancelledTagsDoNotAffectRoll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestCancelledTagsDoNotAffectRoll));
            var room = MakeRoom("elegant", "dilapidated");
            var hero = new Character("Hero", 3);
            var shift = EnvironmentRollModifier.ResolveStructureRollShift(room, hero, 12);
            TestBase.AssertEqual(12, shift.Roll, "opposing structure tags cancel", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!shift.AffectsRoll, "cancelled shift does not affect the die", ref run, ref pass, ref fail);
        }

        private static void TestClampDoesNotAffectRoll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestClampDoesNotAffectRoll));
            var room = MakeRoom("elegant");
            var hero = new Character("Hero", 4);
            var shift = EnvironmentRollModifier.ResolveStructureRollShift(room, hero, 20);
            TestBase.AssertEqual(20, shift.Roll, "elegant cannot raise a natural 20", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!shift.AffectsRoll, "clamped shift does not affect the die", ref run, ref pass, ref fail);
        }

        private static void TestActionBlockLineOnlyWhenShiftLands(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestActionBlockLineOnlyWhenShiftLands));
            var room = MakeRoom("dilapidated");
            var hero = new Character("Hero", 1);
            var shift = EnvironmentRollModifier.ResolveStructureRollShift(room, hero, 18);
            TestBase.AssertEqual(17, shift.Roll, "level 1 dilapidated subtracts 1", ref run, ref pass, ref fail);
            TestBase.AssertEqual(-1, shift.AppliedDelta, "applied delta is -1", ref run, ref pass, ref fail);
            var line = RollInfoFormatter.FormatStructureRollLine(shift.Label, shift.AppliedDelta);
            string plain = line == null ? "" : ColoredTextRenderer.RenderAsPlainText(line);
            TestBase.AssertTrue(plain.Contains("(dilapidated: -1)", System.StringComparison.Ordinal),
                $"action block line names the room shift, got: {plain}", ref run, ref pass, ref fail);

            var none = RollInfoFormatter.FormatStructureRollLine(null, 0);
            TestBase.AssertTrue(none == null, "no line when the room does not change the die", ref run, ref pass, ref fail);

            var elegant = EnvironmentRollModifier.ResolveStructureRollShift(MakeRoom("elegant"), hero, 10);
            string elegantPlain = ColoredTextRenderer.RenderAsPlainText(
                RollInfoFormatter.FormatStructureRollLine(elegant.Label, elegant.AppliedDelta)!);
            TestBase.AssertTrue(elegantPlain.Contains("(elegant: +1)", System.StringComparison.Ordinal),
                $"elegant line is signed, got: {elegantPlain}", ref run, ref pass, ref fail);
        }
    }
}
