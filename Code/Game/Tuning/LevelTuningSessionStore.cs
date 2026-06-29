using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Utils;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Persists multi-level sim results and analysis between LEVELSIM / LEVELANALYZE / LEVELAPPLY steps.
    /// </summary>
    public static class LevelTuningSessionStore
    {
        public const string DefaultRelativePath = "GameData/BalancePatches/balance-tuning-session.json";
        public const string LegacyRelativePath = "GameData/BalancePatches/level-tuning-session.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string ResolvePath(string? overridePath = null)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
                return overridePath;

            string primary = Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath);
            if (File.Exists(primary))
                return primary;

            string legacy = Path.Combine(Directory.GetCurrentDirectory(), LegacyRelativePath);
            return File.Exists(legacy) ? legacy : primary;
        }

        public static LevelTuningSession Load(string? path = null)
        {
            path = ResolvePath(path);
            if (!File.Exists(path))
                return new LevelTuningSession();

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<LevelTuningSession>(json, JsonOptions) ?? new LevelTuningSession();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"LevelTuningSessionStore: load failed: {ex.Message}");
                return new LevelTuningSession();
            }
        }

        public static void Save(LevelTuningSession session, string? path = null)
        {
            path = ResolvePath(path);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            session.UpdatedUtc = DateTime.UtcNow;
            string json = JsonSerializer.Serialize(session, JsonOptions);
            File.WriteAllText(path, json);
        }

        public static MultiLevelSimulationResult ToSimulationResult(LevelTuningSession session)
        {
            if (session.Simulation == null)
                return new MultiLevelSimulationResult();

            var result = new MultiLevelSimulationResult
            {
                WorstLevel = session.Simulation.WorstLevel,
                WorstDeltaMagnitude = session.Simulation.WorstDeltaMagnitude,
                OverallCurveScore = session.Simulation.OverallCurveScore,
                AllAnchorsWithinTolerance = session.Simulation.AllAnchorsWithinTolerance,
                Timestamp = session.Simulation.TimestampUtc,
                BattlesPerCombination = session.Simulation.BattlesPerCombination,
                LevelSnapshots = session.Simulation.Snapshots?.Select(s => new LevelSimulationSnapshot
                {
                    Level = s.Level,
                    TargetWinRate = s.TargetWinRate,
                    ActualWinRate = s.ActualWinRate,
                    AverageTurns = s.AverageTurns,
                    WithinTolerance = s.WithinTolerance,
                    ConformanceScore = s.ConformanceScore,
                    TotalBattles = s.TotalBattles
                }).ToList() ?? new List<LevelSimulationSnapshot>()
            };

            return result;
        }

        public static SimulationSessionDto FromSimulationResult(MultiLevelSimulationResult result) =>
            new SimulationSessionDto
            {
                WorstLevel = result.WorstLevel,
                WorstDeltaMagnitude = result.WorstDeltaMagnitude,
                OverallCurveScore = result.OverallCurveScore,
                AllAnchorsWithinTolerance = result.AllAnchorsWithinTolerance,
                TimestampUtc = result.Timestamp,
                BattlesPerCombination = result.BattlesPerCombination,
                Snapshots = result.LevelSnapshots.Select(s => new SnapshotDto
                {
                    Level = s.Level,
                    TargetWinRate = s.TargetWinRate,
                    ActualWinRate = s.ActualWinRate,
                    AverageTurns = s.AverageTurns,
                    WithinTolerance = s.WithinTolerance,
                    ConformanceScore = s.ConformanceScore,
                    TotalBattles = s.TotalBattles
                }).ToList()
            };
    }

    public sealed class LevelTuningSession
    {
        public string Version { get; set; } = "2.0";
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public string? ProfileId { get; set; }
        public string? ProfileName { get; set; }
        public string? SimulationMode { get; set; }
        public SimulationSessionDto? Simulation { get; set; }
        public ComprehensiveSimulationDto? Comprehensive { get; set; }
        public FundamentalsSimulationDto? Fundamentals { get; set; }
        public AnalysisSessionDto? Analysis { get; set; }
        public ApplySessionDto? LastApply { get; set; }

        /// <summary>Analysis config used for the last sim run (includes UI overrides).</summary>
        public AnalysisProfileConfig? RunAnalysisConfig { get; set; }
    }

    public sealed class SimulationSessionDto
    {
        public int WorstLevel { get; set; }
        public double WorstDeltaMagnitude { get; set; }
        public double OverallCurveScore { get; set; }
        public bool AllAnchorsWithinTolerance { get; set; }
        public DateTime TimestampUtc { get; set; }
        public int BattlesPerCombination { get; set; }
        public List<SnapshotDto> Snapshots { get; set; } = new();
    }

    public sealed class SnapshotDto
    {
        public int Level { get; set; }
        public double TargetWinRate { get; set; }
        public double ActualWinRate { get; set; }
        public double AverageTurns { get; set; }
        public bool WithinTolerance { get; set; }
        public double ConformanceScore { get; set; }
        public int TotalBattles { get; set; }
        public double Delta => ActualWinRate - TargetWinRate;
    }

    public sealed class AnalysisSessionDto
    {
        public double QualityScore { get; set; }
        public string Summary { get; set; } = "";
        public bool AllAnchorsPass { get; set; }
        public string? PrimaryDial { get; set; }
        public string? DialDiagnosis { get; set; }
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public SuggestionDto? TopSuggestion { get; set; }
    }

    public sealed class SuggestionDto
    {
        public string Id { get; set; } = "";
        public string Category { get; set; } = "";
        public string Parameter { get; set; } = "";
        public double CurrentValue { get; set; }
        public double SuggestedValue { get; set; }
        public string Reason { get; set; } = "";
        public string Impact { get; set; } = "";
    }

    public sealed class ApplySessionDto
    {
        public DateTime AppliedUtc { get; set; }
        public string SuggestionId { get; set; } = "";
        public bool Success { get; set; }
        public string? PatchName { get; set; }
        public string Message { get; set; } = "";
    }

    public sealed class FundamentalsSimulationDto
    {
        public DateTime TimestampUtc { get; set; }
        public int EncounterCount { get; set; }
        public int SuccessfulEncounters { get; set; }
        public int ErroredEncounters { get; set; }
        public double AverageActionsPerEncounter { get; set; }
        public double MedianActionsPerEncounter { get; set; }
        public double AveragePlayerTurnsPerEncounter { get; set; }
        public double MedianPlayerTurnsPerEncounter { get; set; }
        public double AverageEnemyTurnsPerEncounter { get; set; }
        public double MedianEnemyTurnsPerEncounter { get; set; }
        public int MinActions { get; set; }
        public int MaxActions { get; set; }
        public double AverageComboPlusEventsPerEncounter { get; set; }
        public double AverageComboStreakRuns2PlusPerEncounter { get; set; }
        public double AverageMaxComboStreak { get; set; }
        public Dictionary<int, int> ComboStreakRunTotals { get; set; } = new();
        public string ForcedCatalogAction { get; set; } = "";
        public int ComboStripCount { get; set; }
        public string? EnemyType { get; set; }
        public int PlayerLevel { get; set; }
        public int EnemyLevel { get; set; }
        public string WeaponType { get; set; } = "Sword";
        public double WinRate { get; set; }
        public double TurnDurationStdDev { get; set; }
        public double AverageMissRate { get; set; }
        public double AverageCritRate { get; set; }
        public double AverageLossSeverity { get; set; }
        public double AverageTurnsBelowZero { get; set; }
        public bool ContinuePastZeroHp { get; set; }
        public List<FundamentalsLevelSnapshotDto> LevelSnapshots { get; set; } = new();
    }

    public sealed class FundamentalsLevelSnapshotDto
    {
        public int Level { get; set; }
        public int EncounterCount { get; set; }
        public int SuccessfulEncounters { get; set; }
        public double MedianCombinedActions { get; set; }
        public double AverageCombinedActions { get; set; }
        public double MedianPlayerTurns { get; set; }
        public double MedianEnemyTurns { get; set; }
        public double WinRate { get; set; }
    }
}
