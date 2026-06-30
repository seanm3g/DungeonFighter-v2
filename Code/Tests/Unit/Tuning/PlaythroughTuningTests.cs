using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;
using RPGGame.Tuning.Suggesters;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class PlaythroughTuningTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Playthrough Tuning Tests ===\n");
            _run = _pass = _fail = 0;

            TestLoadProfile();
            TestQualityScore();
            TestProgressionValidatorFailsWhenTooHard();
            TestParityValidatorFailsOnSpread();
            TestSuggesterWhenTooHard();
            TestSuggesterParitySuggestsEarlyGameKnobs();
            TestEarlyGameSuggestionApplier();
            TestSessionRoundTrip();

            TestBase.PrintSummary("Playthrough Tuning Tests", _run, _pass, _fail);
        }

        private static void TestLoadProfile()
        {
            Console.WriteLine("--- Load class-playthrough-balance profile ---");
            var profile = BalanceTuningProfileLoader.Load("class-playthrough-balance");
            TestBase.AssertEqual("class-playthrough-balance", profile.Id, "Profile id", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(TuningSimulationModes.ClassPlaythroughBatch, profile.Simulation.Mode,
                "Simulation mode", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(10, profile.Simulation.RunsPerClass, "Default runs per class", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(profile.Analysis.Validators.Contains(TuningValidatorIds.PlaythroughProgression),
                "Has progression validator", ref _run, ref _pass, ref _fail);
        }

        private static void TestQualityScore()
        {
            Console.WriteLine("--- Quality score ---");
            var targets = new PlaythroughAnalysisTargets();
            var good = new PlaythroughSimulationDto
            {
                OverallMeanFinalLevel = 4,
                OverallMeanDungeonsCompleted = 2,
                LevelSpread = 0.5,
                DungeonSpread = 0.3
            };
            var bad = new PlaythroughSimulationDto
            {
                OverallMeanFinalLevel = 1.2,
                OverallMeanDungeonsCompleted = 0.4,
                LevelSpread = 1.0,
                DungeonSpread = 0.8,
                HasParityWarnings = true,
                ParityWarnings = { "Wizard weak" }
            };

            double goodScore = PlaythroughSimulationMapper.CalculateQualityScore(good, targets);
            double badScore = PlaythroughSimulationMapper.CalculateQualityScore(bad, targets);
            TestBase.AssertTrue(goodScore > badScore, "Good batch scores higher than bad", ref _run, ref _pass, ref _fail);
        }

        private static void TestProgressionValidatorFailsWhenTooHard()
        {
            Console.WriteLine("--- Progression validator ---");
            var profile = BalanceTuningProfileLoader.Load("class-playthrough-balance");
            var outcome = BuildOutcome(meanLevel: 1.5, meanDungeons: 0.5, levelSpread: 0.4, dungeonSpread: 0.3);
            var analysisOutcome = TuningAnalysisPipeline.Analyze(profile, outcome);
            TestBase.AssertTrue(analysisOutcome.Validation.Warnings.Count > 0,
                "Low progression should warn", ref _run, ref _pass, ref _fail);
        }

        private static void TestParityValidatorFailsOnSpread()
        {
            Console.WriteLine("--- Parity validator ---");
            var profile = BalanceTuningProfileLoader.Load("class-playthrough-balance");
            var outcome = BuildOutcome(meanLevel: 4, meanDungeons: 2, levelSpread: 2.5, dungeonSpread: 1.5);
            var analysisOutcome = TuningAnalysisPipeline.Analyze(profile, outcome);
            TestBase.AssertTrue(analysisOutcome.Validation.Warnings.Any(w => w.Contains("spread")),
                "Large spread should warn", ref _run, ref _pass, ref _fail);
        }

        private static void TestSuggesterWhenTooHard()
        {
            Console.WriteLine("--- Playthrough suggester ---");
            var batch = new ClassPlaythroughBatchResult
            {
                ClassAggregates =
                {
                    new ClassPlaythroughAggregate { ClassDisplayName = "Barbarian", MeanFinalLevel = 1.5, MeanDungeonsCompleted = 0.5 },
                    new ClassPlaythroughAggregate { ClassDisplayName = "Wizard", MeanFinalLevel = 1.0, MeanDungeonsCompleted = 0.2 }
                }
            };
            var analysis = new TuningAnalysis { OverallWinRate = 0 };
            var suggestions = PlaythroughAdjustmentSuggester.Suggest(batch, analysis, new PlaythroughAnalysisTargets());
            TestBase.AssertTrue(suggestions.Count > 0, "Too-hard batch should suggest adjustments", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(suggestions.Any(s => s.Parameter == "HealthMultiplier" || s.Parameter == "BaseHealth"),
                "Should suggest power knobs", ref _run, ref _pass, ref _fail);
        }

        private static void TestSuggesterParitySuggestsEarlyGameKnobs()
        {
            Console.WriteLine("--- Parity suggester early-game knobs ---");
            var batch = new ClassPlaythroughBatchResult
            {
                ParityWarnings = { "Rogue weak" },
                ClassAggregates =
                {
                    new ClassPlaythroughAggregate
                    {
                        WeaponType = WeaponType.Mace,
                        ClassDisplayName = "Barbarian",
                        MeanFinalLevel = 4.5,
                        MeanDungeonsCompleted = 2.5
                    },
                    new ClassPlaythroughAggregate
                    {
                        WeaponType = WeaponType.Dagger,
                        ClassDisplayName = "Rogue",
                        MeanFinalLevel = 1.5,
                        MeanDungeonsCompleted = 0.3
                    }
                }
            };
            var suggestions = PlaythroughAdjustmentSuggester.Suggest(
                batch, new TuningAnalysis(), new PlaythroughAnalysisTargets { MaxLevelSpread = 1.5 });

            TestBase.AssertTrue(
                suggestions.Any(s => s.Category == "early_game" || s.Category == "starting_weapon" || s.Category == "class_balance"),
                "Parity issue should suggest early-game knobs", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(
                suggestions.Any(s => s.Target.Equals("Dagger", StringComparison.OrdinalIgnoreCase)),
                "Weakest path should be targeted", ref _run, ref _pass, ref _fail);
        }

        private static void TestEarlyGameSuggestionApplier()
        {
            Console.WriteLine("--- Early-game suggestion applier ---");
            var cfg = GameConfiguration.Instance;
            cfg.EarlyGame ??= new EarlyGameConfig();
            double savedActionMult = cfg.EarlyGame.StartingActionDamageMultiplier.Dagger;
            int savedWeaponDamage = cfg.WeaponScaling!.StartingWeaponDamage.Dagger;

            try
            {
                var suggestion = new TuningSuggestion
                {
                    Category = "early_game",
                    Target = "Dagger",
                    Parameter = "StartingActionDamageMultiplier",
                    SuggestedValue = 1.15
                };
                TestBase.AssertTrue(TuningSuggestionApplier.Apply(suggestion),
                    "Should apply starting action multiplier", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(
                    Math.Abs(cfg.EarlyGame.StartingActionDamageMultiplier.Dagger - 1.15) < 0.001,
                    "Dagger action mult updated", ref _run, ref _pass, ref _fail);

                suggestion = new TuningSuggestion
                {
                    Category = "starting_weapon",
                    Target = "Dagger",
                    Parameter = "StartingWeaponDamage",
                    SuggestedValue = savedWeaponDamage + 1
                };
                TestBase.AssertTrue(TuningSuggestionApplier.Apply(suggestion),
                    "Should apply starting weapon damage", ref _run, ref _pass, ref _fail);
            }
            finally
            {
                cfg.EarlyGame.StartingActionDamageMultiplier.Dagger = savedActionMult;
                cfg.WeaponScaling.StartingWeaponDamage.Dagger = savedWeaponDamage;
            }
        }

        private static void TestSessionRoundTrip()
        {
            Console.WriteLine("--- Session round trip ---");
            var batch = new ClassPlaythroughBatchResult
            {
                RunsPerClass = 5,
                ClassAggregates =
                {
                    new ClassPlaythroughAggregate
                    {
                        WeaponType = WeaponType.Sword,
                        ClassDisplayName = "Warrior",
                        RunCount = 5,
                        MeanFinalLevel = 2,
                        MeanDungeonsCompleted = 1
                    }
                }
            };
            var dto = PlaythroughSimulationMapper.FromBatch(batch);
            var session = new LevelTuningSession
            {
                ProfileId = "class-playthrough-balance",
                SimulationMode = TuningSimulationModes.ClassPlaythroughBatch,
                PlaythroughBatch = dto
            };
            var outcome = ComprehensiveSimulationMapper.ToOutcome(session);
            TestBase.AssertTrue(outcome.PlaythroughBatch != null, "Round-trip batch present", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(TuningSimulationModes.ClassPlaythroughBatch, outcome.Mode, "Mode preserved", ref _run, ref _pass, ref _fail);
        }

        private static TuningSimulationOutcome BuildOutcome(
            double meanLevel,
            double meanDungeons,
            double levelSpread,
            double dungeonSpread)
        {
            var dto = new PlaythroughSimulationDto
            {
                OverallMeanFinalLevel = meanLevel,
                OverallMeanDungeonsCompleted = meanDungeons,
                LevelSpread = levelSpread,
                DungeonSpread = dungeonSpread,
                Classes = new List<PlaythroughClassAggregateDto>
                {
                    new() { ClassDisplayName = "Barbarian", MeanFinalLevel = meanLevel + levelSpread / 2, MeanDungeonsCompleted = meanDungeons + dungeonSpread / 2 },
                    new() { ClassDisplayName = "Wizard", MeanFinalLevel = meanLevel - levelSpread / 2, MeanDungeonsCompleted = meanDungeons - dungeonSpread / 2 }
                }
            };
            dto.QualityScore = PlaythroughSimulationMapper.CalculateQualityScore(dto, new PlaythroughAnalysisTargets());

            return new TuningSimulationOutcome
            {
                Mode = TuningSimulationModes.ClassPlaythroughBatch,
                ProfileId = "class-playthrough-balance",
                PlaythroughSnapshot = dto,
                PlaythroughBatch = PlaythroughSimulationMapper.ToBatch(dto)
            };
        }
    }
}
