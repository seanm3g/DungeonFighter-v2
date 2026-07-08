using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Config;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;
using RPGGame.ActionInteractionLab;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class DeveloperSimModeTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DeveloperSimMode Tests ===\n");
            _run = _pass = _fail = 0;

            TestScopeRestoresFlag();
            TestScopeRestoresNegativeHpFloor();
            TestShouldContinueEncounter();
            TestDevSimSingleEncounter();

            TestBase.PrintSummary("DeveloperSimMode Tests", _run, _pass, _fail);
        }

        private static void TestScopeRestoresFlag()
        {
            Console.WriteLine("--- Scope restores flag ---");
            DeveloperSimMode.SetContinuePastZeroHp(false);
            using (DeveloperSimMode.BeginScope(true))
            {
                TestBase.AssertTrue(DeveloperSimMode.ContinuePastZeroHp, "Enabled in scope", ref _run, ref _pass, ref _fail);
            }
            TestBase.AssertFalse(DeveloperSimMode.ContinuePastZeroHp, "Restored after scope", ref _run, ref _pass, ref _fail);
        }

        private static void TestScopeRestoresNegativeHpFloor()
        {
            Console.WriteLine("--- Scope restores negative HP floor ---");
            using (DeveloperSimMode.BeginScope(true, -100))
            {
                TestBase.AssertEqual(-100, DeveloperSimMode.NegativeHpFloor, "Scoped floor", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(DeveloperSimMode.ShouldContinueEncounter(-50, 100, true),
                    "Continues above scoped floor", ref _run, ref _pass, ref _fail);
                TestBase.AssertFalse(DeveloperSimMode.ShouldContinueEncounter(-150, 100, true),
                    "Stops at scoped floor", ref _run, ref _pass, ref _fail);
            }
            TestBase.AssertEqual(-500, DeveloperSimMode.NegativeHpFloor, "Default floor restored", ref _run, ref _pass, ref _fail);
        }

        private static void TestShouldContinueEncounter()
        {
            Console.WriteLine("--- ShouldContinueEncounter ---");
            DeveloperSimMode.SetContinuePastZeroHp(false);
            TestBase.AssertFalse(DeveloperSimMode.ShouldContinueEncounter(0, 100, true),
                "Normal mode stops at 0 HP", ref _run, ref _pass, ref _fail);

            DeveloperSimMode.SetContinuePastZeroHp(true);
            TestBase.AssertTrue(DeveloperSimMode.ShouldContinueEncounter(-10, 100, true),
                "Dev mode continues above floor", ref _run, ref _pass, ref _fail);
            TestBase.AssertFalse(DeveloperSimMode.ShouldContinueEncounter(-600, 100, true),
                "Dev mode stops at floor", ref _run, ref _pass, ref _fail);
            DeveloperSimMode.SetContinuePastZeroHp(false);
        }

        private static void TestDevSimSingleEncounter()
        {
            Console.WriteLine("--- Dev sim single encounter ---");
            _ = GameConfiguration.Instance;
            RPGGame.EnemyLoader.LoadEnemies();
            var config = new SimulationProfileConfig { PlayerLevel = 10, EnemyLevel = 10 };
            var snapshot = FundamentalsCombatSetup.BuildSnapshot(config);
            using (DeveloperSimMode.BeginScope(true))
            {
                var metrics = ActionLabEncounterSimulator
                    .RunSingleEncounterAsync(snapshot, new Random(42)).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(metrics.ErrorMessage))
                    Console.WriteLine($"  Error: {metrics.ErrorMessage}");
                TestBase.AssertTrue(string.IsNullOrEmpty(metrics.ErrorMessage),
                    $"Dev sim encounter runs ({metrics.ErrorMessage})", ref _run, ref _pass, ref _fail);
            }
        }
    }
}
