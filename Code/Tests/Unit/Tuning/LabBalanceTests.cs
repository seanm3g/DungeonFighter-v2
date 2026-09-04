using System;
using System.Linq;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.Tuning.LabBalance;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class LabBalanceTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayerDefinitions_CoverAllEnumValues(ref run, ref passed, ref failed);
            CombatEquation_KnobsNonEmpty(ref run, ref passed, ref failed);
            Feel_KnobsIncludeRollFeel(ref run, ref passed, ref failed);
            BuildSnapshot_WeaponOnlyGoblin(ref run, ref passed, ref failed);
            TargetEvaluator_PassWhenInBand(ref run, ref passed, ref failed);
            TargetEvaluator_FailWhenOutOfBand(ref run, ref passed, ref failed);
            FormatRecipe_ContainsLabBands(ref run, ref passed, ref failed);
            FormatRecipeForLayer_CombatEquationMentionsOkDefinition(ref run, ref passed, ref failed);
            LayerGoals_AreDistinctPerProcessStep(ref run, ref passed, ref failed);
            EvaluateForLayer_FeelIncludesStdDev(ref run, ref passed, ref failed);
            EvaluateWeaponMatrix_ChecksSpread(ref run, ref passed, ref failed);
            EvaluateDungeon_ChecksClearRate(ref run, ref passed, ref failed);
            EvaluateAnchors_ChecksEveryLevel(ref run, ref passed, ref failed);
            FormatRunStatus_MetAndUnmet(ref run, ref passed, ref failed);
            SuggestFromFailedChecks_YieldsKnobWhenDurationFail(ref run, ref passed, ref failed);
            SuggestOne_AllPassedReturnsMetReason(ref run, ref passed, ref failed);
            FormatLoopProgressReport_IncludesStartEndAndKnobDelta(ref run, ref passed, ref failed);
            FormatChecklistProgress_ShowsPassFailTransitions(ref run, ref passed, ref failed);
            DurationTargetsForLevel_RespectsEditedCombinedTarget(ref run, ref passed, ref failed);
            ResetEditableTargets_RestoresCombatEquationDefaults(ref run, ref passed, ref failed);
            Registry_HasEnemyBalancePoolKnobs(ref run, ref passed, ref failed);
            ScenarioApplicator_AppliesCombatEquationToSession(ref run, ref passed, ref failed);

            TestBase.PrintSummary("LabBalanceTests", run, passed, failed);
        }

        private static void LayerDefinitions_CoverAllEnumValues(ref int run, ref int passed, ref int failed)
        {
            foreach (LabBalanceProcessLayer layer in Enum.GetValues(typeof(LabBalanceProcessLayer)))
            {
                var def = LabBalanceLayerDefinition.Get(layer);
                TestBase.AssertTrue(def.Layer == layer,
                    $"Definition for {layer}", ref run, ref passed, ref failed);
                TestBase.AssertTrue(!string.IsNullOrWhiteSpace(def.DisplayName),
                    $"{layer} has display name", ref run, ref passed, ref failed);
                TestBase.AssertTrue(!string.IsNullOrWhiteSpace(def.GoalIntent),
                    $"{layer} has GoalIntent", ref run, ref passed, ref failed);
            }
        }

        private static void CombatEquation_KnobsNonEmpty(ref int run, ref int passed, ref int failed)
        {
            var knobs = LabBalanceScenarioApplicator.GetKnobsForLayer(LabBalanceProcessLayer.CombatEquation);
            TestBase.AssertTrue(knobs.Count >= 5,
                $"CombatEquation knobs >= 5 (actual {knobs.Count})", ref run, ref passed, ref failed);
            TestBase.AssertTrue(knobs.All(k => k.Layer == CombatTuningLayer.Duration),
                "CombatEquation knobs are Duration layer", ref run, ref passed, ref failed);
        }

        private static void Feel_KnobsIncludeRollFeel(ref int run, ref int passed, ref int failed)
        {
            var knobs = LabBalanceScenarioApplicator.GetKnobsForLayer(LabBalanceProcessLayer.Feel);
            TestBase.AssertTrue(knobs.Any(k => k.Layer == CombatTuningLayer.RollFeel),
                "Feel includes RollFeel knobs", ref run, ref passed, ref failed);
            TestBase.AssertTrue(knobs.Any(k => k.Layer == CombatTuningLayer.ComboAffordance),
                "Feel includes ComboAffordance knobs", ref run, ref passed, ref failed);
        }

        private static void BuildSnapshot_WeaponOnlyGoblin(ref int run, ref int passed, ref int failed)
        {
            try
            {
                var snap = LabBalanceScenarioApplicator.BuildFundamentalsStyleSnapshot(
                    "Sword", FundamentalsCombatSetup.DefaultFundamentalsEnemyType, 1, 1);
                TestBase.AssertTrue(
                    string.Equals(snap.SessionEnemyLoaderType, FundamentalsCombatSetup.DefaultFundamentalsEnemyType,
                        StringComparison.OrdinalIgnoreCase)
                    || snap.SessionEnemyLoaderType != null,
                    "Snapshot has loader enemy", ref run, ref passed, ref failed);
                TestBase.AssertEqual(1, snap.EnemyLevel, "Enemy level 1", ref run, ref passed, ref failed);
                TestBase.AssertTrue(snap.ComboStripActionNames.Count > 0,
                    "Snapshot has combo strip", ref run, ref passed, ref failed);
                TestBase.AssertTrue(!string.IsNullOrWhiteSpace(snap.SelectedCatalogActionName),
                    "Snapshot has forced action", ref run, ref passed, ref failed);
                TestBase.AssertEqual(0, snap.LabPanelArmorDelta, "No armor panel delta", ref run, ref passed, ref failed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"BuildSnapshot threw: {ex.Message}", ref run, ref passed, ref failed);
            }
        }

        private static void TargetEvaluator_PassWhenInBand(ref int run, ref int passed, ref int failed)
        {
            var report = BuildSyntheticReport(medianTurns: 27, playerTurns: 13, enemyTurns: 14, winRate: 0.9);
            var targets = new FundamentalsAnalysisTargets
            {
                TargetMedianCombinedActions = 27,
                TargetMedianPlayerTurns = 12,
                TargetMedianEnemyTurns = 12,
                TempoTolerance = 2,
                MinAverageActions = 24,
                MaxAverageActions = 30,
                MinAverageComboStreakRuns2Plus = 0,
                MinAverageMaxComboStreak = 0
            };
            var eval = LabBalanceTargetEvaluator.EvaluateEncounter(report, targets, includeComboChecks: false);
            TestBase.AssertTrue(eval.AllPassed, "In-band report passes", ref run, ref passed, ref failed);
        }

        private static void TargetEvaluator_FailWhenOutOfBand(ref int run, ref int passed, ref int failed)
        {
            var report = BuildSyntheticReport(medianTurns: 8, playerTurns: 4, enemyTurns: 4, winRate: 1.0);
            var targets = new FundamentalsAnalysisTargets
            {
                TargetMedianCombinedActions = 27,
                TargetMedianPlayerTurns = 12,
                TargetMedianEnemyTurns = 12,
                TempoTolerance = 1.5,
                MinAverageActions = 24,
                MaxAverageActions = 30,
                MinAverageComboStreakRuns2Plus = 0,
                MinAverageMaxComboStreak = 0
            };
            var eval = LabBalanceTargetEvaluator.EvaluateEncounter(report, targets, includeComboChecks: false);
            TestBase.AssertTrue(!eval.AllPassed, "Out-of-band report fails", ref run, ref passed, ref failed);
        }

        private static void FormatRecipe_ContainsLabBands(ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation);
            string recipe = LabBalanceTargetEvaluator.FormatRecipeForLayer(def);
            TestBase.AssertTrue(recipe.Contains("27", StringComparison.Ordinal),
                "Recipe mentions combined 27", ref run, ref passed, ref failed);
            TestBase.AssertTrue(recipe.Contains("±", StringComparison.Ordinal),
                "Recipe mentions tolerance", ref run, ref passed, ref failed);
            TestBase.AssertTrue(recipe.Contains("24", StringComparison.Ordinal) && recipe.Contains("30", StringComparison.Ordinal),
                "Recipe mentions mean band 24–30", ref run, ref passed, ref failed);
            TestBase.AssertTrue(recipe.Contains("OK = every checklist line PASS", StringComparison.Ordinal),
                "Recipe defines OK", ref run, ref passed, ref failed);
        }

        private static void FormatRecipeForLayer_CombatEquationMentionsOkDefinition(
            ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation);
            string recipe = LabBalanceTargetEvaluator.FormatRecipeForLayer(def);
            TestBase.AssertTrue(recipe.Contains("Goal:", StringComparison.Ordinal),
                "Layer recipe has Goal", ref run, ref passed, ref failed);
            TestBase.AssertTrue(recipe.Contains("12", StringComparison.Ordinal),
                "Layer recipe mentions hero/enemy 12", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!recipe.Contains("combo", StringComparison.OrdinalIgnoreCase),
                "Combat equation recipe omits combo", ref run, ref passed, ref failed);
        }

        private static void LayerGoals_AreDistinctPerProcessStep(ref int run, ref int passed, ref int failed)
        {
            var combat = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation);
            var feel = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.Feel);
            var curve = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.LevelCurve);
            var parity = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.WeaponParity);
            var gear = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.GearInjection);
            var dungeon = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.DungeonAttrition);

            TestBase.AssertTrue(combat.HasFlag(LabBalanceCheckFlags.Duration)
                                && !combat.HasFlag(LabBalanceCheckFlags.Combo),
                "Combat equation: duration only", ref run, ref passed, ref failed);
            TestBase.AssertTrue(feel.HasFlag(LabBalanceCheckFlags.Combo)
                                && feel.HasFlag(LabBalanceCheckFlags.FeelVariance),
                "Feel: combo + variance", ref run, ref passed, ref failed);
            TestBase.AssertTrue(curve.HasFlag(LabBalanceCheckFlags.AllAnchors)
                                && curve.HasFlag(LabBalanceCheckFlags.WinRateCurve),
                "Level curve: all anchors + WR curve", ref run, ref passed, ref failed);
            TestBase.AssertTrue(parity.HasFlag(LabBalanceCheckFlags.WeaponSpread),
                "Weapon parity: WR spread", ref run, ref passed, ref failed);
            TestBase.AssertTrue(gear.WinRateMin >= 0.90 && gear.Targets.MinAverageActions <= 18.01,
                "Gear: high WR floor + short mean floor", ref run, ref passed, ref failed);
            TestBase.AssertTrue(dungeon.HasFlag(LabBalanceCheckFlags.DungeonClear)
                                && !dungeon.HasFlag(LabBalanceCheckFlags.Duration),
                "Dungeon: clear rate only", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                Math.Abs(LabBalanceLayerDefinition.TempoToleranceForLevel(25) - 2.5) < 0.01,
                "L25 tempo tolerance widens to 2.5", ref run, ref passed, ref failed);
        }

        private static void EvaluateForLayer_FeelIncludesStdDev(ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.Feel);
            var report = BuildSyntheticReport(medianTurns: 27, playerTurns: 12, enemyTurns: 15, winRate: 0.95);
            report.StdDevTurns = 12;
            report.AveragePlayerMaxComboStreak = 2.5;
            var eval = LabBalanceTargetEvaluator.EvaluateForLayer(def, report, levelOverride: 1);
            TestBase.AssertTrue(eval.Checks.Any(c => c.Name.Contains("std-dev", StringComparison.OrdinalIgnoreCase)),
                "Feel checklist has std-dev", ref run, ref passed, ref failed);
            TestBase.AssertTrue(eval.Checks.Any(c =>
                    c.Name.Contains("std-dev", StringComparison.OrdinalIgnoreCase) && !c.Passed),
                "High std-dev fails", ref run, ref passed, ref failed);
        }

        private static void EvaluateWeaponMatrix_ChecksSpread(ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.WeaponParity);
            var rows = new[]
            {
                ("Mace", 0.95, 27.0),
                ("Sword", 0.94, 26.0),
                ("Dagger", 0.70, 28.0),
                ("Wand", 0.93, 27.0)
            };
            var eval = LabBalanceTargetEvaluator.EvaluateWeaponMatrix(def, rows);
            TestBase.AssertTrue(eval.Checks.Any(c => c.Name.Contains("spread", StringComparison.OrdinalIgnoreCase)),
                "Matrix has spread check", ref run, ref passed, ref failed);
            TestBase.AssertTrue(eval.Checks.Any(c =>
                    c.Name.Contains("spread", StringComparison.OrdinalIgnoreCase) && !c.Passed),
                "25pp spread fails ≤12pp", ref run, ref passed, ref failed);
        }

        private static void EvaluateDungeon_ChecksClearRate(ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.DungeonAttrition);
            var report = new ActionLabDungeonSimulationReport { ClearRate = 0.40 };
            var eval = LabBalanceTargetEvaluator.EvaluateDungeon(def, report);
            TestBase.AssertTrue(eval.Checks.Count == 1, "One dungeon check", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!eval.AllPassed, "Low clear rate fails", ref run, ref passed, ref failed);
        }

        private static void EvaluateAnchors_ChecksEveryLevel(ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.LevelCurve);
            var snaps = new[]
            {
                new FundamentalsLevelSnapshot { Level = 1, MedianCombinedActions = 27, WinRate = 0.95 },
                new FundamentalsLevelSnapshot { Level = 10, MedianCombinedActions = 10, WinRate = 0.95 },
                new FundamentalsLevelSnapshot { Level = 25, MedianCombinedActions = 27, WinRate = 0.95 }
            };
            var eval = LabBalanceTargetEvaluator.EvaluateAnchors(def, snaps);
            TestBase.AssertTrue(eval.Checks.Count >= 6,
                $"Anchor checks cover levels (actual {eval.Checks.Count})", ref run, ref passed, ref failed);
            TestBase.AssertTrue(eval.Checks.Any(c =>
                    c.Name.Contains("L10", StringComparison.Ordinal) && !c.Passed),
                "L10 short fight fails its own band", ref run, ref passed, ref failed);
        }

        private static void FormatRunStatus_MetAndUnmet(ref int run, ref int passed, ref int failed)
        {
            var passReport = BuildSyntheticReport(medianTurns: 27, playerTurns: 12, enemyTurns: 15, winRate: 0.9);
            var passEval = LabBalanceTargetEvaluator.EvaluateEncounter(
                passReport,
                new FundamentalsAnalysisTargets
                {
                    TargetMedianCombinedActions = 27,
                    TargetMedianPlayerTurns = 12,
                    TargetMedianEnemyTurns = 15,
                    TempoTolerance = 2,
                    MinAverageActions = 24,
                    MaxAverageActions = 30,
                    MinAverageComboStreakRuns2Plus = 0,
                    MinAverageMaxComboStreak = 0
                },
                includeComboChecks: false);

            var met = ActionLabBalanceViewModel.FormatRunStatus(new LabBalanceStatsRunResult
            {
                Targets = passEval,
                EncounterReport = passReport
            });
            TestBase.AssertTrue(met.StartsWith("Targets met", StringComparison.Ordinal),
                $"Met status: {met}", ref run, ref passed, ref failed);

            var failReport = BuildSyntheticReport(medianTurns: 8, playerTurns: 4, enemyTurns: 4, winRate: 1.0);
            var failEval = LabBalanceTargetEvaluator.EvaluateEncounter(
                failReport, LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation).Targets,
                includeComboChecks: false);
            var unmet = ActionLabBalanceViewModel.FormatRunStatus(new LabBalanceStatsRunResult
            {
                Targets = failEval,
                EncounterReport = failReport
            });
            TestBase.AssertTrue(unmet.StartsWith("Targets unmet", StringComparison.Ordinal),
                $"Unmet status: {unmet}", ref run, ref passed, ref failed);
            TestBase.AssertTrue(unmet.Contains("Suggest", StringComparison.Ordinal),
                "Unmet status prompts Suggest", ref run, ref passed, ref failed);
        }

        private static void SuggestFromFailedChecks_YieldsKnobWhenDurationFail(
            ref int run, ref int passed, ref int failed)
        {
            var report = BuildSyntheticReport(medianTurns: 20, playerTurns: 10, enemyTurns: 10, winRate: 0.95);
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation);
            var eval = LabBalanceTargetEvaluator.EvaluateEncounter(report, def.Targets, includeComboChecks: false);
            TestBase.AssertTrue(!eval.AllPassed, "Synthetic short fight fails checklist", ref run, ref passed, ref failed);

            var suggestion = LabBalanceStatsService.SuggestFromFailedChecks(eval, def, report);
            TestBase.AssertTrue(suggestion != null, "Fallback yields a suggestion", ref run, ref passed, ref failed);
            TestBase.AssertTrue(suggestion!.Reason.StartsWith("Addresses FAIL", StringComparison.Ordinal),
                "Reason prefixes FAIL", ref run, ref passed, ref failed);
            var allowed = LabBalanceScenarioApplicator.GetKnobsForLayer(def.Layer)
                .Select(k => k.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            TestBase.AssertTrue(allowed.Contains(suggestion.Parameter),
                $"Suggestion parameter {suggestion.Parameter} is a layer knob", ref run, ref passed, ref failed);

            double before = GameConfiguration.Instance.EnemySystem.BaselineStats.Health;
            bool applied = TuningSuggestionApplier.Apply(suggestion);
            TestBase.AssertTrue(applied, "Applier accepts lab_balance registry knob", ref run, ref passed, ref failed);
            double after = GameConfiguration.Instance.EnemySystem.BaselineStats.Health;
            TestBase.AssertTrue(Math.Abs(after - suggestion.SuggestedValue) < 0.5,
                $"enemyBaselineHealth updated ({before} → {after})", ref run, ref passed, ref failed);
            GameConfiguration.Instance.EnemySystem.BaselineStats.Health = (int)Math.Round(before);
        }

        private static void SuggestOne_AllPassedReturnsMetReason(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ActionLoader.LoadActions();
                var hero = new Character("LabBalanceSuggestHero", 5);
                var combatManager = new CombatManager();
                ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
                try
                {
                    LabBalanceScenarioApplicator.Apply(
                        ActionInteractionLabSession.Current!,
                        LabBalanceProcessLayer.CombatEquation);

                    var report = BuildSyntheticReport(medianTurns: 27, playerTurns: 12, enemyTurns: 15, winRate: 0.9);
                    var eval = LabBalanceTargetEvaluator.EvaluateEncounter(
                        report,
                        new FundamentalsAnalysisTargets
                        {
                            TargetMedianCombinedActions = 27,
                            TargetMedianPlayerTurns = 12,
                            TargetMedianEnemyTurns = 15,
                            TempoTolerance = 2,
                            MinAverageActions = 24,
                            MaxAverageActions = 30,
                            MinAverageComboStreakRuns2Plus = 0,
                            MinAverageMaxComboStreak = 0
                        },
                        includeComboChecks: false);
                    TestBase.AssertTrue(eval.AllPassed, "Fixture AllPassed", ref run, ref passed, ref failed);

                    var outcome = LabBalanceStatsService.SuggestOne(
                        ActionInteractionLabSession.Current!,
                        new LabBalanceStatsRunResult
                        {
                            EncounterReport = report,
                            Targets = eval
                        });
                    TestBase.AssertTrue(outcome.Suggestion == null, "No suggestion when met", ref run, ref passed, ref failed);
                    TestBase.AssertTrue(
                        outcome.EmptyReason.Contains("All targets met", StringComparison.Ordinal),
                        $"Empty reason: {outcome.EmptyReason}", ref run, ref passed, ref failed);
                }
                finally
                {
                    ActionInteractionLabSession.EndSession();
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"SuggestOne AllPassed threw: {ex.Message}", ref run, ref passed, ref failed);
                try { ActionInteractionLabSession.EndSession(); } catch { /* ignore */ }
            }
        }

        private static void FormatLoopProgressReport_IncludesStartEndAndKnobDelta(
            ref int run, ref int passed, ref int failed)
        {
            var start = new LabBalanceTargetEvaluation
            {
                Checks = new[]
                {
                    new LabBalanceTargetCheck
                    {
                        Name = "Median combined",
                        ActualText = "25.0",
                        TargetText = "27 ± 1.5",
                        Passed = false
                    }
                }
            };
            var end = new LabBalanceTargetEvaluation
            {
                Checks = new[]
                {
                    new LabBalanceTargetCheck
                    {
                        Name = "Median combined",
                        ActualText = "27.2",
                        TargetText = "27 ± 1.5",
                        Passed = true
                    }
                }
            };
            var startKnobs = new Dictionary<string, double> { ["enemyBaseHealth"] = 53 };
            var endKnobs = new Dictionary<string, double> { ["enemyBaseHealth"] = 66 };
            string report = LabBalanceStatsService.FormatLoopProgressReport(
                "1. Combat equation",
                maxIterations: 10,
                iterationsUsed: 3,
                stopReason: "All targets met",
                start,
                end,
                startKnobs,
                endKnobs,
                new[] { "enemyBaseHealth: 53.000 → 66.000" },
                "--- Iteration 1/10 — targets 0/1 ---\nApplied + saved.");

            TestBase.AssertTrue(report.Contains("RUN LOOP PROGRESS REPORT", StringComparison.Ordinal),
                "Has progress header", ref run, ref passed, ref failed);
            TestBase.AssertTrue(report.Contains("Iterations: 3/10", StringComparison.Ordinal),
                "Has iteration count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(report.Contains("All targets met", StringComparison.Ordinal),
                "Has stop reason", ref run, ref passed, ref failed);
            TestBase.AssertTrue(report.Contains("enemyBaseHealth: 53.000 → 66.000", StringComparison.Ordinal),
                "Has knob delta", ref run, ref passed, ref failed);
            TestBase.AssertTrue(report.Contains("CHECKLIST PROGRESS", StringComparison.Ordinal),
                "Has checklist progress section", ref run, ref passed, ref failed);
        }

        private static void FormatChecklistProgress_ShowsPassFailTransitions(
            ref int run, ref int passed, ref int failed)
        {
            var start = new LabBalanceTargetEvaluation
            {
                Checks = new[]
                {
                    new LabBalanceTargetCheck
                    {
                        Name = "Median combined", ActualText = "25.0", TargetText = "27", Passed = false
                    },
                    new LabBalanceTargetCheck
                    {
                        Name = "Win rate", ActualText = "90%", TargetText = "80–99%", Passed = true
                    }
                }
            };
            var end = new LabBalanceTargetEvaluation
            {
                Checks = new[]
                {
                    new LabBalanceTargetCheck
                    {
                        Name = "Median combined", ActualText = "27.0", TargetText = "27", Passed = true
                    },
                    new LabBalanceTargetCheck
                    {
                        Name = "Win rate", ActualText = "90%", TargetText = "80–99%", Passed = true
                    }
                }
            };
            string text = LabBalanceStatsService.FormatChecklistProgress(start, end);
            TestBase.AssertTrue(text.Contains("0/2 → 2/2", StringComparison.Ordinal)
                                || text.Contains("Passed: 1/2 → 2/2", StringComparison.Ordinal),
                $"Passed counts in progress: {text}", ref run, ref passed, ref failed);
            TestBase.AssertTrue(text.Contains("Median combined", StringComparison.Ordinal),
                "Mentions median check", ref run, ref passed, ref failed);
            TestBase.AssertTrue(text.Contains("↑", StringComparison.Ordinal),
                "Marks FAIL→PASS with up arrow", ref run, ref passed, ref failed);
        }

        private static void DurationTargetsForLevel_RespectsEditedCombinedTarget(
            ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation);
            double original = def.Targets.TargetMedianCombinedActions;
            try
            {
                def.Targets.TargetMedianCombinedActions = 33;
                def.Targets.TempoTolerance = 2.5;
                var resolved = LabBalanceTargetEvaluator.DurationTargetsForLevel(def, level: 1);
                TestBase.AssertEqual(33, resolved.TargetMedianCombinedActions,
                    "Edited combined target used", ref run, ref passed, ref failed);
                TestBase.AssertEqual(2.5, resolved.TempoTolerance,
                    "Edited tolerance used at default level", ref run, ref passed, ref failed);
            }
            finally
            {
                def.Targets.TargetMedianCombinedActions = original;
                LabBalanceLayerDefinition.ResetEditableTargets(LabBalanceProcessLayer.CombatEquation);
            }
        }

        private static void ResetEditableTargets_RestoresCombatEquationDefaults(
            ref int run, ref int passed, ref int failed)
        {
            var def = LabBalanceLayerDefinition.Get(LabBalanceProcessLayer.CombatEquation);
            def.Targets.TargetMedianCombinedActions = 99;
            LabBalanceLayerDefinition.ResetEditableTargets(LabBalanceProcessLayer.CombatEquation);
            TestBase.AssertEqual(27, def.Targets.TargetMedianCombinedActions,
                "Reset restores combined 27", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1.5, def.Targets.TempoTolerance,
                "Reset restores L1 tolerance 1.5", ref run, ref passed, ref failed);
        }

        private static void Registry_HasEnemyBalancePoolKnobs(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(CombatTuningParameterRegistry.GetById("enemyAttributePoolBase") != null,
                "enemyAttributePoolBase registered", ref run, ref passed, ref failed);
            TestBase.AssertTrue(CombatTuningParameterRegistry.GetById("statConversionHealth") != null,
                "statConversionHealth registered", ref run, ref passed, ref failed);
        }

        private static void ScenarioApplicator_AppliesCombatEquationToSession(ref int run, ref int passed, ref int failed)
        {
            try
            {
                ActionLoader.LoadActions();
                var hero = new Character("LabBalanceTestHero", 5);
                var combatManager = new CombatManager();
                ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
                try
                {
                    var summary = LabBalanceScenarioApplicator.Apply(
                        ActionInteractionLabSession.Current!,
                        LabBalanceProcessLayer.CombatEquation);
                    TestBase.AssertTrue(summary.Layer == LabBalanceProcessLayer.CombatEquation,
                        "Layer applied", ref run, ref passed, ref failed);
                    TestBase.AssertTrue(summary.WeaponOnly, "Weapon-only scenario", ref run, ref passed, ref failed);
                    TestBase.AssertEqual(1, summary.PlayerLevel, "Hero level 1", ref run, ref passed, ref failed);
                    TestBase.AssertEqual(1, summary.EnemyLevel, "Enemy level 1", ref run, ref passed, ref failed);
                    TestBase.AssertTrue(
                        ActionInteractionLabSession.Current!.ActiveBalanceProcessLayer
                        == LabBalanceProcessLayer.CombatEquation,
                        "Session stores active layer", ref run, ref passed, ref failed);

                    var snap = ActionInteractionLabSession.Current.CaptureSimulationSnapshot();
                    TestBase.AssertTrue(snap.SessionEnemyLoaderType != null,
                        "Session enemy loader set", ref run, ref passed, ref failed);
                    TestBase.AssertTrue(!string.IsNullOrWhiteSpace(snap.SelectedCatalogActionName),
                        "Forced catalog action set", ref run, ref passed, ref failed);
                }
                finally
                {
                    ActionInteractionLabSession.EndSession();
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Applicator session test threw: {ex.Message}", ref run, ref passed, ref failed);
                try { ActionInteractionLabSession.EndSession(); } catch { /* ignore */ }
            }
        }

        private static ActionLabEncounterSimulationReport BuildSyntheticReport(
            double medianTurns, int playerTurns, int enemyTurns, double winRate)
        {
            var report = new ActionLabEncounterSimulationReport
            {
                WinRate = winRate,
                MedianTurns = medianTurns,
                AverageTurns = medianTurns,
                AveragePlayerMaxComboStreak = 2
            };
            report.Encounters.Add(new EncounterMetrics
            {
                Turns = (int)medianTurns,
                PlayerTurns = playerTurns,
                EnemyTurns = enemyTurns
            });
            report.PlayerWins = winRate >= 0.5 ? 1 : 0;
            return report;
        }
    }
}
