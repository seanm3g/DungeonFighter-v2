using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Applies UI / CLI run options onto a loaded profile before sim or analyze.
    /// </summary>
    public static class BalanceTuningProfileApplicator
    {
        public static void ApplySimulation(BalanceTuningProfile profile, BalanceTuningRunOptions options)
        {
            var sim = profile.Simulation;

            if (options.BattlesPerCombination != null)
                sim.BattlesPerCombination = options.BattlesPerCombination.Value;
            if (options.BattlesPerWeapon != null)
                sim.BattlesPerWeapon = options.BattlesPerWeapon.Value;
            if (options.NumberOfBattles != null)
                sim.NumberOfBattles = options.NumberOfBattles.Value;
            if (options.PlayerLevel != null)
                sim.PlayerLevel = options.PlayerLevel.Value;
            if (options.EnemyLevel != null)
                sim.EnemyLevel = options.EnemyLevel.Value;
            if (options.EncounterCount != null)
                sim.EncounterCount = options.EncounterCount.Value;

            if (!string.IsNullOrWhiteSpace(options.LevelsCsv))
            {
                sim.Levels = options.LevelsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToList();
            }

            if (options.EnemyType != null)
                sim.EnemyType = string.IsNullOrWhiteSpace(options.EnemyType) ? null : options.EnemyType.Trim();
            if (options.WeaponType != null)
                sim.WeaponType = options.WeaponType.Trim();
            if (options.ForcedCatalogAction != null)
                sim.ForcedCatalogAction = string.IsNullOrWhiteSpace(options.ForcedCatalogAction)
                    ? null
                    : options.ForcedCatalogAction.Trim();
        }

        public static void ApplyAnalysis(BalanceTuningProfile profile, BalanceTuningRunOptions options)
        {
            var analysis = profile.Analysis;

            if (options.OptimizeWinRate != null)
                analysis.OptimizeWinRate = options.OptimizeWinRate.Value;
            if (options.MaxSuggestions != null)
                analysis.MaxSuggestions = options.MaxSuggestions.Value;

            analysis.FundamentalsTargets ??= new FundamentalsAnalysisTargets();
            var targets = analysis.FundamentalsTargets;

            if (options.TargetMedianCombinedActions != null)
                targets.TargetMedianCombinedActions = options.TargetMedianCombinedActions.Value;
            if (options.TargetMedianPlayerTurns != null)
                targets.TargetMedianPlayerTurns = options.TargetMedianPlayerTurns.Value;
            if (options.TargetMedianEnemyTurns != null)
                targets.TargetMedianEnemyTurns = options.TargetMedianEnemyTurns.Value;
            if (options.MinAverageActions != null)
                targets.MinAverageActions = options.MinAverageActions.Value;
            if (options.MaxAverageActions != null)
                targets.MaxAverageActions = options.MaxAverageActions.Value;
            if (options.MinAverageComboStreakRuns2Plus != null)
                targets.MinAverageComboStreakRuns2Plus = options.MinAverageComboStreakRuns2Plus.Value;
            if (options.MinAverageMaxComboStreak != null)
                targets.MinAverageMaxComboStreak = options.MinAverageMaxComboStreak.Value;
        }

        public static void ApplyAll(BalanceTuningProfile profile, BalanceTuningRunOptions options)
        {
            ApplySimulation(profile, options);
            ApplyAnalysis(profile, options);
        }

        public static SimulationRunOverrides BuildSimulationOverrides(BalanceTuningProfile profile, BalanceTuningRunOptions options)
        {
            int? encounters = options.EncounterCount;
            int? battles = options.BattlesPerCombination;

            if (profile.Simulation.Mode == TuningSimulationModes.FundamentalsEncounter)
            {
                if (encounters == null && battles != null)
                    encounters = battles;
            }
            else if (battles == null && encounters != null)
            {
                battles = encounters;
            }

            IReadOnlyList<int>? levels = null;
            if (!string.IsNullOrWhiteSpace(options.LevelsCsv))
            {
                levels = options.LevelsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToList();
            }
            else if (profile.Simulation.Levels != null)
            {
                levels = profile.Simulation.Levels;
            }

            return new SimulationRunOverrides
            {
                BattlesPerCombination = battles,
                BattlesPerWeapon = options.BattlesPerWeapon,
                NumberOfBattles = options.NumberOfBattles,
                Levels = levels,
                PlayerLevel = options.PlayerLevel,
                EnemyLevel = options.EnemyLevel,
                EncounterCount = encounters,
                WeaponType = options.WeaponType,
                EnemyType = options.EnemyType,
                ForcedCatalogAction = options.ForcedCatalogAction
            };
        }

        public static void PopulateOptionsFromProfile(BalanceTuningProfile profile, BalanceTuningRunOptions options)
        {
            var sim = profile.Simulation;
            var analysis = profile.Analysis;
            var targets = analysis.FundamentalsTargets ?? new FundamentalsAnalysisTargets();

            options.BattlesPerCombination = sim.BattlesPerCombination;
            options.BattlesPerWeapon = sim.BattlesPerWeapon;
            options.NumberOfBattles = sim.NumberOfBattles;
            options.PlayerLevel = sim.PlayerLevel;
            options.EnemyLevel = sim.EnemyLevel;
            options.EncounterCount = sim.EncounterCount;
            options.LevelsCsv = sim.Levels != null && sim.Levels.Count > 0
                ? string.Join(", ", sim.Levels)
                : "";
            options.EnemyType = sim.EnemyType ?? "";
            options.WeaponType = sim.WeaponType;
            options.ForcedCatalogAction = sim.ForcedCatalogAction ?? "";

            options.OptimizeWinRate = analysis.OptimizeWinRate;
            options.MaxSuggestions = analysis.MaxSuggestions;
            options.TargetMedianCombinedActions = targets.TargetMedianCombinedActions;
            options.TargetMedianPlayerTurns = targets.TargetMedianPlayerTurns;
            options.TargetMedianEnemyTurns = targets.TargetMedianEnemyTurns;
            options.MinAverageActions = targets.MinAverageActions;
            options.MaxAverageActions = targets.MaxAverageActions;
            options.MinAverageComboStreakRuns2Plus = targets.MinAverageComboStreakRuns2Plus;
            options.MinAverageMaxComboStreak = targets.MinAverageMaxComboStreak;
        }
    }
}
