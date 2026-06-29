using System;
using System.IO;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class BalanceDialClassifierTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BalanceDialClassifier Tests ===\n");
            _run = _pass = _fail = 0;

            TestPowerDialWhenDurationOff();
            TestVarianceDialWhenSpreadHigh();
            TestLoadCombatDialsProfile();

            TestBase.PrintSummary("BalanceDialClassifier Tests", _run, _pass, _fail);
        }

        private static void TestPowerDialWhenDurationOff()
        {
            Console.WriteLine("--- Power dial when duration off ---");
            var simulation = new TuningSimulationOutcome
            {
                Fundamentals = new FundamentalsSimulationResult
                {
                    AveragePlayerTurnsPerEncounter = 6,
                    WinRate = 0.9
                }
            };
            var analysis = new TuningAnalysis
            {
                AverageCombatDuration = 6,
                OverallWinRate = 90
            };

            var result = BalanceDialClassifier.Classify(simulation, analysis);
            TestBase.AssertTrue(result.PrimaryDial == BalanceDial.Power,
                "Short fights classify as Power", ref _run, ref _pass, ref _fail);
        }

        private static void TestVarianceDialWhenSpreadHigh()
        {
            Console.WriteLine("--- Variance dial when spread high ---");
            var simulation = new TuningSimulationOutcome
            {
                Fundamentals = new FundamentalsSimulationResult
                {
                    AveragePlayerTurnsPerEncounter = 13,
                    TurnDurationStdDev = 6,
                    AverageMissRate = 0.45,
                    AverageMaxComboStreak = 1.5,
                    WinRate = 0.9
                }
            };
            var analysis = new TuningAnalysis
            {
                AverageCombatDuration = 13,
                TurnDurationStdDev = 6,
                AverageMissRate = 0.45,
                AverageComboStreak = 1.5,
                OverallWinRate = 90
            };

            var result = BalanceDialClassifier.Classify(simulation, analysis);
            TestBase.AssertTrue(result.PrimaryDial == BalanceDial.Variance,
                "High spread classifies as Variance", ref _run, ref _pass, ref _fail);
        }

        private static void TestLoadCombatDialsProfile()
        {
            Console.WriteLine("--- Load combat-dials profile ---");
            string dir = BalanceTuningProfileLoader.ResolveProfilesDirectory();
            var profile = BalanceTuningProfileLoader.Load("combat-dials", dir);

            TestBase.AssertEqual("combat-dials", profile.Id, "Profile id", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(profile.Analysis.EnableDialRouting, "Dial routing enabled", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(profile.Simulation.ContinuePastZeroHp, "Continue past zero HP", ref _run, ref _pass, ref _fail);
        }
    }
}
