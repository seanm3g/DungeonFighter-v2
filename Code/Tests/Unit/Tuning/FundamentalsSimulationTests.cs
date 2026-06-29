using System;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Config;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;
using RPGGame.Tuning.Suggesters;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class FundamentalsSimulationTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== FundamentalsSimulation Tests ===\n");
            _run = _pass = _fail = 0;

            TestFundamentalsValidatorsPass();
            TestFundamentalsValidatorsFailTempo();
            TestFundamentalsDtoRoundTrip();
            TestFundamentalsSessionSnapshotRoundTrip();
            TestFundamentalsUsesLoaderEnemyWithHealthMultiplier();
            TestFundamentalsDurationSuggesterScalesToTarget();
            TestFundamentalsL1GateBlocksScalingUntilL1InBand();

            TestBase.PrintSummary("FundamentalsSimulation Tests", _run, _pass, _fail);
        }

        private static void TestFundamentalsValidatorsPass()
        {
            Console.WriteLine("--- Fundamentals validators pass ---");
            var profile = BalanceTuningProfileLoader.Load("combat-fundamentals");
            profile.Analysis.Suggesters = new System.Collections.Generic.List<string>();
            profile.Analysis.EnableDialRouting = false;
            profile.Analysis.MaxSuggestions = 0;
            var simulation = new TuningSimulationOutcome
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                Fundamentals = new FundamentalsSimulationResult
                {
                    MedianPlayerTurnsPerEncounter = 12,
                    MedianEnemyTurnsPerEncounter = 12,
                    MedianActionsPerEncounter = 27,
                    AverageActionsPerEncounter = 27,
                    AveragePlayerTurnsPerEncounter = 13,
                    AverageEnemyTurnsPerEncounter = 14,
                    AverageComboStreakRuns2PlusPerEncounter = 1.2,
                    AverageMaxComboStreak = 3.0,
                    SuccessfulEncounters = 100,
                    WinRate = 0.9
                }
            };

            var outcome = TuningAnalysisPipeline.Analyze(profile, simulation);
            TestBase.AssertTrue(outcome.Validation.IsValid, "Validation passes", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(outcome.Analysis.Suggestions.Count == 0, "No suggestions", ref _run, ref _pass, ref _fail);
        }

        private static void TestFundamentalsValidatorsFailTempo()
        {
            Console.WriteLine("--- Fundamentals tempo fail ---");
            var profile = BalanceTuningProfileLoader.Load("combat-fundamentals");
            var simulation = new TuningSimulationOutcome
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                Fundamentals = new FundamentalsSimulationResult
                {
                    AverageActionsPerEncounter = 2,
                    AverageComboStreakRuns2PlusPerEncounter = 1.0,
                    AverageMaxComboStreak = 3.0,
                    SuccessfulEncounters = 50
                }
            };

            var outcome = TuningAnalysisPipeline.Analyze(profile, simulation);
            TestBase.AssertTrue(!outcome.Validation.IsValid, "Tempo too low fails", ref _run, ref _pass, ref _fail);
        }

        private static void TestFundamentalsUsesLoaderEnemyWithHealthMultiplier()
        {
            Console.WriteLine("--- Fundamentals uses loader enemy with health multiplier ---");
            var cfg = GameConfiguration.Instance;
            EnemyLoader.LoadEnemies();
            double savedMult = cfg.EnemySystem.GlobalMultipliers.HealthMultiplier;
            try
            {
                var snapshot = FundamentalsCombatSetup.BuildSnapshot(new SimulationProfileConfig
                {
                    PlayerLevel = 10,
                    EnemyLevel = 10
                });

                TestBase.AssertTrue(!string.IsNullOrEmpty(snapshot.SessionEnemyLoaderType),
                    "Snapshot uses loader enemy", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(snapshot.LabEnemyBattleConfig == null,
                    "Snapshot has no hardcoded battle config", ref _run, ref _pass, ref _fail);

                cfg.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.0;
                var enemy1 = EnemyLoader.CreateEnemy(snapshot.SessionEnemyLoaderType!, snapshot.EnemyLevel);
                TestBase.AssertTrue(enemy1 != null, "Loader enemy created", ref _run, ref _pass, ref _fail);
                int hp1 = enemy1!.MaxHealth;

                cfg.EnemySystem.GlobalMultipliers.HealthMultiplier = 2.0;
                var enemy2 = EnemyLoader.CreateEnemy(snapshot.SessionEnemyLoaderType!, snapshot.EnemyLevel);
                TestBase.AssertTrue(enemy2 != null, "Loader enemy created at 2×", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(enemy2!.MaxHealth >= hp1 * 1.9,
                    $"HP scales with multiplier ({hp1} → {enemy2.MaxHealth})", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                cfg.EnemySystem.GlobalMultipliers.HealthMultiplier = savedMult;
            }
        }

        private static void TestFundamentalsDurationSuggesterScalesToTarget()
        {
            Console.WriteLine("--- Fundamentals duration suggester scales health to target ---");
            _ = GameConfiguration.Instance;
            var cfg = GameConfiguration.Instance;
            double savedMult = cfg.EnemySystem.GlobalMultipliers.HealthMultiplier;
            int savedBaseHp = cfg.Character.PlayerBaseHealth;
            double savedBaseScale = cfg.EnemySystem.ProgressionScales.BaseHealthScale;
            double savedGrowth = cfg.EnemySystem.ProgressionScales.HealthGrowthScale;
            try
            {
                cfg.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.0;
                cfg.Character.PlayerBaseHealth = 200;
                cfg.EnemySystem.ProgressionScales.BaseHealthScale = 1.0;
                cfg.EnemySystem.ProgressionScales.HealthGrowthScale = 1.0;
                var targets = new FundamentalsAnalysisTargets { TargetMedianCombinedActions = 27 };

                var highWrL10 = new FundamentalsSimulationResult
                {
                    WeaponType = "Sword",
                    EnemyType = "Goblin",
                    LevelSnapshots = new[]
                    {
                        new FundamentalsLevelSnapshot
                        {
                            Level = 10,
                            MedianCombinedActions = 9,
                            WinRate = 0.9
                        },
                        new FundamentalsLevelSnapshot
                        {
                            Level = 100,
                            MedianCombinedActions = 27,
                            WinRate = 0.01
                        }
                    }
                };

                var enemySuggestions = FundamentalsDurationAdjustmentSuggester.Suggest(highWrL10, targets);
                TestBase.AssertTrue(enemySuggestions.Count == 1, "One suggestion when L10 below target", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(enemySuggestions[0].Parameter == "ProgressionShape",
                    "High WR at L10 tunes progression shape (early/base)", ref _run, ref _pass, ref _fail);

                var lowWrL10 = new FundamentalsSimulationResult
                {
                    WeaponType = "Sword",
                    EnemyType = "Goblin",
                    LevelSnapshots = new[]
                    {
                        new FundamentalsLevelSnapshot
                        {
                            Level = 10,
                            MedianCombinedActions = 8,
                            WinRate = 0.2
                        }
                    }
                };

                var playerMidSuggestions = FundamentalsDurationAdjustmentSuggester.Suggest(lowWrL10, targets);
                TestBase.AssertTrue(playerMidSuggestions.Count == 1, "One suggestion for low WR at L10", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(playerMidSuggestions[0].Parameter == "HealthPerLevel",
                    "Low WR at L10 tunes health per level", ref _run, ref _pass, ref _fail);

                var lowWrL1 = new FundamentalsSimulationResult
                {
                    MedianActionsPerEncounter = 10,
                    AverageActionsPerEncounter = 10,
                    WinRate = 0.1,
                    WeaponType = "Sword",
                    PlayerLevel = 1,
                    EnemyLevel = 1,
                    EnemyType = "Goblin"
                };

                var playerSuggestions = FundamentalsDurationAdjustmentSuggester.Suggest(lowWrL1, targets);
                TestBase.AssertTrue(playerSuggestions.Count == 1, "One suggestion for low WR at L1", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(playerSuggestions[0].Parameter == "BaseHealth",
                    "Low WR at L1 tunes player base health", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(playerSuggestions[0].SuggestedValue > 200,
                    $"Player base HP increases ({playerSuggestions[0].SuggestedValue:F0})", ref _run, ref _pass, ref _fail);

                var highWrL1 = new FundamentalsSimulationResult
                {
                    MedianActionsPerEncounter = 9,
                    WinRate = 1.0,
                    WeaponType = "Sword",
                    PlayerLevel = 1,
                    EnemyLevel = 1,
                    EnemyType = "Goblin",
                    LevelSnapshots = new[]
                    {
                        new FundamentalsLevelSnapshot { Level = 1, MedianCombinedActions = 9, WinRate = 1.0 },
                        new FundamentalsLevelSnapshot { Level = 50, MedianCombinedActions = 27, WinRate = 0.504 }
                    }
                };

                var l1EnemySuggestions = FundamentalsDurationAdjustmentSuggester.Suggest(highWrL1, targets);
                TestBase.AssertTrue(l1EnemySuggestions.Count == 1, "L1 short fight on-curve WR suggests tempo", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(l1EnemySuggestions[0].Parameter == "BaseHealthScale",
                    "L1 on-curve WR lengthens fights via enemy base scale", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(l1EnemySuggestions[0].SuggestedValue > 1.0,
                    "Short L1 fight increases enemy base scale", ref _run, ref _pass, ref _fail);

                var inBand = FundamentalsDurationAdjustmentSuggester.Suggest(
                    new FundamentalsSimulationResult { MedianActionsPerEncounter = 27 }, targets);
                TestBase.AssertTrue(inBand.Count == 0, "No suggestion at target", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                cfg.EnemySystem.GlobalMultipliers.HealthMultiplier = savedMult;
                cfg.Character.PlayerBaseHealth = savedBaseHp;
                cfg.EnemySystem.ProgressionScales.BaseHealthScale = savedBaseScale;
                cfg.EnemySystem.ProgressionScales.HealthGrowthScale = savedGrowth;
            }
        }

        private static void TestFundamentalsDtoRoundTrip()
        {
            Console.WriteLine("--- Fundamentals DTO round-trip ---");
            var original = new FundamentalsSimulationResult
            {
                AverageActionsPerEncounter = 24,
                MedianActionsPerEncounter = 24,
                AveragePlayerTurnsPerEncounter = 12.2,
                MedianPlayerTurnsPerEncounter = 12,
                AverageEnemyTurnsPerEncounter = 11.8,
                MedianEnemyTurnsPerEncounter = 12,
                AverageComboStreakRuns2PlusPerEncounter = 0.8,
                AverageMaxComboStreak = 2.5,
                EncounterCount = 100,
                SuccessfulEncounters = 100,
                ForcedCatalogAction = "ATTACK",
                LevelSnapshots = new[]
                {
                    new FundamentalsLevelSnapshot
                    {
                        Level = 1,
                        MedianCombinedActions = 17,
                        WinRate = 1.0,
                        SuccessfulEncounters = 20
                    },
                    new FundamentalsLevelSnapshot
                    {
                        Level = 10,
                        MedianCombinedActions = 5,
                        WinRate = 0.8,
                        SuccessfulEncounters = 20
                    }
                }
            };
            original.ComboStreakRunTotals[2] = 40;
            original.ComboStreakRunTotals[3] = 12;

            var dto = ComprehensiveSimulationMapper.FromFundamentalsResult(original);
            var restored = ComprehensiveSimulationMapper.ToFundamentalsResult(dto);

            TestBase.AssertTrue(Math.Abs(restored.AverageActionsPerEncounter - 24) < 0.01,
                "Actions persisted", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(restored.MedianPlayerTurnsPerEncounter - 12) < 0.01,
                "Hero turns persisted", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(restored.MedianEnemyTurnsPerEncounter - 12) < 0.01,
                "Enemy turns persisted", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(40, restored.ComboStreakRunTotals[2],
                "Streak totals", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, restored.LevelSnapshots.Count,
                "Level snapshot count", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(restored.GetLevelSnapshot(10)!.MedianCombinedActions - 5) < 0.01,
                "L10 snapshot persisted", ref _run, ref _pass, ref _fail);
        }

        private static void TestFundamentalsSessionSnapshotRoundTrip()
        {
            Console.WriteLine("--- Fundamentals session snapshot round-trip ---");
            var session = new LevelTuningSession
            {
                ProfileId = "combat-dials",
                Fundamentals = ComprehensiveSimulationMapper.FromFundamentalsResult(new FundamentalsSimulationResult
                {
                    PlayerLevel = 1,
                    MedianActionsPerEncounter = 4.5,
                    WinRate = 0.98,
                    LevelSnapshots = new[]
                    {
                        new FundamentalsLevelSnapshot { Level = 1, MedianCombinedActions = 27, WinRate = 1.0 },
                        new FundamentalsLevelSnapshot { Level = 20, MedianCombinedActions = 3, WinRate = 1.0 }
                    }
                })
            };

            var outcome = ComprehensiveSimulationMapper.ToOutcome(session);
            var suggestions = FundamentalsDurationAdjustmentSuggester.Suggest(
                outcome.Fundamentals!,
                new FundamentalsAnalysisTargets
                {
                    TargetMedianCombinedActions = 27,
                    RequireL1AnchorBeforeScaling = false
                });

            TestBase.AssertTrue(outcome.Fundamentals!.LevelSnapshots.Count == 2,
                "Session restores level snapshots", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(suggestions.Count == 1, "One suggestion from restored snapshots", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(suggestions[0].Parameter == "ProgressionShape",
                "Routes to L20 progression shape not aggregate L1 base scale", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(suggestions[0].Target.Contains("L20"),
                "Targets worst decade level", ref _run, ref _pass, ref _fail);
        }

        private static void TestFundamentalsL1GateBlocksScalingUntilL1InBand()
        {
            Console.WriteLine("--- L1 gate blocks scaling until L1 in band ---");
            var targets = new FundamentalsAnalysisTargets
            {
                TargetMedianCombinedActions = 27,
                RequireL1AnchorBeforeScaling = true
            };

            var brokenL1 = new FundamentalsSimulationResult
            {
                WeaponType = "Sword",
                EnemyType = "Goblin",
                LevelSnapshots = new[]
                {
                    new FundamentalsLevelSnapshot { Level = 1, MedianCombinedActions = 38, WinRate = 0.13 },
                    new FundamentalsLevelSnapshot { Level = 30, MedianCombinedActions = 8, WinRate = 0.0 }
                }
            };

            var suggestions = FundamentalsDurationAdjustmentSuggester.Suggest(brokenL1, targets);
            TestBase.AssertTrue(suggestions.Count == 1, "One L1-only suggestion", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(suggestions[0].Parameter == "BaseHealth",
                "Low WR at L1 tunes hero base health before tempo knobs", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(suggestions[0].SuggestedValue > 200,
                "Low WR at L1 increases hero base HP", ref _run, ref _pass, ref _fail);

            var goodL1 = new FundamentalsSimulationResult
            {
                LevelSnapshots = new[]
                {
                    new FundamentalsLevelSnapshot { Level = 1, MedianCombinedActions = 27, WinRate = 1.0 },
                    new FundamentalsLevelSnapshot { Level = 10, MedianCombinedActions = 9, WinRate = 0.9 }
                }
            };

            var scaling = FundamentalsDurationAdjustmentSuggester.Suggest(goodL1, targets);
            TestBase.AssertTrue(scaling.Count == 0,
                "L1 anchor met — no scaling knobs while requireL1AnchorBeforeScaling", ref _run, ref _pass, ref _fail);
        }
    }
}
