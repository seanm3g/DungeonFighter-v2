using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="StatsPanelStateManager"/> HUD section collapse toggles.
    /// </summary>
    public static class StatsPanelStateManagerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== StatsPanelStateManager Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestDefaultsAllSectionsOpen(ref run, ref passed, ref failed);
            TestToggleHeroCollapsed(ref run, ref passed, ref failed);
            TestToggleStatsCollapsed(ref run, ref passed, ref failed);
            TestToggleGearCollapsed(ref run, ref passed, ref failed);
            TestToggleThresholdsCollapsed(ref run, ref passed, ref failed);
            TestToggleThresholdsDisplayMode(ref run, ref passed, ref failed);
            TestChancesFlashClearsWhenLeavingChancesView(ref run, ref passed, ref failed);
            TestCycleActionStripDamageLineMode(ref run, ref passed, ref failed);

            TestBase.PrintSummary("StatsPanelStateManager Tests", run, passed, failed);
        }

        private static void TestDefaultsAllSectionsOpen(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            TestBase.AssertTrue(
                !m.HeroCollapsed && !m.StatsCollapsed && !m.GearCollapsed && !m.ThresholdsCollapsed,
                "New manager: all section collapse flags false (open)",
                ref run, ref passed, ref failed);
        }

        private static void TestToggleHeroCollapsed(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            m.ToggleHeroCollapsed();
            TestBase.AssertTrue(m.HeroCollapsed, "ToggleHeroCollapsed sets HeroCollapsed", ref run, ref passed, ref failed);
            m.ToggleHeroCollapsed();
            TestBase.AssertTrue(!m.HeroCollapsed, "Second toggle clears HeroCollapsed", ref run, ref passed, ref failed);
        }

        private static void TestToggleStatsCollapsed(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            m.ToggleStatsCollapsed();
            TestBase.AssertTrue(m.StatsCollapsed, "ToggleStatsCollapsed", ref run, ref passed, ref failed);
        }

        private static void TestToggleGearCollapsed(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            m.ToggleGearCollapsed();
            TestBase.AssertTrue(m.GearCollapsed, "ToggleGearCollapsed", ref run, ref passed, ref failed);
        }

        private static void TestToggleThresholdsCollapsed(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            m.ToggleThresholdsCollapsed();
            TestBase.AssertTrue(m.ThresholdsCollapsed, "ToggleThresholdsCollapsed", ref run, ref passed, ref failed);
        }

        private static void TestToggleThresholdsDisplayMode(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            TestBase.AssertFalse(m.ThresholdsShowChances, "ThresholdsShowChances defaults to numbers", ref run, ref passed, ref failed);
            m.ToggleThresholdsDisplayMode();
            TestBase.AssertTrue(m.ThresholdsShowChances, "ToggleThresholdsDisplayMode enables chance view", ref run, ref passed, ref failed);
            m.ToggleThresholdsDisplayMode();
            TestBase.AssertFalse(m.ThresholdsShowChances, "ToggleThresholdsDisplayMode returns to number view", ref run, ref passed, ref failed);
        }

        private static void TestChancesFlashClearsWhenLeavingChancesView(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            m.ToggleThresholdsDisplayMode();
            m.BeginThresholdsChancesModeFlash(TimeSpan.FromHours(1));
            TestBase.AssertTrue(m.IsThresholdsChancesFlashActive(), "BeginThresholdsChancesModeFlash activates until expiry", ref run, ref passed, ref failed);
            m.ToggleThresholdsDisplayMode();
            TestBase.AssertFalse(m.ThresholdsShowChances, "sanity: back to ladder", ref run, ref passed, ref failed);
            TestBase.AssertFalse(m.IsThresholdsChancesFlashActive(), "leaving CHANCES view clears flash timer", ref run, ref passed, ref failed);
        }

        private static void TestCycleActionStripDamageLineMode(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            TestBase.AssertEqual(
                (int)ActionStripDamageLineMode.EffectiveWithComboAmp,
                (int)m.ActionStripDamageLineMode,
                "ActionStripDamageLineMode defaults to EffectiveWithComboAmp",
                ref run, ref passed, ref failed);
            m.CycleActionStripDamageLineMode();
            TestBase.AssertEqual(
                (int)ActionStripDamageLineMode.BaseIntrinsic,
                (int)m.ActionStripDamageLineMode,
                "CycleActionStripDamageLineMode switches to BaseIntrinsic",
                ref run, ref passed, ref failed);
            m.CycleActionStripDamageLineMode();
            TestBase.AssertEqual(
                (int)ActionStripDamageLineMode.EffectiveWithComboAmp,
                (int)m.ActionStripDamageLineMode,
                "Second cycle returns to EffectiveWithComboAmp",
                ref run, ref passed, ref failed);
        }
    }
}
