using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGGame.Tuning
{
    public static class ClassPlaythroughBatchRunner
    {
        /// <summary>PLAYTODEATHBATCH --fights-json: every recorded fight with the run's class (balance-check per-enemy gate).</summary>
        public static string? FightsJsonPath { get; set; }

        public static async Task<ClassPlaythroughBatchResult> RunAsync(
            int runsPerClass = 5,
            string? classesCsv = null,
            int maxActionsPerRun = 500,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            if (runsPerClass < 1)
                throw new ArgumentOutOfRangeException(nameof(runsPerClass), "runsPerClass must be at least 1.");

            var classes = ClassPlaythroughClassResolver.ResolveClasses(classesCsv);
            var stopwatch = Stopwatch.StartNew();

            var batch = new ClassPlaythroughBatchResult
            {
                RunsPerClass = runsPerClass,
                MaxActionsPerRun = maxActionsPerRun,
                Timestamp = DateTime.UtcNow
            };

            int totalRuns = classes.Count * runsPerClass;
            int completed = 0;

            foreach (var (weaponType, menuSlot, displayName) in classes)
            {
                var runs = new List<PlaythroughRunResult>();

                for (int runIndex = 0; runIndex < runsPerClass; runIndex++)
                {
                    progress?.Report((completed, totalRuns,
                        $"{displayName} run {runIndex + 1}/{runsPerClass}"));

                    var run = await ClassPlaythroughRunner.RunAsync(
                        weaponType,
                        menuSlot,
                        displayName,
                        maxActionsPerRun);

                    runs.Add(run);
                    completed++;
                    progress?.Report((completed, totalRuns,
                        $"{displayName} run {runIndex + 1}/{runsPerClass} complete"));
                }

                batch.ClassAggregates.Add(
                    ClassPlaythroughAggregator.BuildAggregate(weaponType, displayName, runs));
            }

            stopwatch.Stop();
            batch.Elapsed = stopwatch.Elapsed;
            batch.ParityWarnings = ClassPlaythroughAggregator.BuildParityWarnings(batch.ClassAggregates);
            if (!string.IsNullOrEmpty(FightsJsonPath))
                WriteFightsJson(batch, FightsJsonPath);
            return batch;
        }

        private static void WriteFightsJson(ClassPlaythroughBatchResult batch, string path)
        {
            var rows = batch.ClassAggregates.SelectMany(a => a.Runs).SelectMany(r => r.Fights.Select(f => new
            {
                Class = r.ClassDisplayName, f.HeroLevel, Enemy = f.EnemyName, f.EnemyLevel, f.HeroActions, f.EnemyActions,
                f.FightInDungeon, f.HeroDied,
                HpLost = f.HeroMaxHp > 0 ? Math.Round(Math.Max(0, f.HeroHpStart - f.HeroHpEnd) / (double)f.HeroMaxHp, 3) : 0
            }));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
            System.IO.File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(rows));
        }

        public static string FormatReport(ClassPlaythroughBatchResult batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Class Playthrough Batch Report ===");
            sb.AppendLine($"Runs per class: {batch.RunsPerClass}");
            sb.AppendLine($"Max actions per run: {batch.MaxActionsPerRun}");
            sb.AppendLine($"Elapsed: {batch.Elapsed.TotalSeconds:F1}s");
            sb.AppendLine($"Timestamp: {batch.Timestamp:u}");
            sb.AppendLine();

            sb.AppendLine("Class           Runs  Death%  AvgLvl  MedLvl  AvgDng  AvgTurns");
            sb.AppendLine("--------------  ----  ------  ------  ------  ------  --------");

            foreach (var aggregate in batch.ClassAggregates.OrderBy(a => a.WeaponMenuSlotOrDefault()))
            {
                sb.AppendLine(
                    $"{aggregate.ClassDisplayName,-14}  " +
                    $"{aggregate.RunCount,4}  " +
                    $"{aggregate.DeathRate * 100,5:F0}%  " +
                    $"{aggregate.MeanFinalLevel,6:F1}  " +
                    $"{aggregate.MedianFinalLevel,6:F1}  " +
                    $"{aggregate.MeanDungeonsCompleted,6:F1}  " +
                    $"{aggregate.MeanTurnCount,8:F1}");
            }

            if (batch.ParityWarnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Parity warnings:");
                foreach (var warning in batch.ParityWarnings)
                    sb.AppendLine($"  - {warning}");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("No cross-class parity warnings.");
            }

            var allRuns = batch.ClassAggregates.SelectMany(a => a.Runs).ToList();
            if (ClassPlaythroughRunner.StopAtLevel > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Survival to L{ClassPlaythroughRunner.StopAtLevel}: " + string.Join("  ", batch.ClassAggregates.Select(a =>
                    $"{a.ClassDisplayName} {a.Runs.Count(r => r.StopReason == PlaythroughStopReason.ReachedLevel) * 100.0 / Math.Max(1, a.Runs.Count):F0}%"))
                    + $"  | all {allRuns.Count(r => r.StopReason == PlaythroughStopReason.ReachedLevel) * 100.0 / Math.Max(1, allRuns.Count):F0}%"
                    + $" | ran out of actions {allRuns.Count(r => r.StopReason == PlaythroughStopReason.MaxActions)}");
                sb.AppendLine($"Policy: {(ClassPlaythroughRunner.SmartPolicy ? "smart (skills, gear, sequence by simulation)" : "basic (equip by item score)")}");
                if (ClassPlaythroughRunner.RetreatBelowHpPercent > 0)
                    sb.AppendLine($"Retreat: leaves a dungeon below {ClassPlaythroughRunner.RetreatBelowHpPercent}% HP | mean retreats per run {allRuns.Average(r => r.DungeonsRetreated):F1}");
                sb.AppendLine($"Mean upgrades equipped per run: {allRuns.Average(r => r.UpgradesEquipped):F1}"
                    + (ClassPlaythroughRunner.SmartPolicy
                        ? $" | skills learned {allRuns.Average(r => r.SmartSkillsLearned):F1} | sequence changes {allRuns.Average(r => r.SmartSequenceChanges):F1} | evaluations {allRuns.Average(r => r.SmartEvaluations):F0}"
                        : ""));
                if (RPGGame.Diagnostics.ToolProfiler.Enabled)
                    sb.AppendLine("PROFILE:" + System.Environment.NewLine + RPGGame.Diagnostics.ToolProfiler.Report());
                if (ClassPlaythroughRunner.SmartPolicy && System.Environment.GetEnvironmentVariable("DF_SMARTPROF") == "1")
                    sb.AppendLine($"Smart profile: decisions {PlaythroughSmartPolicy.ProfOptimize.Elapsed.TotalSeconds:F0}s"
                        + $" (fight sims {PlaythroughSmartPolicy.ProfSim.Elapsed.TotalSeconds:F0}s, serialize {PlaythroughSmartPolicy.ProfSerialize.Elapsed.TotalSeconds:F0}s,"
                        + $" clone {PlaythroughSmartPolicy.ProfClone.Elapsed.TotalSeconds:F0}s), evaluations {PlaythroughSmartPolicy.ProfEvaluations}");
                // A run reached level N if it got there alive (dying at N still counts as having reached it).
                for (int lvl = Math.Max(1, ClassPlaythroughRunner.StopAtLevel - 1); lvl <= ClassPlaythroughRunner.StopAtLevel; lvl++)
                {
                    int target = lvl;
                    sb.AppendLine($"Reached L{target}: " + string.Join("  ", batch.ClassAggregates.Select(a =>
                        $"{a.ClassDisplayName} {a.Runs.Count(r => r.FinalLevel >= target) * 100.0 / Math.Max(1, a.Runs.Count):F0}%"))
                        + $"  | all {allRuns.Count(r => r.FinalLevel >= target) * 100.0 / Math.Max(1, allRuns.Count):F0}%");
                }
            }
            AppendDeathSection(sb, batch);
            AppendPerLevelFightSection(sb, allRuns);
            AppendPerLevelTurnSection(sb, batch);

            return sb.ToString();
        }

        /// <summary>Real per-fight outcomes by hero level: fights, losses, win rate, HP lost from the fight's starting HP.</summary>
        private static void AppendPerLevelFightSection(StringBuilder sb, List<PlaythroughRunResult> runs)
        {
            var fights = runs.SelectMany(r => r.Fights).ToList();
            if (fights.Count == 0)
                return;
            sb.AppendLine();
            sb.AppendLine("Per-level fights (playthrough reality):");
            sb.AppendLine("  Lvl  Fights  Losses   WinRate  HPlost/fight  HeroAct  EnemyAct  Dungeons  DungeonClear  Retreats");
            foreach (var g in fights.GroupBy(f => f.HeroLevel).OrderBy(g => g.Key))
            {
                int losses = g.Count(f => f.HeroDied);
                double hpLost = g.Average(f => f.HeroMaxHp > 0 ? Math.Max(0, f.HeroHpStart - f.HeroHpEnd) / (double)f.HeroMaxHp : 0);
                // A dungeon starts at its first fight; every death or retreat ends one uncleared,
                // so clear rate = 1 - (deaths + retreats) / dungeon starts.
                int dungeonStarts = Math.Max(1, g.Count(f => f.FightInDungeon == 1));
                int retreats = g.Count(f => f.RetreatedAfter);
                sb.AppendLine($"  {g.Key,3}  {g.Count(),6}  {losses,6}  {(1 - losses / (double)g.Count()) * 100,7:F2}%  {hpLost * 100,11:F0}%  {g.Average(f => f.HeroActions),7:F1}  {g.Average(f => f.EnemyActions),8:F1}  {dungeonStarts,8}  {(1 - (losses + retreats) / (double)dungeonStarts) * 100,11:F1}%  {retreats,8}");
            }
        }

        /// <summary>Where runs die: level, dungeon room, fight number since entering, HP at entry.</summary>
        private static void AppendDeathSection(StringBuilder sb, ClassPlaythroughBatchResult batch)
        {
            var deaths = batch.ClassAggregates
                .SelectMany(a => a.Runs.Where(r => r.Death != null).Select(r => (a.ClassDisplayName, Death: r.Death!)))
                .ToList();
            if (deaths.Count == 0)
                return;

            sb.AppendLine();
            sb.AppendLine($"Deaths ({deaths.Count}):");
            sb.AppendLine("  By hero level: " + string.Join("  ", deaths.GroupBy(d => d.Death.Level).OrderBy(g => g.Key).Select(g => $"L{g.Key}={g.Count()}")));
            sb.AppendLine("  By fight # within the dungeon: " + string.Join("  ", deaths.GroupBy(d => d.Death.FightsInDungeon).OrderBy(g => g.Key).Select(g => $"#{g.Key}={g.Count()}")));
            sb.AppendLine("  By room (room/total): " + string.Join("  ", deaths.GroupBy(d => $"{d.Death.RoomNumber}/{d.Death.TotalRooms}").OrderBy(g => g.Key).Select(g => $"{g.Key}={g.Count()}")));
            double entryPct = deaths.Average(d => d.Death.MaxHpAtDungeonEntry > 0 ? d.Death.HpAtDungeonEntry / (double)d.Death.MaxHpAtDungeonEntry : 0);
            sb.AppendLine($"  Mean HP at dungeon entry: {entryPct * 100:F0}% of max");
            sb.AppendLine("  Killers: " + string.Join("  ", deaths.GroupBy(d => d.Death.EnemyName).OrderByDescending(g => g.Count()).Take(8).Select(g => $"{g.Key}={g.Count()}")));
        }

        private static void AppendPerLevelTurnSection(StringBuilder sb, ClassPlaythroughBatchResult batch)
        {
            bool anyLevelData = batch.ClassAggregates.Any(a => a.MeanActionsByLevel.Count > 0);
            if (!anyLevelData)
                return;

            sb.AppendLine();
            sb.AppendLine("Avg actions spent at each hero level (menu + combat steps):");
            sb.AppendLine();

            int maxLevel = batch.ClassAggregates
                .SelectMany(a => a.MeanActionsByLevel)
                .Select(s => s.Level)
                .DefaultIfEmpty(0)
                .Max();

            sb.Append("Level ");
            for (int level = 1; level <= maxLevel; level++)
                sb.Append($"  L{level,2}");
            sb.AppendLine();

            foreach (var aggregate in batch.ClassAggregates.OrderBy(a => a.WeaponMenuSlotOrDefault()))
            {
                sb.Append($"{aggregate.ClassDisplayName,-14}");
                for (int level = 1; level <= maxLevel; level++)
                {
                    var stat = aggregate.MeanActionsByLevel.FirstOrDefault(s => s.Level == level);
                    if (stat == null)
                        sb.Append("     -");
                    else
                        sb.Append($"  {stat.MeanActions,4:F0}");
                }
                sb.AppendLine();
            }
        }
    }

    internal static class ClassPlaythroughAggregateExtensions
    {
        internal static int WeaponMenuSlotOrDefault(this ClassPlaythroughAggregate aggregate)
        {
            int index = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, aggregate.WeaponType);
            return index >= 0 ? index + 1 : 99;
        }
    }
}
