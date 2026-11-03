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
            // Balance patches cap the shift (roomStructureRollShiftCap); these tests pin the uncapped level shift.
            var combat = GameConfiguration.Instance.Combat;
            int savedCap = combat.RoomStructureRollShiftCap;
            combat.RoomStructureRollShiftCap = 0;
            try
            {
                TestElegantBoostsRoll(ref _run, ref _pass, ref _fail);
                TestDilapidatedReducesRoll(ref _run, ref _pass, ref _fail);
                TestCancelledTagsDoNotAffectRoll(ref _run, ref _pass, ref _fail);
                TestClampDoesNotAffectRoll(ref _run, ref _pass, ref _fail);
                TestActionBlockLineOnlyWhenShiftLands(ref _run, ref _pass, ref _fail);
                TestStrikeIsD20Threshold(ref _run, ref _pass, ref _fail);
                TestRoomTagsShowAsStatusEffects(ref _run, ref _pass, ref _fail);
            }
            finally
            {
                combat.RoomStructureRollShiftCap = savedCap;
            }
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

        private static void TestStrikeIsD20Threshold(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestStrikeIsD20Threshold));
            var state = new EnvironmentCombatStateManager();
            TestBase.AssertEqual(20, state.GetStrikeThreshold(true), "fresh room strikes only on a natural 20", ref run, ref pass, ref fail);
            TestBase.AssertEqual(21, state.GetStrikeThreshold(false), "non-hostile room never acts", ref run, ref pass, ref fail);
            try
            {
                Dice.QueueUnforcedTestRolls(19, 19);
                TestBase.AssertTrue(!state.ShouldEnvironmentAct(true), "19 misses a 20 threshold", ref run, ref pass, ref fail);
                TestBase.AssertEqual(19, state.GetStrikeThreshold(true), "quiet turn widens the strike range by one face", ref run, ref pass, ref fail);
                TestBase.AssertTrue(state.ShouldEnvironmentAct(true), "19 meets the widened threshold", ref run, ref pass, ref fail);
                TestBase.AssertEqual(20, state.GetStrikeThreshold(true), "striking resets the threshold", ref run, ref pass, ref fail);
            }
            finally
            {
                Dice.ClearUnforcedTestRolls();
            }
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
            TestBase.AssertTrue(plain.Contains("(Dilapidated: -1)", System.StringComparison.Ordinal),
                $"action block line names the room shift, got: {plain}", ref run, ref pass, ref fail);

            var none = RollInfoFormatter.FormatStructureRollLine(null, 0);
            TestBase.AssertTrue(none == null, "no line when the room does not change the die", ref run, ref pass, ref fail);

            var elegant = EnvironmentRollModifier.ResolveStructureRollShift(MakeRoom("elegant"), hero, 10);
            string elegantPlain = ColoredTextRenderer.RenderAsPlainText(
                RollInfoFormatter.FormatStructureRollLine(elegant.Label, elegant.AppliedDelta)!);
            TestBase.AssertTrue(elegantPlain.Contains("(Elegant: +1)", System.StringComparison.Ordinal),
                $"elegant line is signed, got: {elegantPlain}", ref run, ref pass, ref fail);
        }

        private static void TestRoomTagsShowAsStatusEffects(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestRoomTagsShowAsStatusEffects));
            var hero = new Character("Hero", 5);
            var enemy = new Enemy(name: "Foe", level: 5, maxHealth: 50, strength: 5, agility: 5, technique: 5, intelligence: 5, armor: 0);
            var room = MakeRoom("elegant");
            room.SetUnstableThresholdMod(2);
            using (CombatEnvironmentContext.BeginScope(room))
            {
                var heroLines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                TestBase.AssertTrue(heroLines.Contains("Elegant +5"), $"hero shows Elegant +level, got: {string.Join(" | ", heroLines)}",
                    ref run, ref pass, ref fail);
                TestBase.AssertTrue(heroLines.Contains("Unstable +2"), "hero shows Unstable", ref run, ref pass, ref fail);

                var enemyLines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(enemy, enemy);
                TestBase.AssertTrue(!enemyLines.Exists(l => l.StartsWith("Elegant", System.StringComparison.Ordinal)),
                    "enemy rolls are not shifted, so no Elegant row", ref run, ref pass, ref fail);
                TestBase.AssertTrue(enemyLines.Contains("Unstable +2"), "Unstable is on everyone in the room", ref run, ref pass, ref fail);

                var hover = HoverEffectCopy.ExplainStatusLine(hero, "Elegant +5");
                TestBase.AssertTrue(hover.Exists(l => l.Contains("Applied by: Test (room)", System.StringComparison.Ordinal)),
                    $"hover names the room, got: {string.Join(" | ", hover)}", ref run, ref pass, ref fail);
                TestBase.AssertTrue(hover.Exists(l => l.StartsWith("Ends: when you leave the room", System.StringComparison.Ordinal)),
                    "hover says how it ends", ref run, ref pass, ref fail);
            }

            var dilapidated = MakeRoom("dilapidated");
            using (CombatEnvironmentContext.BeginScope(dilapidated))
            {
                var lines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                TestBase.AssertTrue(lines.Contains("Dilapidated −5"), "dilapidated shows as a negative status", ref run, ref pass, ref fail);
            }

            using (CombatEnvironmentContext.BeginScope(MakeRoom("elegant", "dilapidated")))
            {
                var lines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                TestBase.AssertTrue(lines.TrueForAll(l => RoomStatusEffects.NameFromLine(l) == null),
                    "cancelled structure tags give no status", ref run, ref pass, ref fail);
            }

            using (CombatEnvironmentContext.BeginScope(MakeRoom("active")))
            {
                var heroLines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                var enemyLines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(enemy, enemy);
                TestBase.AssertTrue(heroLines.Contains(RoomStatusEffects.ActiveName) && enemyLines.Contains(RoomStatusEffects.ActiveName),
                    "active room shows on hero and enemy", ref run, ref pass, ref fail);
                var hover = HoverEffectCopy.ExplainActorStatusLine(enemy, RoomStatusEffects.ActiveName, hero);
                TestBase.AssertTrue(hover.Exists(l => l.Contains("up to 3 times", System.StringComparison.Ordinal)),
                    $"active hover gives the act cap, got: {string.Join(" | ", hover)}", ref run, ref pass, ref fail);
            }

            using (CombatEnvironmentContext.BeginScope(MakeRoom("dormant")))
            {
                var lines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                TestBase.AssertTrue(lines.Contains(RoomStatusEffects.DormantName), "dormant room shows as a status", ref run, ref pass, ref fail);
            }

            using (CombatEnvironmentContext.BeginScope(MakeRoom("cycling")))
            {
                var lines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                TestBase.AssertTrue(lines.TrueForAll(l => RoomStatusEffects.NameFromLine(l) == null),
                    "cycling is the default cadence and gives no status", ref run, ref pass, ref fail);
            }

            var calm = new Environment("Calm", "desc", false, "Forest");
            calm.SetTags(new[] { "active" });
            using (CombatEnvironmentContext.BeginScope(calm))
            {
                var lines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
                TestBase.AssertTrue(!lines.Contains(RoomStatusEffects.ActiveName),
                    "a room that never acts shows no activity status", ref run, ref pass, ref fail);
            }
        }
    }
}
