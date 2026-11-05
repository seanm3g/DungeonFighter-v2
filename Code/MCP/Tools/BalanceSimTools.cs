using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// MCP access to the current balance pipeline (Scripts/dungeon-sim.py, bo-solve.py, balance-scan.py,
    /// median-curve-solve.py --check). Sims take minutes, so each one runs as a background job: start_* returns a job id
    /// and get_balance_job reports progress and the result. Scripts run from the repo root against the DF.dll this server
    /// was built from, so a sim measures the same code the server runs.
    /// </summary>
    public static class BalanceSimTools
    {
        private const int MaxOutputChars = 400_000;

        private sealed class Job
        {
            public string Id = "";
            public string Kind = "";
            public string CommandLine = "";
            public string? JsonPath;
            public DateTime StartedUtc;
            public DateTime? EndedUtc;
            public int? ExitCode;
            public Process? Process;
            public readonly StringBuilder Output = new();
            public bool Cancelled;
            public bool Running => EndedUtc == null;
        }

        private static readonly ConcurrentDictionary<string, Job> Jobs = new();
        private static int _jobCounter;

        // ---------- context ----------

        public static Task<string> GetBalanceContext() => McpToolExecutor.ExecuteAsync(() =>
        {
            string root = FindRepoRoot();
            string gameData = Path.Combine(root, "GameData");
            string activePatch = ReadActivePatch(gameData);
            double? enemyHealthMult = ReadJsonDouble(Path.Combine(gameData, "GeneralSettings.json"), "enemyHealthMultiplier");

            JsonElement? targets = null;
            string targetsPath = Path.Combine(gameData, "TuningProfiles", "balance-targets.json");
            if (File.Exists(targetsPath))
                targets = JsonDocument.Parse(File.ReadAllText(targetsPath)).RootElement.Clone();

            var warnings = new List<string>();
            if (enemyHealthMult is double m && Math.Abs(m - 1.0) > 1e-9)
                warnings.Add($"GameData/GeneralSettings.json enemyHealthMultiplier is {m}: every sim inherits it and scales enemy HP. Ask the user before changing it.");
            string? gdOverride = System.Environment.GetEnvironmentVariable("DF_GAMEDATA_DIR");
            if (!string.IsNullOrEmpty(gdOverride))
                warnings.Add($"DF_GAMEDATA_DIR is set to {gdOverride} in the server environment.");

            return new
            {
                repoRoot = root,
                activeBalancePatch = activePatch,
                activePatchPath = Path.Combine(gameData, "Patches", "Balance", activePatch + ".json"),
                simToolsDir = DefaultToolsDir(),
                enemyHealthMultiplier = enemyHealthMult,
                warnings,
                realClearAnchors = targets?.TryGetProperty("realClearAnchors", out var rc) == true ? rc : (JsonElement?)null,
                heroTurnsAnchors = targets?.TryGetProperty("heroTurnsAnchors", out var ht) == true ? ht : (JsonElement?)null,
                retreatBelowHpPercent = targets?.TryGetProperty("retreatBelowHpPercent", out var rt) == true ? rt : (JsonElement?)null,
                workflow = new[]
                {
                    "Balance is judged on real dungeons (DUNGEONSIM: HP carried, no post-fight heal, retreat rule, rules bot), never single-fight win rate.",
                    "Enemy health bands set fight length (heroTurnsAnchors); damage bands set clear rate (realClearAnchors). Bands are 5 levels; test band midpoints (8, 13, 18, 33, 48...).",
                    "1 start_balance_scan (gross misses) -> 2 start_bo_solve per flagged band midpoint, or start_median_check for fight length -> 3 write the change into a NEW patch copy in GameData/Patches/Balance -> 4 start_dungeon_sim on a fresh seed for base and candidate (>= 8 shards) -> 5 balance-check.ps1 gate -> ask the user before activating it.",
                    "Legacy tools (run_battle_simulation, validate_balance, suggest_tuning, run_full_cycle, balance_tuner_agent_run, adjust_*, test_what_if...) measure single-fight win rate on a bare hero; do not tune with them.",
                    "Full reference: Documentation/BALANCE_PROCESS.md and .claude/commands/balance.md."
                }
            };
        }, writeIndented: true);

        // ---------- starters ----------

        public static Task<string> StartDungeonSim(string levels, string? patch, int shards, int runs, int dungeons, int seed,
            string? classes, string? bot, bool regionMix, string? regionTheme, string? anchors, string? toolsDir)
        {
            var args = new List<string> { "Scripts/dungeon-sim.py", "--levels", levels,
                "--shards", shards.ToString(), "--runs", runs.ToString(), "--dungeons", dungeons.ToString(), "--seed", seed.ToString() };
            AddOpt(args, "--patch", patch);
            AddOpt(args, "--classes", classes);
            AddOpt(args, "--bot", bot);
            if (regionMix) args.Add("--region-mix");
            AddOpt(args, "--region-theme", regionTheme);
            AddAnchors(args, anchors);
            return Start("dungeon-sim", args, toolsDir, jsonFlag: "--json");
        }

        public static Task<string> StartBoSolve(int level, double? target, string? patch, string? scope, int spark,
            int seed, double tolerance, int maxRounds, string? anchors, string? toolsDir)
        {
            var args = new List<string> { "Scripts/bo-solve.py", "--level", level.ToString(), "--seed", seed.ToString(),
                "--tolerance", tolerance.ToString(System.Globalization.CultureInfo.InvariantCulture), "--max-rounds", maxRounds.ToString() };
            if (target is double t) { args.Add("--target"); args.Add(t.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            AddOpt(args, "--patch", patch);
            AddOpt(args, "--scope", scope);
            if (spark > 0) { args.Add("--spark"); args.Add(spark.ToString()); }
            AddAnchors(args, anchors);
            return Start("bo-solve", args, toolsDir, jsonFlag: "--json");
        }

        public static Task<string> StartBalanceScan(string? patch, string? medianLevels, string? dungeonLevels,
            int heroes, string? compare, string? toolsDir)
        {
            var args = new List<string> { "Scripts/balance-scan.py", "--skip-build", "--heroes", heroes.ToString() };
            AddOpt(args, "--patch", patch);
            AddOpt(args, "--median-levels", medianLevels);
            if (dungeonLevels != null) { args.Add("--dungeon-levels"); args.Add(dungeonLevels); }
            AddOpt(args, "--compare", compare);
            return Start("balance-scan", args, toolsDir, jsonFlag: "--json");
        }

        public static Task<string> StartMedianCheck(string? patch, string? levels, string? toolsDir)
        {
            // median-curve-solve.py defaults --patch to an old patch name, so always pass one.
            string root = FindRepoRoot();
            string usePatch = string.IsNullOrWhiteSpace(patch) ? ReadActivePatch(Path.Combine(root, "GameData")) : patch!;
            var args = new List<string> { "Scripts/median-curve-solve.py", "--check", "--patch", usePatch };
            AddOpt(args, "--levels", levels);
            return Start("median-check", args, toolsDir, jsonFlag: "--json");
        }

        // ---------- job control ----------

        public static Task<string> GetBalanceJob(string jobId, int tailLines) => McpToolExecutor.ExecuteAsync(() =>
        {
            if (!Jobs.TryGetValue(jobId, out var job))
                return new { error = $"No balance job '{jobId}'. Known: {string.Join(", ", Jobs.Keys)}" };

            string output;
            lock (job.Output) output = job.Output.ToString();
            var lines = output.Replace("\r", "").Split('\n');
            int take = Math.Max(1, tailLines);
            string tail = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - take)));

            object? result = null;
            if (!job.Running && job.JsonPath != null && File.Exists(job.JsonPath))
            {
                var info = new FileInfo(job.JsonPath);
                result = info.Length <= 60_000
                    ? JsonDocument.Parse(File.ReadAllText(job.JsonPath)).RootElement.Clone()
                    : (object)$"{info.Length / 1024} KB; read it from jsonPath";
            }

            return new
            {
                jobId = job.Id,
                kind = job.Kind,
                status = job.Cancelled ? "cancelled" : job.Running ? "running" : job.ExitCode == 0 ? "done" : "failed",
                exitCode = job.ExitCode,
                elapsedSeconds = Math.Round(((job.EndedUtc ?? DateTime.UtcNow) - job.StartedUtc).TotalSeconds),
                command = job.CommandLine,
                jsonPath = job.JsonPath,
                outputTail = tail,
                result
            };
        }, writeIndented: true);

        public static Task<string> ListBalanceJobs() => McpToolExecutor.ExecuteAsync(() => new
        {
            jobs = Jobs.Values.OrderBy(j => j.StartedUtc).Select(j => new
            {
                jobId = j.Id,
                kind = j.Kind,
                status = j.Cancelled ? "cancelled" : j.Running ? "running" : j.ExitCode == 0 ? "done" : "failed",
                startedUtc = j.StartedUtc,
                elapsedSeconds = Math.Round(((j.EndedUtc ?? DateTime.UtcNow) - j.StartedUtc).TotalSeconds),
                jsonPath = j.JsonPath
            }).ToList()
        });

        public static Task<string> CancelBalanceJob(string jobId) => McpToolExecutor.ExecuteAsync(() =>
        {
            if (!Jobs.TryGetValue(jobId, out var job))
                return new { success = false, message = $"No balance job '{jobId}'" };
            if (!job.Running)
                return new { success = false, message = "Job already finished" };
            job.Cancelled = true;
            try { job.Process?.Kill(entireProcessTree: true); } catch (Exception ex) { return new { success = false, message = ex.Message }; }
            return new { success = true, message = $"Cancelled {jobId}" };
        });

        // ---------- internals ----------

        private static Task<string> Start(string kind, List<string> scriptArgs, string? toolsDir, string jsonFlag) =>
            McpToolExecutor.ExecuteAsync(() =>
            {
                var running = Jobs.Values.FirstOrDefault(j => j.Running);
                if (running != null)
                    return new
                    {
                        started = false,
                        message = $"Balance job {running.Id} ({running.Kind}) is still running and uses every core. Wait for it (get_balance_job) or cancel_balance_job first."
                    };

                string root = FindRepoRoot();
                string tools = string.IsNullOrWhiteSpace(toolsDir) ? DefaultToolsDir() : Path.GetFullPath(toolsDir!);
                if (!File.Exists(Path.Combine(tools, "DF.dll")))
                    return new { started = false, message = $"No DF.dll in {tools}. Pass toolsDir (e.g. Code/bin/BalanceTools after a build)." };

                string id = $"{kind}-{System.Threading.Interlocked.Increment(ref _jobCounter)}";
                string runsDir = Path.Combine(root, "GameData", "TuningRuns");
                Directory.CreateDirectory(runsDir);
                string jsonPath = Path.Combine(runsDir, $"mcp-{id}-{DateTime.Now:yyyyMMdd-HHmmss}.json");

                var args = new List<string>(scriptArgs) { "--tools", tools, jsonFlag, jsonPath };
                string python = System.Environment.GetEnvironmentVariable("DF_PYTHON") is { Length: > 0 } p ? p : "python";

                var psi = new ProcessStartInfo(python)
                {
                    WorkingDirectory = root,
                    // Never let sims inherit the server's stdin: under stdio transport it is the MCP channel.
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                psi.ArgumentList.Add("-u");
                foreach (var a in args) psi.ArgumentList.Add(a);
                psi.Environment["PYTHONIOENCODING"] = "utf-8";
                psi.Environment["PYTHONUNBUFFERED"] = "1";

                var job = new Job
                {
                    Id = id,
                    Kind = kind,
                    CommandLine = python + " -u " + string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                    JsonPath = jsonPath,
                    StartedUtc = DateTime.UtcNow
                };

                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (_, e) => Append(job, e.Data);
                proc.ErrorDataReceived += (_, e) => Append(job, e.Data);
                proc.Exited += (_, _) =>
                {
                    // WaitForExit() with no timeout also drains the async stdout/stderr readers.
                    try { proc.WaitForExit(); job.ExitCode = proc.ExitCode; } catch { job.ExitCode = -1; }
                    job.EndedUtc = DateTime.UtcNow;
                };

                try { proc.Start(); }
                catch (Exception ex)
                {
                    return new { started = false, message = $"Could not start '{python}': {ex.Message}. Set DF_PYTHON to the Python executable." };
                }
                try { proc.StandardInput.Close(); } catch { /* already gone */ }
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                job.Process = proc;
                Jobs[id] = job;

                return new
                {
                    started = true,
                    jobId = id,
                    command = job.CommandLine,
                    jsonPath,
                    message = "Running in the background. Poll get_balance_job every minute or two; scans take ~8 min, solves 10+ min."
                };
            });

        private static void Append(Job job, string? line)
        {
            if (line == null) return;
            lock (job.Output)
            {
                job.Output.AppendLine(line);
                if (job.Output.Length > MaxOutputChars)
                    job.Output.Remove(0, job.Output.Length - MaxOutputChars);
            }
        }

        private static void AddOpt(List<string> args, string flag, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            args.Add(flag);
            args.Add(value!.Trim());
        }

        /// <summary>"10=1.2,15=1.4" → repeated --anchor L=multiplier.</summary>
        private static void AddAnchors(List<string> args, string? anchors)
        {
            if (string.IsNullOrWhiteSpace(anchors)) return;
            foreach (var a in anchors!.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                args.Add("--anchor");
                args.Add(a.Trim());
            }
        }

        private static string DefaultToolsDir() => Path.GetFullPath(AppContext.BaseDirectory);

        private static string FindRepoRoot()
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "Scripts", "dungeon-sim.py")) &&
                        Directory.Exists(Path.Combine(dir.FullName, "GameData")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            throw new InvalidOperationException("Repo root (folder with Scripts/dungeon-sim.py and GameData) not found above the server or working directory.");
        }

        private static string ReadActivePatch(string gameData)
        {
            string? name = ReadJsonString(Path.Combine(gameData, "PatchProfile.json"), "activeBalancePatch");
            return string.IsNullOrWhiteSpace(name) ? "default" : name!;
        }

        private static string? ReadJsonString(string path, string prop)
        {
            if (!File.Exists(path)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static double? ReadJsonDouble(string path, string prop)
        {
            if (!File.Exists(path)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return FindNumber(doc.RootElement, prop);
        }

        private static double? FindNumber(JsonElement el, string prop)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(prop) && p.Value.ValueKind == JsonValueKind.Number) return p.Value.GetDouble();
                if (p.Value.ValueKind == JsonValueKind.Object && FindNumber(p.Value, prop) is double d) return d;
            }
            return null;
        }
    }
}
