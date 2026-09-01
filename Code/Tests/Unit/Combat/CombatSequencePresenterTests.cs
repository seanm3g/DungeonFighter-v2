using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Combat.Sequence;
using RPGGame.Combat.UI;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    public static class CombatSequencePresenterTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatSequencePresenter Tests ===\n");
            _run = _passed = _failed = 0;

            TestShouldPlayFalseWhenMuted();
            TestShouldPlayFalseWhenInstant();
            TestMutedPlaySkipsAndClearsPending();
            TestPlayPendingOrWaitPlaysHudWhenPending();
            TestPlayPendingOrWaitDoesNotMarkPlayedWhenEmpty();
            TestCueOrderOutcomeBeforeDamage();
            TestHealthHoldReleasedOnDamageCue();
            TestLayoutHudBetweenStripAndLog();
            TestShouldReserveBandInDungeon();
            TestIdleColumnsAreTheDungeonHeaders();
            TestInvalidateDoesNotInvokeSynchronouslyOnBackgroundThread();
            TestHorizontalStripShowsAllTitles();
            TestActiveStepIsHighlightedUntilReveal();
            TestCompletedColumnsKeepResults();
            TestFinishSequenceMarksEveryColumnComplete();
            TestMathBeatsFireCueOnLastPiece();
            TestManualPlaybackAdvancesOneBeatPerClick();
            TestManualPlaybackCancelUnblocksPlayPending();

            CombatSequencePresenter.ResetForTests();
            TestBase.PrintSummary("CombatSequencePresenter Tests", _run, _passed, _failed);
        }

        private static void TestShouldPlayFalseWhenMuted()
        {
            Console.WriteLine("--- ShouldPlay is false when muted ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = true;
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                TestBase.AssertTrue(!CombatSequencePresenter.ShouldPlay(),
                    "muted combat does not play HUD", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestShouldPlayFalseWhenInstant()
        {
            Console.WriteLine("--- ShouldPlay is false when combat log is instant ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                TestBase.AssertTrue(!CombatSequencePresenter.ShouldPlay(),
                    "instant log does not play HUD", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestMutedPlaySkipsAndClearsPending()
        {
            Console.WriteLine("--- Muted PlayPendingAsync skips and clears pending ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = true;
                var steps = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Outcome, "OUTCOME",
                        new ColoredTextBuilder().Add("HIT", ColorPalette.Success).Build(),
                        CombatSequenceCue.StripFlashAndSfx)
                };
                CombatSequencePresenter.SetPending(steps);
                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();
                TestBase.AssertTrue(!CombatSequencePresenter.PlayedThisBlock,
                    "muted play does not mark PlayedThisBlock", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestPlayPendingOrWaitPlaysHudWhenPending()
        {
            Console.WriteLine("--- PlayPendingOrWait plays HUD instead of waiting when pending ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.SetPending(new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Outcome, "OUTCOME",
                        new ColoredTextBuilder().Add("HIT", ColorPalette.Success).Build())
                });
                CombatSequencePresenter.PlayPendingOrWaitAsync(5000).GetAwaiter().GetResult();
                TestBase.AssertTrue(CombatSequencePresenter.PlayedThisBlock,
                    "pending HUD plays between setup and punchline", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestPlayPendingOrWaitDoesNotMarkPlayedWhenEmpty()
        {
            Console.WriteLine("--- PlayPendingOrWait does not mark played when nothing is pending ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.PlayPendingOrWaitAsync(5000).GetAwaiter().GetResult();
                TestBase.AssertTrue(!CombatSequencePresenter.PlayedThisBlock,
                    "no pending HUD does not mark PlayedThisBlock", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestCueOrderOutcomeBeforeDamage()
        {
            Console.WriteLine("--- Cue order: Outcome then Damage ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.RecordCuesForTests = true;
                CombatSequencePresenter.CuesFiredForTests.Clear();

                var steps = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION",
                        new ColoredTextBuilder().Add("STRIKE", ColorPalette.Success).Build()),
                    new CombatSequenceStep(CombatSequenceStepKind.Outcome, "OUTCOME",
                        new ColoredTextBuilder().Add("HIT", ColorPalette.Success).Build(),
                        CombatSequenceCue.StripFlashAndSfx),
                    new CombatSequenceStep(CombatSequenceStepKind.Damage, "DAMAGE",
                        new ColoredTextBuilder().Add("9", ColorPalette.Damage).Build(),
                        CombatSequenceCue.HealthBar)
                };
                CombatSequencePresenter.SetPending(steps);
                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();

                TestBase.AssertTrue(CombatSequencePresenter.PlayedThisBlock,
                    "play marks PlayedThisBlock", ref _run, ref _passed, ref _failed);
                var cues = CombatSequencePresenter.CuesFiredForTests.FindAll(c => c != CombatSequenceCue.None);
                TestBase.AssertTrue(cues.Count >= 2, "outcome and damage cues fired", ref _run, ref _passed, ref _failed);
                if (cues.Count >= 2)
                {
                    TestBase.AssertTrue(cues[0] == CombatSequenceCue.StripFlashAndSfx,
                        "first non-none cue is strip/SFX", ref _run, ref _passed, ref _failed);
                    TestBase.AssertTrue(cues[1] == CombatSequenceCue.HealthBar,
                        "second non-none cue is health bar", ref _run, ref _passed, ref _failed);
                }
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestHealthHoldReleasedOnDamageCue()
        {
            Console.WriteLine("--- Health hold released on DAMAGE cue ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;

                const string id = "enemy_HoldFoe";
                var steps = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Damage, "DAMAGE",
                        new ColoredTextBuilder().Add("3", ColorPalette.Damage).Build(),
                        CombatSequenceCue.HealthBar)
                };
                CombatSequencePresenter.SetPending(steps, new List<(string, int)> { (id, 40) });
                TestBase.AssertTrue(HealthBarDisplayHold.TryGet(id, out int held) && held == 40,
                    "hold applied when pending is set", ref _run, ref _passed, ref _failed);

                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();
                TestBase.AssertTrue(!HealthBarDisplayHold.TryGet(id, out _),
                    "hold released after damage cue", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestLayoutHudBetweenStripAndLog()
        {
            Console.WriteLine("--- HUD rect sits between strip and combat log ---");
            LayoutConstantsRestore(() =>
            {
                RPGGame.UI.Avalonia.Layout.LayoutConstants.UpdateGridDimensions(210, 52);
                RPGGame.UI.Avalonia.Layout.LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
                CombatSequenceHudState.IsBandReserved = true;

                int stripBottom = RPGGame.UI.Avalonia.Layout.LayoutConstants.ACTION_INFO_Y
                    + RPGGame.UI.Avalonia.Layout.LayoutConstants.ACTION_INFO_HEIGHT;
                int hudY = RPGGame.UI.Avalonia.Layout.LayoutConstants.CombatSequenceHudY;
                int panelY = RPGGame.UI.Avalonia.Layout.LayoutConstants.CombatSequencePanelY;
                int logFrameY = RPGGame.UI.Avalonia.Layout.LayoutConstants.CENTER_PANEL_Y;
                int logY = RPGGame.UI.Avalonia.Layout.LayoutConstants.CombatLogContentY;
                int centerX = RPGGame.UI.Avalonia.Layout.LayoutConstants.CENTER_PANEL_X + 1;

                TestBase.AssertTrue(hudY == stripBottom && panelY == stripBottom,
                    "sequence panel sits flush under the action strip", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    logFrameY == hudY + RPGGame.UI.Avalonia.Layout.LayoutConstants.COMBAT_SEQUENCE_HUD_HEIGHT
                        + RPGGame.UI.Avalonia.Layout.LayoutConstants.COMBAT_SEQUENCE_LOG_GAP,
                    "combat log frame sits one row below the sequence panel", ref _run, ref _passed, ref _failed);
                int gapY = hudY + RPGGame.UI.Avalonia.Layout.LayoutConstants.COMBAT_SEQUENCE_HUD_HEIGHT;
                TestBase.AssertTrue(
                    !RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCombatSequenceHud(centerX, gapY)
                    && !RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCenterPanelContent(centerX, gapY),
                    "padding row between HUD and log is not either panel", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(logY == logFrameY + 1,
                    "log text starts inside the log frame", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCombatSequenceHud(centerX, hudY),
                    "HUD row is the sequence HUD", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    !RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCenterPanelContent(centerX, hudY),
                    "sequence panel is not the combat log frame", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    !RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCombatLogScrollRegion(centerX, hudY),
                    "HUD row is not the scrollable log", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCombatLogScrollRegion(centerX, logY),
                    "log row is scrollable", ref _run, ref _passed, ref _failed);

                CombatSequenceHudState.IsBandReserved = false;
                TestBase.AssertTrue(
                    !RPGGame.UI.Avalonia.Layout.LayoutConstants.ContainsCombatSequenceHud(centerX, hudY),
                    "HUD hit is off when band is not reserved", ref _run, ref _passed, ref _failed);
            });
        }

        private static void TestShouldReserveBandInDungeon()
        {
            Console.WriteLine("--- Sequence HUD band is dungeon chrome, not combat-only ---");
            TestBase.AssertTrue(CombatSequenceHudState.ShouldReserveBand(GameState.Dungeon),
                "dungeon exploration reserves the HUD", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CombatSequenceHudState.ShouldReserveBand(GameState.Combat),
                "combat keeps the same HUD", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CombatSequenceHudState.ShouldReserveBand(GameState.ActionInteractionLab),
                "Action Lab reserves the HUD", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceHudState.ShouldReserveBand(GameState.MainMenu),
                "main menu does not show the HUD", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceHudState.ShouldReserveBand(GameState.DungeonSelection),
                "dungeon selection does not show the HUD", ref _run, ref _passed, ref _failed);
        }

        private static void TestIdleColumnsAreTheDungeonHeaders()
        {
            Console.WriteLine("--- Idle HUD headers are ATTACKER through EFFECTS ---");
            var titles = CombatSequenceHudLayout.ColumnTitles;
            TestBase.AssertEqual(7, titles.Length, "seven dungeon HUD columns", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(titles[0] == "ATTACKER" && titles[1] == "ROLL" && titles[2] == "OUTCOME"
                && titles[3] == "ACTION" && titles[4] == "DEFENSE" && titles[5] == "DAMAGE"
                && titles[6] == "EFFECTS",
                "column order is ATTACKER, ROLL, OUTCOME, ACTION, DEFENSE, DAMAGE, EFFECTS",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(5, CombatSequenceHudLayout.ColumnIndexFor(CombatSequenceStepKind.Heal),
                "HEAL occupies the DAMAGE column", ref _run, ref _passed, ref _failed);
        }

        private static void TestInvalidateDoesNotInvokeSynchronouslyOnBackgroundThread()
        {
            Console.WriteLine("--- HUD invalidate does not run inline on a background thread ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool ranInline = false;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.SetRequestInvalidate(() => ranInline = true);

                var steps = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION",
                        new ColoredTextBuilder().Add("SLAM", ColorPalette.Success).Build())
                };
                CombatSequencePresenter.SetPending(steps);
                Task.Run(() => CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult())
                    .GetAwaiter().GetResult();

                TestBase.AssertTrue(!ranInline,
                    "HUD paint must be posted, not invoked on the combat thread",
                    ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.SetRequestInvalidate(null);
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestHorizontalStripShowsAllTitles()
        {
            Console.WriteLine("--- Horizontal HUD shows every step title ---");
            var columns = CombatSequenceHudLayout.MeasureColumns(0, 80, CombatSequenceHudLayout.ColumnTitles.Length);
            TestBase.AssertTrue(columns.Length == 7,
                "one column per dungeon HUD header", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(columns[0].X == 0, "first column starts at origin", ref _run, ref _passed, ref _failed);
            for (int i = 1; i < columns.Length; i++)
            {
                TestBase.AssertTrue(
                    columns[i].X == columns[i - 1].X + columns[i - 1].Width + CombatSequenceHudLayout.SeparatorWidth,
                    $"column {i} sits after the previous title and separator",
                    ref _run, ref _passed, ref _failed);
            }

            string joined = "";
            for (int i = 0; i < CombatSequenceHudLayout.ColumnTitles.Length; i++)
                joined += CombatSequenceHudLayout.FormatTitle(CombatSequenceHudLayout.ColumnTitles[i], CombatSequenceHudLayout.Phase.Pending, columns[i].Width) + " ";
            TestBase.AssertTrue(joined.Contains("ATTACKER", StringComparison.Ordinal)
                && joined.Contains("ROLL", StringComparison.Ordinal)
                && joined.Contains("OUTCOME", StringComparison.Ordinal)
                && joined.Contains("ACTION", StringComparison.Ordinal)
                && joined.Contains("DEFENSE", StringComparison.Ordinal)
                && joined.Contains("DAMAGE", StringComparison.Ordinal)
                && joined.Contains("EFFECTS", StringComparison.Ordinal),
                "all dungeon HUD headers are laid out horizontally", ref _run, ref _passed, ref _failed);
        }

        private static void TestActiveStepIsHighlightedUntilReveal()
        {
            Console.WriteLine("--- Current step is highlighted; result waits for reveal ---");
            var steps = SampleSwingSteps();
            int roll = 1;
            TestBase.AssertTrue(
                CombatSequenceHudLayout.GetPhase(roll, currentIndex: roll) == CombatSequenceHudLayout.Phase.Active,
                "current index is Active", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatSequenceHudLayout.TitlePalette(CombatSequenceHudLayout.Phase.Active) == ColorPalette.Gold,
                "active title is gold", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatSequenceHudLayout.TitlePalette(CombatSequenceHudLayout.Phase.Pending) == ColorPalette.DarkGray,
                "pending title is dim", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatSequenceHudLayout.FormatTitle("ROLL", CombatSequenceHudLayout.Phase.Active, 10) == "[ROLL]",
                "active title is bracket-highlighted when it fits", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !CombatSequenceHudLayout.ShowsResult(CombatSequenceHudLayout.Phase.Active, resultRevealed: false),
                "active result is hidden until the beat reveals it", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatSequenceHudLayout.ShowsResult(CombatSequenceHudLayout.Phase.Active, resultRevealed: true),
                "active result appears on reveal", ref _run, ref _passed, ref _failed);

            var hidden = CombatSequenceHudLayout.FormatResult(steps[roll].Result,
                CombatSequenceHudLayout.Phase.Active, resultRevealed: false, width: 12);
            var shown = CombatSequenceHudLayout.FormatResult(steps[roll].Result,
                CombatSequenceHudLayout.Phase.Active, resultRevealed: true, width: 12);
            TestBase.AssertTrue(hidden.Count == 0, "unrevealed ROLL cell is empty", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ColoredTextRenderer.RenderAsPlainText(shown).Contains("14", StringComparison.Ordinal),
                "revealed ROLL cell shows the face", ref _run, ref _passed, ref _failed);
        }

        private static void TestCompletedColumnsKeepResults()
        {
            Console.WriteLine("--- Completed columns keep their results ---");
            var steps = SampleSwingSteps();
            var actionStep = steps.First(s => s.Kind == CombatSequenceStepKind.Action);
            var action = CombatSequenceHudLayout.FormatResult(actionStep.Result,
                CombatSequenceHudLayout.Phase.Complete, resultRevealed: false, width: 12);
            TestBase.AssertTrue(
                CombatSequenceHudLayout.GetPhase(0, currentIndex: 2) == CombatSequenceHudLayout.Phase.Complete,
                "earlier columns are complete while a later step is active", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ColoredTextRenderer.RenderAsPlainText(action).Contains("SLAM", StringComparison.Ordinal),
                "completed ACTION still shows SLAM", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                CombatSequenceHudLayout.TitlePalette(CombatSequenceHudLayout.Phase.Complete) == ColorPalette.White,
                "completed title is white", ref _run, ref _passed, ref _failed);
        }

        private static void TestFinishSequenceMarksEveryColumnComplete()
        {
            Console.WriteLine("--- FinishSequence leaves every column complete ---");
            try
            {
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequenceHudState.Begin(SampleSwingSteps());
                CombatSequenceHudState.SetActive(0, resultRevealed: true);
                CombatSequenceHudState.FinishSequence();
                TestBase.AssertTrue(CombatSequenceHudState.CurrentIndex == CombatSequenceHudState.Steps.Count,
                    "finished index is past the last step", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    CombatSequenceHudLayout.GetPhase(0, CombatSequenceHudState.CurrentIndex) == CombatSequenceHudLayout.Phase.Complete
                    && CombatSequenceHudLayout.GetPhase(3, CombatSequenceHudState.CurrentIndex) == CombatSequenceHudLayout.Phase.Complete,
                    "every column is complete after finish", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequenceHudState.HasVisibleSequence,
                    "finished strip stays visible until the next swing", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                CombatSequenceHudState.ResetForTests();
            }
        }

        private static void TestMathBeatsFireCueOnLastPiece()
        {
            Console.WriteLine("--- Math beats fire the column cue once, on the last piece ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.RecordCuesForTests = true;
                CombatSequencePresenter.CuesFiredForTests.Clear();

                var beats = new List<List<ColoredText>>
                {
                    new ColoredTextBuilder().Add("base 23", ColorPalette.Info).Build(),
                    new ColoredTextBuilder().Add("×1.18", ColorPalette.Success).Build(),
                    new ColoredTextBuilder().Add("27", ColorPalette.Damage).Build()
                };
                var steps = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Damage, "DAMAGE", beats[2],
                        CombatSequenceCue.HealthBar, beats)
                };
                CombatSequencePresenter.SetPending(steps);
                CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();
                var cues = CombatSequencePresenter.CuesFiredForTests.FindAll(c => c != CombatSequenceCue.None);
                TestBase.AssertTrue(cues.Count == 1 && cues[0] == CombatSequenceCue.HealthBar,
                    "HP cue fires once after the last damage math beat", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestManualPlaybackAdvancesOneBeatPerClick()
        {
            Console.WriteLine("--- Piece mode reveals one math beat per TryAdvanceManualBeat ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.UseManualPlayback = true;

                var beatA = new ColoredTextBuilder().Add("14", ColorPalette.Info).Build();
                var beatB = new ColoredTextBuilder().Add("+2 → 16", ColorPalette.Success).Build();
                var steps = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Roll, "ROLL", beatB,
                        CombatSequenceCue.None, new List<List<ColoredText>> { beatA, beatB })
                };
                CombatSequencePresenter.SetPending(steps);
                var play = Task.Run(() => CombatSequencePresenter.PlayPendingAsync());
                TestBase.AssertTrue(WaitUntilManualWaiting(),
                    "first piece is waiting after setup", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequenceHudState.ResultRevealed,
                    "first formula piece is revealed without an empty title pause", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    ColoredTextRenderer.RenderAsPlainText(CombatSequenceHudState.VisibleResult.ToList())
                        .Contains("14", StringComparison.Ordinal),
                    "visible result is the first ROLL piece", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequencePresenter.TryAdvanceManualBeat(),
                    "advance consumes the waiter", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    WaitUntilVisibleContains("16"),
                    "second piece shows the roll total", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequencePresenter.IsManualPlaybackWaiting,
                    "second piece waits for another Step", ref _run, ref _passed, ref _failed);
                CombatSequencePresenter.FlushRemainingManualBeats();
                TestBase.AssertTrue(play.Wait(TimeSpan.FromSeconds(2)),
                    "flush completes PlayPendingAsync", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestManualPlaybackCancelUnblocksPlayPending()
        {
            Console.WriteLine("--- CancelManualPlayback unblocks PlayPendingAsync ---");
            bool prevMute = CombatUiMuteScope.GlobalMute;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                CombatSequencePresenter.SkipDelaysForTests = true;
                CombatSequencePresenter.UseManualPlayback = true;
                CombatSequencePresenter.SetPending(SampleSwingSteps());
                var play = Task.Run(() => CombatSequencePresenter.PlayPendingAsync());
                TestBase.AssertTrue(WaitUntilManualWaiting(),
                    "manual play is waiting", ref _run, ref _passed, ref _failed);
                CombatSequencePresenter.CancelManualPlayback();
                TestBase.AssertTrue(play.Wait(TimeSpan.FromSeconds(2)),
                    "cancel completes PlayPendingAsync", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static bool WaitUntilManualWaiting(int timeoutMs = 2000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (CombatSequencePresenter.IsManualPlaybackWaiting)
                    return true;
                System.Threading.Thread.Sleep(10);
            }
            return false;
        }

        private static bool WaitUntilVisibleContains(string fragment, int timeoutMs = 2000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                string visible = ColoredTextRenderer.RenderAsPlainText(
                    CombatSequenceHudState.VisibleResult.ToList());
                if (visible.Contains(fragment, StringComparison.Ordinal)
                    && CombatSequencePresenter.IsManualPlaybackWaiting)
                    return true;
                System.Threading.Thread.Sleep(10);
            }
            return false;
        }

        private static List<CombatSequenceStep> SampleSwingSteps()
        {
            return new List<CombatSequenceStep>
            {
                new CombatSequenceStep(CombatSequenceStepKind.Attacker, "ATTACKER",
                    new ColoredTextBuilder().Add("Sage", ColorPalette.White).Build()),
                new CombatSequenceStep(CombatSequenceStepKind.Roll, "ROLL",
                    new ColoredTextBuilder().Add("14", ColorPalette.Info).Build()),
                new CombatSequenceStep(CombatSequenceStepKind.Outcome, "OUTCOME",
                    new ColoredTextBuilder().Add("HIT", ColorPalette.Success).Build(),
                    CombatSequenceCue.StripFlashAndSfx),
                new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION",
                    new ColoredTextBuilder().Add("SLAM", ColorPalette.Success).Build()),
                new CombatSequenceStep(CombatSequenceStepKind.Damage, "DAMAGE",
                    new ColoredTextBuilder().Add("27", ColorPalette.Damage).Build(),
                    CombatSequenceCue.HealthBar)
            };
        }

        private static void LayoutConstantsRestore(System.Action body)
        {
            bool reserved = CombatSequenceHudState.IsBandReserved;
            try
            {
                body();
            }
            finally
            {
                CombatSequenceHudState.IsBandReserved = reserved;
            }
        }
    }
}
