using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="ActionBonusBorderShimmer"/>.
    /// </summary>
    public static class ActionBonusBorderShimmerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionBonusBorderShimmer Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestCueRequiresPendingBuffNotGrantingAction(ref run, ref passed, ref failed);
            TestCueTrueForSlotQueuedBuff(ref run, ref passed, ref failed);
            TestCueTrueForBankOnCurrentStepOnly(ref run, ref passed, ref failed);
            TestCueBankStaysOnStickySlotAfterComboStepReset(ref run, ref passed, ref failed);
            TestCueFalseWhenNoPending(ref run, ref passed, ref failed);
            TestBorderColorLerpsUnselected(ref run, ref passed, ref failed);
            TestSelectedBorderUsesWhiteEnd(ref run, ref passed, ref failed);
            TestWaveTInUnitInterval(ref run, ref passed, ref failed);
            TestPerimeterCellCount(ref run, ref passed, ref failed);
            TestTravelHighlightCount(ref run, ref passed, ref failed);
            TestTravelHighlightMovesWithTime(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionBonusBorderShimmer Tests", run, passed, failed);
        }

        private static Character CreateHeroWithTwoComboSlots(out Action grantAction)
        {
            var hero = TestDataBuilders.Character().WithName("ShimmerHero").WithStats(10, 10, 10, 10).Build();
            grantAction = TestDataBuilders.CreateMockAction("ShimmerGrant", ActionType.Attack);
            grantAction.IsComboAction = true;
            grantAction.ActionAttackBonuses = new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "ACTION",
                        Count = 1,
                        Bonuses = new List<ActionAttackBonusItem>
                        {
                            new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = 1 }
                        }
                    }
                }
            };
            var followUp = TestDataBuilders.CreateMockAction("ShimmerFollow", ActionType.Attack);
            followUp.IsComboAction = true;
            hero.AddAction(grantAction, 1.0);
            hero.AddAction(followUp, 1.0);
            hero.Actions.AddToCombo(grantAction);
            hero.Actions.AddToCombo(followUp);
            hero.ComboStep = 0;
            return hero;
        }

        private static void TestCueRequiresPendingBuffNotGrantingAction(ref int run, ref int passed, ref int failed)
        {
            var hero = CreateHeroWithTwoComboSlots(out var grant);
            TestBase.AssertTrue(
                Action.HasActionAttackBonusGroups(grant),
                "Setup: granting action has authored ActionAttackBonuses",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 0),
                "Cue false on granting action with no pending buff applied",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 1),
                "Cue false on follow-up before buff is applied",
                ref run, ref passed, ref failed);
        }

        private static void TestCueTrueForSlotQueuedBuff(ref int run, ref int passed, ref int failed)
        {
            var hero = CreateHeroWithTwoComboSlots(out _);
            hero.Effects.AddPendingActionBonuses(1, new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = 1 }
            });

            TestBase.AssertTrue(
                ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 1),
                "Cue true on slot with queued pending Multihit",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 0),
                "Cue still false on granting slot without its own pending buff",
                ref run, ref passed, ref failed);
        }

        private static void TestCueTrueForBankOnCurrentStepOnly(ref int run, ref int passed, ref int failed)
        {
            var hero = CreateHeroWithTwoComboSlots(out _);
            hero.ComboStep = 1;
            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 25 }
            }, previewSlot: 1);

            TestBase.AssertTrue(
                ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 1),
                "Cue true on current combo step when ACTION bank is pending",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 0),
                "Cue false on non-current step while bank applies to current only",
                ref run, ref passed, ref failed);
        }

        private static void TestCueBankStaysOnStickySlotAfterComboStepReset(ref int run, ref int passed, ref int failed)
        {
            var hero = CreateHeroWithTwoComboSlots(out _);
            hero.ComboStep = 1;
            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 25 }
            }, previewSlot: 1);
            hero.ComboStep = 0;

            TestBase.AssertTrue(
                ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 1),
                "After miss ComboStep reset, cue stays on sticky recipient slot",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(hero, 0),
                "After miss ComboStep reset, cue does not move to slot 0",
                ref run, ref passed, ref failed);
        }

        private static void TestCueFalseWhenNoPending(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(null, 0),
                "Cue false for null character",
                ref run, ref passed, ref failed);

            var empty = TestDataBuilders.Character().WithName("NoCombo").Build();
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(empty, 0),
                "Cue false with empty combo and no bank",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionBonusBorderShimmer.SlotHasPendingBonusCue(empty, -1),
                "Cue false for negative slot",
                ref run, ref passed, ref failed);
        }

        private static void TestBorderColorLerpsUnselected(ref int run, ref int passed, ref int failed)
        {
            // phase such that sin≈-1 → wave near 0 → DimCyan
            var low = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Color cLow = ActionBonusBorderShimmer.GetBorderColor(isSelected: false, low);
            TestBase.AssertTrue(
                cLow.R <= ActionBonusBorderShimmer.BrightCyan.R
                && cLow.B <= ActionBonusBorderShimmer.BrightCyan.B,
                "Unselected shimmer stays within cyan range",
                ref run, ref passed, ref failed);

            var highPhaseMs = (long)(ActionBonusBorderShimmer.ColorPhaseMs * (Math.PI / 2.0));
            var high = DateTimeOffset.FromUnixTimeMilliseconds(highPhaseMs);
            Color cHigh = ActionBonusBorderShimmer.GetBorderColor(isSelected: false, high);
            TestBase.AssertTrue(
                cHigh.G >= cLow.G || cHigh.B >= cLow.B || cHigh.R >= cLow.R,
                "Shimmer brightens across phase",
                ref run, ref passed, ref failed);
        }

        private static void TestSelectedBorderUsesWhiteEnd(ref int run, ref int passed, ref int failed)
        {
            var t0 = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Color c = ActionBonusBorderShimmer.GetBorderColor(isSelected: true, t0);
            TestBase.AssertTrue(
                c.R >= 200 && c.G >= 200 && c.B >= 200,
                "Selected shimmer stays near white",
                ref run, ref passed, ref failed);
        }

        private static void TestWaveTInUnitInterval(ref int run, ref int passed, ref int failed)
        {
            for (double p = 0; p < Math.PI * 4; p += 0.2)
            {
                double t = ActionBonusBorderShimmer.GetWaveT(p);
                if (t < 0 || t > 1)
                {
                    TestBase.AssertTrue(false, $"WaveT({p}) in [0,1]", ref run, ref passed, ref failed);
                    return;
                }
            }
            TestBase.AssertTrue(true, "WaveT stays in [0,1] over several cycles", ref run, ref passed, ref failed);
        }

        private static void TestPerimeterCellCount(ref int run, ref int passed, ref int failed)
        {
            // 4x3 rect: perimeter = 2*(4+3)-4 = 10
            var cells = ActionBonusBorderShimmer.BuildPerimeterCells(0, 0, 4, 3);
            TestBase.AssertEqual(10, cells.Count, "4x3 perimeter cell count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(cells[0] == (0, 0), "Perimeter starts at top-left", ref run, ref passed, ref failed);
        }

        private static void TestTravelHighlightCount(ref int run, ref int passed, ref int failed)
        {
            // 6x4 rect: perimeter = 2*(6+4)-4 = 16; ~¾ ≈ 12 with a 4-cell gap
            var rects = ActionBonusBorderShimmer.GetTravelHighlightRects(2, 2, 6, 4, DateTimeOffset.FromUnixTimeMilliseconds(0));
            int expected = ActionBonusBorderShimmer.GetTravelHighlightLength(16);
            TestBase.AssertEqual(12, expected, "¾ of 16-cell perimeter is 12", ref run, ref passed, ref failed);
            TestBase.AssertEqual(expected, rects.Count, "Travel highlight covers ~¾ of perimeter", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                expected < 16,
                "Travel highlight leaves a gap so motion stays readable",
                ref run, ref passed, ref failed);
        }

        private static void TestTravelHighlightMovesWithTime(ref int run, ref int passed, ref int failed)
        {
            var a = ActionBonusBorderShimmer.GetTravelHighlightRects(0, 0, 5, 5, DateTimeOffset.FromUnixTimeMilliseconds(0));
            var b = ActionBonusBorderShimmer.GetTravelHighlightRects(
                0, 0, 5, 5,
                DateTimeOffset.FromUnixTimeMilliseconds((long)(ActionBonusBorderShimmer.TravelLapMs * 0.25)));
            TestBase.AssertTrue(
                a.Count > 0 && b.Count > 0 && (a[0].X != b[0].X || a[0].Y != b[0].Y),
                "Travel highlight head moves over a quarter lap",
                ref run, ref passed, ref failed);
        }
    }
}
