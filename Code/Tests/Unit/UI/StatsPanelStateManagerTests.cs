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
            TestToggleExpansionIndependent(ref run, ref passed, ref failed);

            TestBase.PrintSummary("StatsPanelStateManager Tests", run, passed, failed);
        }

        private static void TestDefaultsAllSectionsOpen(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            TestBase.AssertTrue(
                !m.HeroCollapsed && !m.StatsCollapsed && !m.GearCollapsed && !m.ThresholdsCollapsed,
                "New manager: all section collapse flags false (open)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!m.IsExpanded, "New manager: secondary stats expansion off", ref run, ref passed, ref failed);
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

        private static void TestToggleExpansionIndependent(ref int run, ref int passed, ref int failed)
        {
            var m = new StatsPanelStateManager();
            m.ToggleExpansion();
            TestBase.AssertTrue(m.IsExpanded, "ToggleExpansion sets IsExpanded", ref run, ref passed, ref failed);
            m.ToggleStatsCollapsed();
            TestBase.AssertTrue(m.StatsCollapsed && m.IsExpanded, "Section collapse independent of IsExpanded", ref run, ref passed, ref failed);
        }
    }
}
