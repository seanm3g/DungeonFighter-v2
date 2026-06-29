using System;
using System.IO;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class LevelTuningSessionStoreTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== LevelTuningSessionStore Tests ===\n");
            _run = _pass = _fail = 0;

            TestRoundTrip();
            TestEmptyLoad();

            TestBase.PrintSummary("LevelTuningSessionStore Tests", _run, _pass, _fail);
        }

        private static void TestRoundTrip()
        {
            Console.WriteLine("--- Session round-trip ---");
            string path = Path.Combine(Path.GetTempPath(), $"level-tuning-session-test-{Guid.NewGuid():N}.json");
            try
            {
                var session = new LevelTuningSession
                {
                    Simulation = new SimulationSessionDto
                    {
                        OverallCurveScore = 77.5,
                        WorstLevel = 100,
                        WorstDeltaMagnitude = 42.6,
                        BattlesPerCombination = 25,
                        Snapshots =
                        {
                            new SnapshotDto { Level = 1, TargetWinRate = 100, ActualWinRate = 94, WithinTolerance = false }
                        }
                    }
                };

                LevelTuningSessionStore.Save(session, path);
                var loaded = LevelTuningSessionStore.Load(path);
                var result = LevelTuningSessionStore.ToSimulationResult(loaded);

                TestBase.AssertTrue(Math.Abs(loaded.Simulation!.OverallCurveScore - 77.5) < 0.01,
                    "Score persisted", ref _run, ref _pass, ref _fail);
                TestBase.AssertEqual(100, loaded.Simulation.WorstLevel,
                    "Worst level", ref _run, ref _pass, ref _fail);
                TestBase.AssertEqual(1, result.LevelSnapshots.Count,
                    "Snapshot count", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static void TestEmptyLoad()
        {
            Console.WriteLine("--- Empty session load ---");
            string path = Path.Combine(Path.GetTempPath(), $"missing-session-{Guid.NewGuid():N}.json");
            var loaded = LevelTuningSessionStore.Load(path);
            TestBase.AssertTrue(loaded.Simulation == null,
                "Missing file yields empty session", ref _run, ref _pass, ref _fail);
        }
    }
}
