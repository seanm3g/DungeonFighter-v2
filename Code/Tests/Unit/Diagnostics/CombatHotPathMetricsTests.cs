using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Diagnostics;
using RPGGame.Simulation;
using RPGGame.SimulationTest;

namespace RPGGame.Tests.Unit.Diagnostics
{
    /// <summary>
    /// Verifies opt-in combat profiling collects samples during headless simulation.
    /// </summary>
    public static class CombatHotPathMetricsTests
    {
        private static int testsRun;
        private static int testsPassed;

        public static void RunAllTests()
        {
            testsRun = 0;
            testsPassed = 0;

            TestCollectsSamplesDuringSimulation();
            TestCalculateDamageRecordsWhenEnabled();

            Console.WriteLine($"CombatHotPathMetricsTests: {testsPassed}/{testsRun} passed.");
        }

        private static void TestCollectsSamplesDuringSimulation()
        {
            testsRun++;
            CombatHotPathMetrics.Reset();
            CombatHotPathMetrics.IsEnabled = true;
            try
            {
                var player = SimulationTestHarness.CreateTestPlayer();
                var enemies = SimulationTestHarness.CreateTestEnemies();
                BatchSimulationRunner.RunSimulationBatch(player, enemies, 1, "metrics smoke");
            }
            finally
            {
                CombatHotPathMetrics.IsEnabled = false;
            }

            bool ok = CombatHotPathMetrics.FormatReport().Contains("ActionExecutionFlow.Execute");
            if (ok) testsPassed++;
            else
                Console.WriteLine("FAIL: expected baseline report to mention ActionExecutionFlow.Execute");
        }

        private static void TestCalculateDamageRecordsWhenEnabled()
        {
            testsRun++;
            CombatHotPathMetrics.Reset();
            CombatHotPathMetrics.IsEnabled = true;
            try
            {
                var attacker = new Character("A", 1);
                var defender = new Enemy("D", 1, 40, 5, 5, 5, 5);
                var action = new Action("Strike", ActionType.Attack, TargetType.SingleTarget, 0, "hit");
                DamageCalculator.CalculateDamage(attacker, defender, action, 1.0, 1.0, 0, 10);
            }
            finally
            {
                CombatHotPathMetrics.IsEnabled = false;
            }

            bool ok = CombatHotPathMetrics.FormatReport().Contains("DamageCalculator.CalculateDamage");
            if (ok) testsPassed++;
            else
                Console.WriteLine("FAIL: expected report to mention DamageCalculator.CalculateDamage");
        }
    }
}
