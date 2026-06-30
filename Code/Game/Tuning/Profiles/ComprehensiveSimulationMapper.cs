using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// JSON-friendly comprehensive sim snapshot (no per-battle detail).
    /// </summary>
    public sealed class ComprehensiveSimulationDto
    {
        public int PlayerLevel { get; set; }
        public int EnemyLevel { get; set; }
        public int BattlesPerCombination { get; set; }
        public DateTime TimestampUtc { get; set; }
        public double OverallWinRate { get; set; }
        public double OverallAverageTurns { get; set; }
        public int TotalBattles { get; set; }
        public List<WeaponStatDto> Weapons { get; set; } = new();
        public List<EnemyStatDto> Enemies { get; set; } = new();
        public List<CombinationStatDto> Combinations { get; set; } = new();
    }

    public sealed class WeaponStatDto
    {
        public string WeaponType { get; set; } = "";
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public int TotalBattles { get; set; }
    }

    public sealed class EnemyStatDto
    {
        public string EnemyType { get; set; } = "";
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public int TotalBattles { get; set; }
    }

    public sealed class CombinationStatDto
    {
        public string WeaponType { get; set; } = "";
        public string EnemyType { get; set; } = "";
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public int TotalBattles { get; set; }
    }

    public static class ComprehensiveSimulationMapper
    {
        public static ComprehensiveSimulationDto FromResult(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult result,
            int playerLevel,
            int enemyLevel,
            int battlesPerCombination)
        {
            return new ComprehensiveSimulationDto
            {
                PlayerLevel = playerLevel,
                EnemyLevel = enemyLevel,
                BattlesPerCombination = battlesPerCombination,
                TimestampUtc = DateTime.UtcNow,
                OverallWinRate = result.OverallWinRate,
                OverallAverageTurns = result.OverallAverageTurns,
                TotalBattles = result.TotalBattles,
                Weapons = result.WeaponStatistics.Select(kvp => new WeaponStatDto
                {
                    WeaponType = kvp.Key.ToString(),
                    WinRate = kvp.Value.WinRate,
                    AverageTurns = kvp.Value.AverageTurns,
                    TotalBattles = kvp.Value.TotalBattles
                }).ToList(),
                Enemies = result.EnemyStatistics.Select(kvp => new EnemyStatDto
                {
                    EnemyType = kvp.Key,
                    WinRate = kvp.Value.WinRate,
                    AverageTurns = kvp.Value.AverageTurns,
                    TotalBattles = kvp.Value.TotalBattles
                }).ToList(),
                Combinations = result.CombinationResults.Select(c => new CombinationStatDto
                {
                    WeaponType = c.WeaponType.ToString(),
                    EnemyType = c.EnemyType,
                    WinRate = c.WinRate,
                    AverageTurns = c.AverageTurns,
                    TotalBattles = c.TotalBattles
                }).ToList()
            };
        }

        public static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult ToResult(ComprehensiveSimulationDto dto)
        {
            var result = new BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult
            {
                OverallWinRate = dto.OverallWinRate,
                OverallAverageTurns = dto.OverallAverageTurns,
                TotalBattles = dto.TotalBattles,
                WeaponTypes = dto.Weapons
                    .Select(w => Enum.TryParse<WeaponType>(w.WeaponType, out var wt) ? wt : WeaponType.Sword)
                    .Distinct()
                    .ToList(),
                EnemyTypes = dto.Enemies.Select(e => e.EnemyType).Distinct().ToList()
            };

            foreach (var w in dto.Weapons)
            {
                if (!Enum.TryParse<WeaponType>(w.WeaponType, out var weaponType))
                    continue;

                result.WeaponStatistics[weaponType] = new BattleStatisticsRunner.WeaponOverallStats
                {
                    WinRate = w.WinRate,
                    AverageTurns = w.AverageTurns,
                    TotalBattles = w.TotalBattles
                };
            }

            foreach (var e in dto.Enemies)
            {
                result.EnemyStatistics[e.EnemyType] = new BattleStatisticsRunner.EnemyOverallStats
                {
                    WinRate = e.WinRate,
                    AverageTurns = e.AverageTurns,
                    TotalBattles = e.TotalBattles
                };
            }

            foreach (var c in dto.Combinations)
            {
                if (!Enum.TryParse<WeaponType>(c.WeaponType, out var weaponType))
                    continue;

                result.CombinationResults.Add(new BattleStatisticsRunner.WeaponEnemyCombinationResult
                {
                    WeaponType = weaponType,
                    EnemyType = c.EnemyType,
                    WinRate = c.WinRate,
                    AverageTurns = c.AverageTurns,
                    TotalBattles = c.TotalBattles
                });
            }

            return result;
        }

        public static FundamentalsSimulationDto FromFundamentalsResult(FundamentalsSimulationResult result) =>
            new FundamentalsSimulationDto
            {
                TimestampUtc = result.TimestampUtc,
                EncounterCount = result.EncounterCount,
                SuccessfulEncounters = result.SuccessfulEncounters,
                ErroredEncounters = result.ErroredEncounters,
                AverageActionsPerEncounter = result.AverageActionsPerEncounter,
                MedianActionsPerEncounter = result.MedianActionsPerEncounter,
                AveragePlayerTurnsPerEncounter = result.AveragePlayerTurnsPerEncounter,
                MedianPlayerTurnsPerEncounter = result.MedianPlayerTurnsPerEncounter,
                AverageEnemyTurnsPerEncounter = result.AverageEnemyTurnsPerEncounter,
                MedianEnemyTurnsPerEncounter = result.MedianEnemyTurnsPerEncounter,
                MinActions = result.MinActions,
                MaxActions = result.MaxActions,
                AverageComboPlusEventsPerEncounter = result.AverageComboPlusEventsPerEncounter,
                AverageComboStreakRuns2PlusPerEncounter = result.AverageComboStreakRuns2PlusPerEncounter,
                AverageMaxComboStreak = result.AverageMaxComboStreak,
                ComboStreakRunTotals = new Dictionary<int, int>(result.ComboStreakRunTotals),
                ForcedCatalogAction = result.ForcedCatalogAction,
                ComboStripCount = result.ComboStripCount,
                EnemyType = result.EnemyType,
                PlayerLevel = result.PlayerLevel,
                EnemyLevel = result.EnemyLevel,
                WeaponType = result.WeaponType,
                WinRate = result.WinRate,
                TurnDurationStdDev = result.TurnDurationStdDev,
                AverageMissRate = result.AverageMissRate,
                AverageCritRate = result.AverageCritRate,
                AverageLossSeverity = result.AverageLossSeverity,
                AverageTurnsBelowZero = result.AverageTurnsBelowZero,
                ContinuePastZeroHp = result.ContinuePastZeroHp,
                LevelSnapshots = result.LevelSnapshots.Select(s => new FundamentalsLevelSnapshotDto
                {
                    Level = s.Level,
                    EncounterCount = s.EncounterCount,
                    SuccessfulEncounters = s.SuccessfulEncounters,
                    MedianCombinedActions = s.MedianCombinedActions,
                    AverageCombinedActions = s.AverageCombinedActions,
                    MedianPlayerTurns = s.MedianPlayerTurns,
                    MedianEnemyTurns = s.MedianEnemyTurns,
                    WinRate = s.WinRate
                }).ToList()
            };

        public static FundamentalsSimulationResult ToFundamentalsResult(FundamentalsSimulationDto dto) =>
            new FundamentalsSimulationResult
            {
                TimestampUtc = dto.TimestampUtc,
                EncounterCount = dto.EncounterCount,
                SuccessfulEncounters = dto.SuccessfulEncounters,
                ErroredEncounters = dto.ErroredEncounters,
                AverageActionsPerEncounter = dto.AverageActionsPerEncounter,
                MedianActionsPerEncounter = dto.MedianActionsPerEncounter,
                AveragePlayerTurnsPerEncounter = dto.AveragePlayerTurnsPerEncounter,
                MedianPlayerTurnsPerEncounter = dto.MedianPlayerTurnsPerEncounter,
                AverageEnemyTurnsPerEncounter = dto.AverageEnemyTurnsPerEncounter,
                MedianEnemyTurnsPerEncounter = dto.MedianEnemyTurnsPerEncounter,
                MinActions = dto.MinActions,
                MaxActions = dto.MaxActions,
                AverageComboPlusEventsPerEncounter = dto.AverageComboPlusEventsPerEncounter,
                AverageComboStreakRuns2PlusPerEncounter = dto.AverageComboStreakRuns2PlusPerEncounter,
                AverageMaxComboStreak = dto.AverageMaxComboStreak,
                ComboStreakRunTotals = new Dictionary<int, int>(dto.ComboStreakRunTotals),
                ForcedCatalogAction = dto.ForcedCatalogAction,
                ComboStripCount = dto.ComboStripCount,
                EnemyType = dto.EnemyType,
                PlayerLevel = dto.PlayerLevel,
                EnemyLevel = dto.EnemyLevel,
                WeaponType = dto.WeaponType,
                WinRate = dto.WinRate,
                TurnDurationStdDev = dto.TurnDurationStdDev,
                AverageMissRate = dto.AverageMissRate,
                AverageCritRate = dto.AverageCritRate,
                AverageLossSeverity = dto.AverageLossSeverity,
                AverageTurnsBelowZero = dto.AverageTurnsBelowZero,
                ContinuePastZeroHp = dto.ContinuePastZeroHp,
                LevelSnapshots = dto.LevelSnapshots.Select(s => new FundamentalsLevelSnapshot
                {
                    Level = s.Level,
                    EncounterCount = s.EncounterCount,
                    SuccessfulEncounters = s.SuccessfulEncounters,
                    MedianCombinedActions = s.MedianCombinedActions,
                    AverageCombinedActions = s.AverageCombinedActions,
                    MedianPlayerTurns = s.MedianPlayerTurns,
                    MedianEnemyTurns = s.MedianEnemyTurns,
                    WinRate = s.WinRate
                }).ToList()
            };

        public static TuningSimulationOutcome ToOutcome(LevelTuningSession session)
        {
            if (session.PlaythroughBatch != null)
            {
                var batch = PlaythroughSimulationMapper.ToBatch(session.PlaythroughBatch);
                return new TuningSimulationOutcome
                {
                    Mode = TuningSimulationModes.ClassPlaythroughBatch,
                    ProfileId = session.ProfileId ?? "",
                    PlaythroughBatch = batch,
                    PlaythroughSnapshot = session.PlaythroughBatch,
                    TimestampUtc = session.PlaythroughBatch.TimestampUtc
                };
            }

            if (session.Fundamentals != null)
            {
                var fundamentals = ToFundamentalsResult(session.Fundamentals);
                return new TuningSimulationOutcome
                {
                    Mode = TuningSimulationModes.FundamentalsEncounter,
                    ProfileId = session.ProfileId ?? "",
                    Fundamentals = fundamentals,
                    PlayerLevel = fundamentals.PlayerLevel,
                    EnemyLevel = fundamentals.EnemyLevel,
                    TimestampUtc = fundamentals.TimestampUtc
                };
            }

            if (session.Comprehensive != null)
            {
                return new TuningSimulationOutcome
                {
                    Mode = TuningSimulationModes.ComprehensiveWeaponEnemy,
                    ProfileId = session.ProfileId ?? "",
                    Comprehensive = ToResult(session.Comprehensive),
                    PlayerLevel = session.Comprehensive.PlayerLevel,
                    EnemyLevel = session.Comprehensive.EnemyLevel,
                    BattlesPerCombination = session.Comprehensive.BattlesPerCombination,
                    TimestampUtc = session.Comprehensive.TimestampUtc
                };
            }

            var multi = LevelTuningSessionStore.ToSimulationResult(session);
            return new TuningSimulationOutcome
            {
                Mode = session.SimulationMode ?? TuningSimulationModes.MultiLevelWeaponEnemy,
                ProfileId = session.ProfileId ?? "",
                MultiLevel = multi,
                BattlesPerCombination = multi.BattlesPerCombination,
                TimestampUtc = multi.Timestamp
            };
        }
    }
}
