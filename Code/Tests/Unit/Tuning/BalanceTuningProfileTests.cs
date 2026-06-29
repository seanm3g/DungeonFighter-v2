using System;
using System.Collections.Generic;
using System.IO;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class BalanceTuningProfileTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BalanceTuningProfile Tests ===\n");
            _run = _pass = _fail = 0;

            TestLoadLevelCurveProfile();
            TestApplyDefaultsForComprehensive();
            TestWinRateCriteriaDisabled();
            TestComprehensiveRoundTrip();

            TestBase.PrintSummary("BalanceTuningProfile Tests", _run, _pass, _fail);
        }

        private static void TestLoadLevelCurveProfile()
        {
            Console.WriteLine("--- Load level-curve profile ---");
            string dir = BalanceTuningProfileLoader.ResolveProfilesDirectory();
            var profile = BalanceTuningProfileLoader.Load("level-curve", dir);

            TestBase.AssertEqual("level-curve", profile.Id, "Profile id", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(TuningSimulationModes.MultiLevelWeaponEnemy, profile.Simulation.Mode,
                "Simulation mode", ref _run, ref _pass, ref _fail);
            TestBase.AssertFalse(profile.Analysis.OptimizeWinRate,
                "Win rate optimization off by default", ref _run, ref _pass, ref _fail);
            var validators = TuningAnalysisCriteria.GetEffectiveValidators(profile.Analysis);
            TestBase.AssertTrue(validators.Contains(TuningValidatorIds.CombatDuration),
                "Uses combat_duration validator", ref _run, ref _pass, ref _fail);
            TestBase.AssertFalse(validators.Contains(TuningValidatorIds.LevelCurve),
                "Level curve validator excluded", ref _run, ref _pass, ref _fail);
        }

        private static void TestApplyDefaultsForComprehensive()
        {
            Console.WriteLine("--- Defaults for comprehensive profile ---");
            var profile = new BalanceTuningProfile
            {
                Id = "test",
                Simulation = new SimulationProfileConfig
                {
                    Mode = TuningSimulationModes.ComprehensiveWeaponEnemy
                }
            };
            BalanceTuningProfileLoader.ApplyDefaults(profile);

            TestBase.AssertFalse(profile.Analysis.OptimizeWinRate,
                "OptimizeWinRate defaults false", ref _run, ref _pass, ref _fail);
            var validators = TuningAnalysisCriteria.GetEffectiveValidators(profile.Analysis);
            TestBase.AssertTrue(validators.Contains(TuningValidatorIds.CombatDuration),
                "Default combat_duration validator", ref _run, ref _pass, ref _fail);
            TestBase.AssertFalse(validators.Contains(TuningValidatorIds.Comprehensive),
                "Comprehensive validator excluded when win rate off", ref _run, ref _pass, ref _fail);
            var suggesters = TuningAnalysisCriteria.GetEffectiveSuggesters(profile.Analysis);
            TestBase.AssertTrue(suggesters.Contains(TuningSuggesterIds.Weapon),
                "Default weapon suggester", ref _run, ref _pass, ref _fail);
            TestBase.AssertFalse(suggesters.Contains(TuningSuggesterIds.Global),
                "Global suggester excluded when win rate off", ref _run, ref _pass, ref _fail);
        }

        private static void TestWinRateCriteriaDisabled()
        {
            Console.WriteLine("--- Win-rate criteria filtering ---");
            var profile = new BalanceTuningProfile
            {
                Id = "filter-test",
                Analysis = new AnalysisProfileConfig
                {
                    OptimizeWinRate = false,
                    Validators = new List<string>
                    {
                        TuningValidatorIds.LevelCurve,
                        TuningValidatorIds.WinRate,
                        TuningValidatorIds.CombatDuration
                    },
                    Suggesters = new List<string>
                    {
                        TuningSuggesterIds.LevelCurve,
                        TuningSuggesterIds.Global,
                        TuningSuggesterIds.Duration
                    }
                }
            };

            var validators = TuningAnalysisCriteria.GetEffectiveValidators(profile.Analysis);
            TestBase.AssertEqual(1, validators.Count, "Only combat_duration remains", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(TuningValidatorIds.CombatDuration, validators[0],
                "Effective validator id", ref _run, ref _pass, ref _fail);

            var suggesters = TuningAnalysisCriteria.GetEffectiveSuggesters(profile.Analysis);
            TestBase.AssertEqual(1, suggesters.Count, "Only duration suggester remains", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(TuningSuggesterIds.Duration, suggesters[0],
                "Effective suggester id", ref _run, ref _pass, ref _fail);
        }

        private static void TestComprehensiveRoundTrip()
        {
            Console.WriteLine("--- Comprehensive mapper round-trip ---");
            var original = new BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult
            {
                OverallWinRate = 91.5,
                OverallAverageTurns = 9.2,
                TotalBattles = 400
            };
            original.WeaponStatistics[WeaponType.Sword] = new BattleStatisticsRunner.WeaponOverallStats
            {
                WinRate = 88,
                AverageTurns = 9,
                TotalBattles = 100
            };
            original.CombinationResults.Add(new BattleStatisticsRunner.WeaponEnemyCombinationResult
            {
                WeaponType = WeaponType.Sword,
                EnemyType = "Goblin",
                WinRate = 88,
                AverageTurns = 9,
                TotalBattles = 25
            });

            var dto = ComprehensiveSimulationMapper.FromResult(original, 10, 10, 25);
            var restored = ComprehensiveSimulationMapper.ToResult(dto);

            TestBase.AssertTrue(Math.Abs(restored.OverallWinRate - 91.5) < 0.01,
                "Win rate round-trip", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, restored.CombinationResults.Count,
                "Combination count", ref _run, ref _pass, ref _fail);
        }
    }
}
