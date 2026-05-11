using System;
using RPGGame.Diagnostics;
using RPGGame.SimulationTest;

namespace RPGGame.Simulation
{
    /// <summary>
    /// Runs headless combat simulations with <see cref="CombatHotPathMetrics"/> enabled to baseline
    /// time spent in action execution vs damage calculation.
    /// </summary>
    public static class CombatHotPathBenchmark
    {
        /// <summary>
        /// Executes a short simulation batch and returns a formatted metrics report.
        /// </summary>
        public static string RunHeadlessBaseline(int repetitionsPerEnemy = 3)
        {
            CombatHotPathMetrics.Reset();
            CombatHotPathMetrics.IsEnabled = true;
            try
            {
                var player = SimulationTestHarness.CreateTestPlayer();
                var enemies = SimulationTestHarness.CreateTestEnemies();
                BatchSimulationRunner.RunSimulationBatch(player, enemies, repetitionsPerEnemy, "Hot path baseline");
            }
            finally
            {
                CombatHotPathMetrics.IsEnabled = false;
            }

            return CombatHotPathMetrics.FormatReport();
        }
    }
}
