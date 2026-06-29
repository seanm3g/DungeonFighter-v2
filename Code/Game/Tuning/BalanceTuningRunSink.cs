using System;

namespace RPGGame.Tuning
{
    public enum BalanceTuningLogLevel
    {
        Info,
        Progress,
        Success,
        Warning,
        Error,
        Idea,
        Change
    }

    public enum BalanceTuningRunStep
    {
        Idle,
        Initializing,
        Simulate,
        Analyze,
        Apply,
        Completed,
        Cancelled,
        Failed
    }

    public enum BalanceTuningRunMode
    {
        FullCycle,
        SimulateOnly,
        AnalyzeOnly,
        SimulateAndAnalyze
    }

    public sealed class BalanceTuningLogEntry
    {
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public BalanceTuningLogLevel Level { get; init; }
        public string Message { get; init; } = "";
        public int Iteration { get; init; }
        public int MaxIterations { get; init; }
    }

    public sealed class BalanceTuningChangeEntry
    {
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public int Iteration { get; init; }
        public string ProfileId { get; init; } = "";
        public string Parameter { get; init; } = "";
        public double FromValue { get; init; }
        public double ToValue { get; init; }
        public string Reason { get; init; } = "";
        public string? PatchName { get; init; }
        public bool Applied { get; init; }
    }

    public sealed class BalanceTuningIterationResult
    {
        public int Iteration { get; init; }
        public int MaxIterations { get; init; }
        public string ProfileId { get; init; } = "";
        public string ReportText { get; init; } = "";
        public double QualityScore { get; init; }
        public bool AllChecksPass { get; init; }
        public bool SuggestionAvailable { get; init; }
        public bool ChangeApplied { get; init; }
    }

    public interface IBalanceTuningRunSink
    {
        void SetStep(BalanceTuningRunStep step, int iteration, int maxIterations, string? detail = null);
        void Log(BalanceTuningLogLevel level, string message, int iteration = 0, int maxIterations = 0);
        void ReportProgress(int completed, int total, string status, int iteration, int maxIterations);
        void RecordIdea(int iteration, string title, string detail);
        void RecordChange(BalanceTuningChangeEntry entry);
        void IterationCompleted(BalanceTuningIterationResult result);
    }

    /// <summary>Writes to console and optionally forwards to a UI sink.</summary>
    public sealed class CompositeBalanceTuningRunSink : IBalanceTuningRunSink
    {
        private readonly IBalanceTuningRunSink? _forward;

        public CompositeBalanceTuningRunSink(IBalanceTuningRunSink? forward = null) => _forward = forward;

        public void SetStep(BalanceTuningRunStep step, int iteration, int maxIterations, string? detail = null)
        {
            _forward?.SetStep(step, iteration, maxIterations, detail);
        }

        public void Log(BalanceTuningLogLevel level, string message, int iteration = 0, int maxIterations = 0)
        {
            if (level != BalanceTuningLogLevel.Progress)
                Console.WriteLine(message);
            _forward?.Log(level, message, iteration, maxIterations);
        }

        public void ReportProgress(int completed, int total, string status, int iteration, int maxIterations)
        {
            int pct = total > 0 ? (int)((double)completed / total * 100) : 0;
            Console.Write($"\r[{iteration}/{maxIterations}] [{completed}/{total}] {pct}% - {status}".PadRight(100));
            if (completed >= total && total > 0)
                Console.WriteLine();
            _forward?.ReportProgress(completed, total, status, iteration, maxIterations);
        }

        public void RecordIdea(int iteration, string title, string detail)
        {
            Log(BalanceTuningLogLevel.Idea, $"Idea (iter {iteration}): {title} — {detail}");
            _forward?.RecordIdea(iteration, title, detail);
        }

        public void RecordChange(BalanceTuningChangeEntry entry)
        {
            Log(BalanceTuningLogLevel.Change,
                $"Change (iter {entry.Iteration}): {entry.Parameter} {entry.FromValue} → {entry.ToValue} ({entry.Reason})");
            _forward?.RecordChange(entry);
        }

        public void IterationCompleted(BalanceTuningIterationResult result) =>
            _forward?.IterationCompleted(result);
    }
}
