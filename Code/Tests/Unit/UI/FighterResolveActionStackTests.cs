using System;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Resolve-card timing: hidden until ACTION, then centered; archives left on enemy turn.
    /// </summary>
    public static class FighterResolveActionStackTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== FighterResolveActionStack Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestStagedUntilActionColumn(ref run, ref passed, ref failed);
            TestActionColumnReveal(ref run, ref passed, ref failed);
            TestArchiveMovesToPrevious(ref run, ref passed, ref failed);
            TestEnemyActionRevealsWithEnemyFlag(ref run, ref passed, ref failed);
            TestEnemyArchiveMovesToRight(ref run, ref passed, ref failed);
            TestInstantHudRevealsImmediately(ref run, ref passed, ref failed);
            TestCurrentCardIsCenteredInBand(ref run, ref passed, ref failed);
            TestPreviousCardSitsLeftOfCurrent(ref run, ref passed, ref failed);
            TestEnemyPreviousCardSitsRightOfCurrent(ref run, ref passed, ref failed);
            TestMissMarksCurrentAfterReveal(ref run, ref passed, ref failed);
            TestPlainHitMarksUnusedX(ref run, ref passed, ref failed);
            TestComboDoesNotMarkX(ref run, ref passed, ref failed);
            TestCritDoesNotMarkX(ref run, ref passed, ref failed);
            TestMissArchivesWithPrevious(ref run, ref passed, ref failed);

            FighterResolveActionStackState.Clear();
            var historyHero = HeroWithSlam();
            var foreignAction = TestDataBuilders.CreateMockAction("RIPOSTE");
            foreignAction.Description = "Counter with a quick blade strike.";
            FighterResolveActionStackState.BeginResolve(historyHero, foreignAction);
            FighterResolveActionStackState.RevealCurrent();
            TestBase.AssertEqual("RIPOSTE", FighterResolveActionStackState.Current!.Value.Name,
                "non-combo action retains selected identity", ref run, ref passed, ref failed);
            TestBase.AssertEqual(foreignAction.Description, FighterResolveActionStackState.Current!.Value.Description,
                "selected description reaches the card", ref run, ref passed, ref failed);
            FighterResolveActionStackState.MarkCurrentMissed();
            FighterResolveActionStackState.ArchiveCurrent();
            FighterResolveActionStackState.ArchiveCurrent();
            TestBase.AssertEqual(1, FighterResolveActionStackState.FighterHistory.Count,
                "archiving twice does not duplicate cards", ref run, ref passed, ref failed);
            FighterResolveActionStackState.BeginResolve(EnemyWithBite(), foreignAction);
            FighterResolveActionStackState.RevealCurrent();
            FighterResolveActionStackState.ArchiveCurrent();
            TestBase.AssertTrue(FighterResolveActionStackState.FighterHistory[0].Unused &&
                !FighterResolveActionStackState.EnemyHistory[0].Unused,
                "separate histories retain their own outcomes", ref run, ref passed, ref failed);
            for (int i = 0; i < 15; i++)
            {
                var action = TestDataBuilders.CreateMockAction("ACTION " + i);
                FighterResolveActionStackState.BeginResolve(historyHero, action);
                FighterResolveActionStackState.RevealCurrent();
                FighterResolveActionStackState.ArchiveCurrent();
            }
            TestBase.AssertEqual(12, FighterResolveActionStackState.FighterHistory.Count,
                "history remains bounded", ref run, ref passed, ref failed);
            TestBase.AssertEqual("ACTION 14", FighterResolveActionStackState.FighterHistory[0].Info.Name,
                "newest action is on top", ref run, ref passed, ref failed);
            FighterResolveActionStackState.Clear();
            TestBase.AssertTrue(FighterResolveActionStackState.FighterHistory.Count == 0 && FighterResolveActionStackState.EnemyHistory.Count == 0,
                "encounter reset clears both piles", ref run, ref passed, ref failed);
            TestBase.PrintSummary("FighterResolveActionStack Tests", run, passed, failed);
        }

        private static Character HeroWithSlam()
        {
            var hero = TestDataBuilders.CreateTestCharacter("Tyler");
            var slam = TestDataBuilders.CreateMockAction("SLAM");
            slam.DamageMultiplier = 1.2;
            hero.AddToCombo(slam);
            return hero;
        }

        private static void TestStagedUntilActionColumn(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                TestBase.AssertTrue(FighterResolveActionStackState.Current == null,
                    "current card stays hidden before ACTION column",
                    ref run, ref passed, ref failed);

                var attacker = new CombatSequenceStep(
                    CombatSequenceStepKind.Attacker, "ATTACKER",
                    new ColoredTextBuilder().Add("Tyler", ColorPalette.Info).Build());
                CombatSequencePresenter.SetPending(new System.Collections.Generic.List<CombatSequenceStep> { attacker });
                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();
                TestBase.AssertTrue(FighterResolveActionStackState.Current == null,
                    "ATTACKER column does not reveal the action square",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestActionColumnReveal(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);

                var action = new CombatSequenceStep(
                    CombatSequenceStepKind.Action, "ACTION",
                    new ColoredTextBuilder().Add("SLAM", ColorPalette.Success).Build());
                CombatSequencePresenter.SetPending(new System.Collections.Generic.List<CombatSequenceStep> { action });
                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();

                TestBase.AssertTrue(FighterResolveActionStackState.Current.HasValue,
                    "ACTION column reveals the centered action square",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual("SLAM", FighterResolveActionStackState.Current!.Value.Name,
                    "revealed card is the resolving action",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static Enemy EnemyWithBite()
        {
            return MockFactories.CreateMockEnemy("Wolf");
        }

        private static void TestArchiveMovesToPrevious(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                FighterResolveActionStackState.RevealCurrent();
                FighterResolveActionStackState.ArchiveCurrentToPrevious();

                TestBase.AssertTrue(FighterResolveActionStackState.Current == null,
                    "enemy turn clears the centered card",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(FighterResolveActionStackState.Previous.HasValue,
                    "enemy turn moves the card to the past-action slot",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual("SLAM", FighterResolveActionStackState.Previous!.Value.Name,
                    "past-action card keeps the resolved name",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestEnemyActionRevealsWithEnemyFlag(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var enemy = EnemyWithBite();
                var bite = TestDataBuilders.CreateMockAction("BITE");
                FighterResolveActionStackState.BeginResolve(enemy, bite);
                TestBase.AssertTrue(FighterResolveActionStackState.Current == null,
                    "enemy card stays hidden before ACTION column",
                    ref run, ref passed, ref failed);

                var action = new CombatSequenceStep(
                    CombatSequenceStepKind.Action, "ACTION",
                    new ColoredTextBuilder().Add("BITE", ColorPalette.Success).Build());
                CombatSequencePresenter.SetPending(new System.Collections.Generic.List<CombatSequenceStep> { action });
                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();

                TestBase.AssertTrue(FighterResolveActionStackState.Current.HasValue,
                    "ACTION column reveals the enemy action square",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(FighterResolveActionStackState.CurrentIsEnemy,
                    "centered enemy card is marked as an enemy action",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual("BITE", FighterResolveActionStackState.Current!.Value.Name,
                    "revealed enemy card is the resolving action",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestEnemyArchiveMovesToRight(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var enemy = EnemyWithBite();
                var bite = TestDataBuilders.CreateMockAction("BITE");
                FighterResolveActionStackState.BeginResolve(enemy, bite);
                FighterResolveActionStackState.RevealCurrent();
                FighterResolveActionStackState.ArchiveCurrent();

                TestBase.AssertTrue(FighterResolveActionStackState.Current == null,
                    "resolved enemy turn clears the centered card",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(!FighterResolveActionStackState.Previous.HasValue,
                    "enemy past-action does not occupy the fighter left slot",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(FighterResolveActionStackState.EnemyPrevious.HasValue,
                    "resolved enemy card moves to the right past-action slot",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual("BITE", FighterResolveActionStackState.EnemyPrevious!.Value.Name,
                    "right-side past card keeps the enemy action name",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestInstantHudRevealsImmediately(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                TestBase.AssertTrue(FighterResolveActionStackState.Current.HasValue,
                    "instant combat shows the action square without waiting for ACTION",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestCurrentCardIsCenteredInBand(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            CombatArenaHudLayout.GetResolveStackBand(out int bx, out _, out int bw, out _);
            CombatArenaHudLayout.GetResolveCurrentCardRect(out int cx, out _, out int cw, out _);
            int leftover = bw - cw;
            int expectedX = bx + leftover / 2;
            TestBase.AssertEqual(expectedX, cx,
                "resolving action square is centered in the mid-band",
                ref run, ref passed, ref failed);
        }

        private static void TestPreviousCardSitsLeftOfCurrent(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            CombatArenaHudLayout.GetResolveCurrentCardRect(out int cx, out _, out _, out _);
            CombatArenaHudLayout.GetResolvePreviousCardRect(out int px, out _, out int pw, out _);
            TestBase.AssertTrue(px + pw <= cx,
                "past-action square sits to the left of the resolving card",
                ref run, ref passed, ref failed);
        }

        private static void TestEnemyPreviousCardSitsRightOfCurrent(ref int run, ref int passed, ref int failed)
        {
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            CombatArenaHudLayout.GetResolveCurrentCardRect(out int cx, out _, out int cw, out _);
            CombatArenaHudLayout.GetResolveEnemyPreviousCardRect(out int ex, out _, out _, out _);
            TestBase.AssertTrue(ex >= cx + cw,
                "enemy past-action square sits to the right of the resolving card",
                ref run, ref passed, ref failed);
        }

        private static void TestMissMarksCurrentAfterReveal(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                FighterResolveActionStackState.ApplyResolveOutcome(isCombo: false, isCritical: false);
                TestBase.AssertFalse(FighterResolveActionStackState.CurrentMissed,
                    "unused stamp waits until the centered card is revealed",
                    ref run, ref passed, ref failed);

                FighterResolveActionStackState.RevealCurrent();
                TestBase.AssertTrue(FighterResolveActionStackState.CurrentMissed,
                    "revealed miss shows the red X on the centered card",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestPlainHitMarksUnusedX(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                FighterResolveActionStackState.ApplyResolveOutcome(isCombo: false, isCritical: false);
                TestBase.AssertTrue(FighterResolveActionStackState.Current.HasValue,
                    "instant HUD reveals the card on a plain hit",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(FighterResolveActionStackState.CurrentMissed,
                    "a plain hit stamps a red X because the combo action was not used",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestComboDoesNotMarkX(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                FighterResolveActionStackState.ApplyResolveOutcome(isCombo: true, isCritical: false);
                TestBase.AssertFalse(FighterResolveActionStackState.CurrentMissed,
                    "a combo does not stamp a red X",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestCritDoesNotMarkX(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                FighterResolveActionStackState.ApplyResolveOutcome(isCombo: false, isCritical: true);
                TestBase.AssertFalse(FighterResolveActionStackState.CurrentMissed,
                    "a crit does not stamp a red X",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestMissArchivesWithPrevious(ref int run, ref int passed, ref int failed)
        {
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                FighterResolveActionStackState.ResetForTests();

                var hero = HeroWithSlam();
                FighterResolveActionStackState.BeginResolve(hero, hero.GetComboActions()[0]);
                FighterResolveActionStackState.MarkCurrentMissed();
                FighterResolveActionStackState.ArchiveCurrent();

                TestBase.AssertFalse(FighterResolveActionStackState.CurrentMissed,
                    "archiving clears the centered miss stamp",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(FighterResolveActionStackState.PreviousMissed,
                    "fighter past-action keeps the miss X after archive",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }
    }
}

