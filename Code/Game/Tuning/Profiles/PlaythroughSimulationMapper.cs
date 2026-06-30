using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning.Profiles
{
    public sealed class PlaythroughSimulationDto
    {
        public DateTime TimestampUtc { get; set; }
        public int RunsPerClass { get; set; }
        public int MaxActionsPerRun { get; set; }
        public double ElapsedSeconds { get; set; }
        public double OverallMeanFinalLevel { get; set; }
        public double OverallMeanDungeonsCompleted { get; set; }
        public double LevelSpread { get; set; }
        public double DungeonSpread { get; set; }
        public bool HasParityWarnings { get; set; }
        public List<string> ParityWarnings { get; set; } = new();
        public double QualityScore { get; set; }
        public List<PlaythroughClassAggregateDto> Classes { get; set; } = new();
    }

    public sealed class PlaythroughClassAggregateDto
    {
        public string ClassDisplayName { get; set; } = "";
        public string WeaponType { get; set; } = "";
        public int RunCount { get; set; }
        public double DeathRate { get; set; }
        public double MeanFinalLevel { get; set; }
        public double MedianFinalLevel { get; set; }
        public double MeanDungeonsCompleted { get; set; }
        public double MeanTurnCount { get; set; }
        public List<PlaythroughLevelTurnDto> MeanActionsByLevel { get; set; } = new();
    }

    public sealed class PlaythroughLevelTurnDto
    {
        public int Level { get; set; }
        public double MeanActions { get; set; }
        public int SampleRunCount { get; set; }
    }

    public static class PlaythroughSimulationMapper
    {
        public static PlaythroughSimulationDto FromBatch(ClassPlaythroughBatchResult batch, PlaythroughAnalysisTargets? targets = null)
        {
            targets ??= new PlaythroughAnalysisTargets();

            var dto = new PlaythroughSimulationDto
            {
                TimestampUtc = batch.Timestamp,
                RunsPerClass = batch.RunsPerClass,
                MaxActionsPerRun = batch.MaxActionsPerRun,
                ElapsedSeconds = batch.Elapsed.TotalSeconds,
                HasParityWarnings = batch.HasParityWarnings,
                ParityWarnings = batch.ParityWarnings.ToList(),
                Classes = batch.ClassAggregates.Select(a => new PlaythroughClassAggregateDto
                {
                    ClassDisplayName = a.ClassDisplayName,
                    WeaponType = a.WeaponType.ToString(),
                    RunCount = a.RunCount,
                    DeathRate = a.DeathRate,
                    MeanFinalLevel = a.MeanFinalLevel,
                    MedianFinalLevel = a.MedianFinalLevel,
                    MeanDungeonsCompleted = a.MeanDungeonsCompleted,
                    MeanTurnCount = a.MeanTurnCount,
                    MeanActionsByLevel = a.MeanActionsByLevel.Select(s => new PlaythroughLevelTurnDto
                    {
                        Level = s.Level,
                        MeanActions = s.MeanActions,
                        SampleRunCount = s.SampleRunCount
                    }).ToList()
                }).ToList()
            };

            if (batch.ClassAggregates.Count > 0)
            {
                dto.OverallMeanFinalLevel = batch.ClassAggregates.Average(a => a.MeanFinalLevel);
                dto.OverallMeanDungeonsCompleted = batch.ClassAggregates.Average(a => a.MeanDungeonsCompleted);
                var levels = batch.ClassAggregates.Select(a => a.MeanFinalLevel).ToList();
                var dungeons = batch.ClassAggregates.Select(a => a.MeanDungeonsCompleted).ToList();
                dto.LevelSpread = levels.Max() - levels.Min();
                dto.DungeonSpread = dungeons.Max() - dungeons.Min();
            }

            dto.QualityScore = CalculateQualityScore(dto, targets);
            return dto;
        }

        public static ClassPlaythroughBatchResult ToBatch(PlaythroughSimulationDto dto)
        {
            var batch = new ClassPlaythroughBatchResult
            {
                RunsPerClass = dto.RunsPerClass,
                MaxActionsPerRun = dto.MaxActionsPerRun,
                Timestamp = dto.TimestampUtc,
                Elapsed = TimeSpan.FromSeconds(dto.ElapsedSeconds),
                ParityWarnings = dto.ParityWarnings.ToList()
            };

            foreach (var c in dto.Classes)
            {
                Enum.TryParse<WeaponType>(c.WeaponType, out var weaponType);
                batch.ClassAggregates.Add(new ClassPlaythroughAggregate
                {
                    WeaponType = weaponType,
                    ClassDisplayName = c.ClassDisplayName,
                    RunCount = c.RunCount,
                    DeathRate = c.DeathRate,
                    MeanFinalLevel = c.MeanFinalLevel,
                    MedianFinalLevel = c.MedianFinalLevel,
                    MeanDungeonsCompleted = c.MeanDungeonsCompleted,
                    MeanTurnCount = c.MeanTurnCount,
                    MeanActionsByLevel = c.MeanActionsByLevel.Select(s => new PlaythroughLevelTurnStats
                    {
                        Level = s.Level,
                        MeanActions = s.MeanActions,
                        SampleRunCount = s.SampleRunCount
                    }).ToList()
                });
            }

            return batch;
        }

        public static double CalculateQualityScore(PlaythroughSimulationDto dto, PlaythroughAnalysisTargets targets)
        {
            double levelScore = ScoreHigherIsBetter(dto.OverallMeanFinalLevel, targets.MinMeanFinalLevel, targets.MinMeanFinalLevel + 3);
            double dungeonScore = ScoreHigherIsBetter(dto.OverallMeanDungeonsCompleted, targets.MinMeanDungeonsCompleted, targets.MinMeanDungeonsCompleted + 2);
            double parityScore = 100;

            if (dto.LevelSpread > targets.MaxLevelSpread)
                parityScore -= Math.Min(50, (dto.LevelSpread - targets.MaxLevelSpread) * 15);
            if (dto.DungeonSpread > targets.MaxDungeonSpread)
                parityScore -= Math.Min(50, (dto.DungeonSpread - targets.MaxDungeonSpread) * 15);
            if (dto.HasParityWarnings)
                parityScore -= 10;

            return Math.Clamp((levelScore * 0.4) + (dungeonScore * 0.35) + (Math.Max(0, parityScore) * 0.25), 0, 100);
        }

        private static double ScoreHigherIsBetter(double value, double target, double excellent)
        {
            if (value >= excellent)
                return 100;
            if (value >= target)
                return 60 + 40 * ((value - target) / Math.Max(0.001, excellent - target));
            if (value <= 0)
                return 0;
            return Math.Max(0, 60 * (value / Math.Max(0.001, target)));
        }
    }
}
