using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// FUNDMATRIX: seeded no-gear fundamentals across every weapon × enemy at one level.
    /// Reports hero turns, damage per turn and enemy HP per cell, and writes a JSON run record
    /// so tuning changes can be compared against a baseline on identical dice.
    /// </summary>
    /// <example>
    /// DF.exe FUNDMATRIX --level 1 --encounters 200 --seed 1 --target-turns 11
    /// DF.exe FUNDMATRIX --set enemySystem.baselineStats.health=110 --label hp110
    /// DF.exe FUNDMATRIX --patch l1-core --label l1-core
    /// </example>
    public static class FundamentalsMatrixRunner
    {
        public sealed class Cell
        {
            public string Weapon { get; init; } = "";
            public string Enemy { get; init; } = "";
            public double HeroTurns { get; init; }
            public double MedianHeroTurns { get; init; }
            public double EnemyTurns { get; init; }
            public double CombinedActions { get; init; }
            public double HeroHp { get; init; }
            public double EnemyHp { get; init; }
            public double HeroDamagePerTurn { get; init; }
            public double HeroDamagePerHit { get; init; }
            public double EnemyDamagePerTurn { get; init; }
            public double HeroHpLostFraction { get; init; }
            public double MissRate { get; init; }
            public double WinRate { get; init; }
            public double TurnStdDev { get; init; }
        }

        public sealed class RunRecord
        {
            public string Label { get; init; } = "";
            public string BalancePatch { get; init; } = "";
            public DateTime TimestampUtc { get; init; }
            public int Level { get; init; }
            public int Encounters { get; init; }
            public int Seed { get; init; }
            public double TargetHeroTurns { get; init; }
            public Dictionary<string, string> Overrides { get; init; } = new();
            public double MeanHeroTurns { get; init; }
            public double MeanHeroDamagePerTurn { get; init; }
            public double MeanEnemyHp { get; init; }
            public double MeanCombinedActions { get; init; }
            public double MeanWinRate { get; init; }
            public double MeanHeroHpLostFraction { get; init; }
            public List<Cell> Cells { get; init; } = new();
        }

        public static readonly string[] AllWeapons = { "Mace", "Sword", "Dagger", "Wand" };

        public static async Task<int> RunAsync(string[] args)
        {
            ApplyPatchFlag(args);
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 1;
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 200;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            double target = ParseDouble(TuningCliArgs.GetFlagValue(args, "--target-turns")) ?? 11.0;
            string label = TuningCliArgs.GetFlagValue(args, "--label") ?? "run";
            string gear = TuningCliArgs.GetFlagValue(args, "--gear") ?? "none";
            var weapons = SplitCsv(TuningCliArgs.GetFlagValue(args, "--weapons")) ?? AllWeapons.ToList();
            var enemies = SplitCsv(TuningCliArgs.GetFlagValue(args, "--enemies")) ?? LoadDungeonEnemies();

            var overrides = ApplySetFlags(args);

            Console.WriteLine($"Balance patch: {ActiveBalancePatchPath()}");
            Console.WriteLine($"=== FUNDMATRIX L{level} | {weapons.Count} weapons x {enemies.Count} enemies | {encounters} fights/cell | seed {seed} | target {target} hero turns ===");

            var cells = new List<Cell>();
            foreach (string enemy in enemies)
            {
                foreach (string weapon in weapons)
                {
                    var profile = new BalanceTuningProfile
                    {
                        Id = "fundmatrix",
                        Simulation = new SimulationProfileConfig
                        {
                            Mode = TuningSimulationModes.FundamentalsEncounter,
                            EncounterCount = encounters,
                            Levels = new List<int> { level },
                            PlayerLevel = level,
                            EnemyLevel = level,
                            WeaponType = weapon,
                            EnemyType = enemy,
                            Gear = gear
                        }
                    };

                    var r = await FundamentalsSimulationRunner.RunAsync(profile, new SimulationRunOverrides { Seed = seed })
                        .ConfigureAwait(false);
                    cells.Add(new Cell
                    {
                        Weapon = weapon,
                        Enemy = enemy,
                        HeroTurns = r.AveragePlayerTurnsPerEncounter,
                        MedianHeroTurns = r.MedianPlayerTurnsPerEncounter,
                        EnemyTurns = r.AverageEnemyTurnsPerEncounter,
                        CombinedActions = r.AverageActionsPerEncounter,
                        HeroHp = r.AveragePlayerMaxHealth,
                        EnemyHp = r.AverageEnemyMaxHealth,
                        HeroDamagePerTurn = r.AveragePlayerDamagePerTurn,
                        HeroDamagePerHit = r.AveragePlayerDamagePerHit,
                        EnemyDamagePerTurn = r.AverageEnemyDamagePerTurn,
                        HeroHpLostFraction = r.AverageHeroHpLostFraction,
                        MissRate = r.AverageMissRate,
                        WinRate = r.WinRate,
                        TurnStdDev = r.TurnDurationStdDev
                    });
                }
            }

            var record = new RunRecord
            {
                Label = label,
                BalancePatch = ActiveBalancePatchPath(),
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Encounters = encounters,
                Seed = seed,
                TargetHeroTurns = target,
                Overrides = overrides,
                MeanHeroTurns = cells.Average(c => c.HeroTurns),
                MeanHeroDamagePerTurn = cells.Average(c => c.HeroDamagePerTurn),
                MeanEnemyHp = cells.Average(c => c.EnemyHp),
                MeanCombinedActions = cells.Average(c => c.CombinedActions),
                MeanWinRate = cells.Average(c => c.WinRate),
                MeanHeroHpLostFraction = cells.Average(c => c.HeroHpLostFraction),
                Cells = cells
            };

            Console.WriteLine(FormatReport(record, weapons, enemies));

            string outPath = TuningCliArgs.GetFlagValue(args, "--json") ?? DefaultOutputPath(record);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Run record: {Path.GetFullPath(outPath)}");
            if (RPGGame.Diagnostics.ToolProfiler.Enabled)
                Console.WriteLine("PROFILE:" + System.Environment.NewLine + RPGGame.Diagnostics.ToolProfiler.Report());
            return 0;
        }

        public static string FormatReport(RunRecord rec, IReadOnlyList<string> weapons, IReadOnlyList<string> enemies)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Mean hero turns per fight (enemy HP in brackets)");
            sb.Append($"{"Enemy",-16}{"HP",6}");
            foreach (string w in weapons)
                sb.Append($"{w,8}");
            sb.AppendLine($"{"EnemyDmg",10}{"HeroHP%",9}{"WR",6}");
            foreach (string e in enemies)
            {
                var row = rec.Cells.Where(c => c.Enemy == e).ToList();
                if (row.Count == 0)
                    continue;
                sb.Append($"{e,-16}{row.Average(c => c.EnemyHp),6:F0}");
                foreach (string w in weapons)
                {
                    var c = row.FirstOrDefault(x => x.Weapon == w);
                    sb.Append(c == null ? $"{"-",8}" : $"{c.HeroTurns,8:F1}");
                }
                sb.AppendLine($"{row.Average(c => c.EnemyDamagePerTurn),10:F1}{row.Average(c => c.HeroHpLostFraction) * 100,8:F0}%{row.Average(c => c.WinRate) * 100,5:F0}%");
            }

            sb.AppendLine();
            sb.AppendLine("Per weapon");
            sb.AppendLine($"{"Weapon",-10}{"Turns",8}{"Dmg/turn",10}{"Dmg/hit",9}{"Miss",7}{"HeroHP",8}{"WR",6}");
            foreach (string w in weapons)
            {
                var col = rec.Cells.Where(c => c.Weapon == w).ToList();
                if (col.Count == 0)
                    continue;
                sb.AppendLine($"{w,-10}{col.Average(c => c.HeroTurns),8:F2}{col.Average(c => c.HeroDamagePerTurn),10:F2}{col.Average(c => c.HeroDamagePerHit),9:F2}{col.Average(c => c.MissRate) * 100,6:F1}%{col.Average(c => c.HeroHp),8:F0}{col.Average(c => c.WinRate) * 100,5:F0}%");
            }

            double hpScale = rec.MeanHeroTurns > 0 ? rec.TargetHeroTurns / rec.MeanHeroTurns : 1;
            sb.AppendLine();
            sb.AppendLine($"Overall: hero turns {rec.MeanHeroTurns:F2} (target {rec.TargetHeroTurns}) | combined actions {rec.MeanCombinedActions:F1} | hero dmg/turn {rec.MeanHeroDamagePerTurn:F2} | enemy HP {rec.MeanEnemyHp:F1} | WR {rec.MeanWinRate * 100:F1}% | hero HP lost {rec.MeanHeroHpLostFraction * 100:F0}%");
            sb.AppendLine($"Enemy HP needed for target at current damage: ~{rec.MeanEnemyHp * hpScale:F0} (x{hpScale:F2})");
            return sb.ToString();
        }

        /// <summary>
        /// FIGHTLOG: one seeded no-gear fight printed event by event (rolls, damage, HP), for checking what the matrix numbers mean.
        /// </summary>
        /// <example>DF.exe FIGHTLOG --weapon Dagger --enemy "Giant Ants" --seed 3 --fights 2</example>
        public static async Task<int> RunFightLogAsync(string[] args)
        {
            ApplyPatchFlag(args);
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 1;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            int fights = TuningCliArgs.GetIntFlag(args, "--fights") ?? 1;
            ApplySetFlags(args);

            var config = new SimulationProfileConfig
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                PlayerLevel = level,
                EnemyLevel = level,
                WeaponType = TuningCliArgs.GetFlagValue(args, "--weapon", "-w") ?? "Sword",
                EnemyType = TuningCliArgs.GetFlagValue(args, "--enemy"),
                Gear = TuningCliArgs.GetFlagValue(args, "--gear") ?? "none"
            };
            Console.WriteLine($"Balance patch: {ActiveBalancePatchPath()}");
            string? strip = TuningCliArgs.GetFlagValue(args, "--strip");
            var snapshot = strip == null
                ? FundamentalsCombatSetup.BuildSnapshot(config)
                : FundamentalsCombatSetup.BuildSnapshot(config, strip.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
            // --strip runs the real combo routing through the given sequence.
            RPGGame.ActionInteractionLab.ActionLabEncounterSimulator.UseNaturalComboRouting = strip != null;
            if (args.Any(a => a.Equals("--dump-hero", StringComparison.OrdinalIgnoreCase)))
                Console.WriteLine(snapshot.InitialPlayerJson);

            RPGGame.ActionInteractionLab.ActionLabEncounterSimulator.CaptureEventLog = true;
            try
            {
                for (int i = 0; i < fights; i++)
                {
                    var rng = new Random(RPGGame.ActionInteractionLab.ActionLabEncounterSimulator.DeriveEncounterRandomSeed(seed, i));
                    var m = await RPGGame.ActionInteractionLab.ActionLabEncounterSimulator.RunSingleEncounterAsync(snapshot, rng)
                        .ConfigureAwait(false);
                    Console.WriteLine($"--- Fight {i + 1}: L{level} {config.WeaponType} vs {snapshot.SessionEnemyLoaderType} | hero HP {m.PlayerMaxHealth} enemy HP {m.EnemyMaxHealth} | swing time hero {m.PlayerAttackTime:F2}s enemy {m.EnemyAttackTime:F2}s ---");
                    foreach (string line in m.EventLog ?? new List<string>())
                        Console.WriteLine("  " + line);
                    Console.WriteLine($"  => {(m.PlayerWon ? "WIN" : "LOSS")} | hero turns {m.PlayerTurns}, enemy turns {m.EnemyTurns} | hero dealt {m.PlayerDamageDealt}, took {m.EnemyDamageDealt}"
                        + (m.ErrorMessage != null ? $" | error: {m.ErrorMessage}" : ""));
                }
            }
            finally
            {
                RPGGame.ActionInteractionLab.ActionLabEncounterSimulator.CaptureEventLog = false;
            }

            return 0;
        }

        /// <summary>Full path of the balance patch the live config was loaded from.</summary>
        public static string ActiveBalancePatchPath()
        {
            _ = GameConfiguration.Instance;
            return Path.GetFullPath(GameConfiguration.LoadedBalancePatchPath);
        }

        /// <summary>
        /// <c>--patch name|path</c>: run against another balance patch (e.g. <c>l1-core</c> in GameData/Patches/Balance) without activating it.
        /// </summary>
        public static void ApplyPatchFlag(string[] args)
        {
            string? patch = TuningCliArgs.GetFlagValue(args, "--patch");
            if (string.IsNullOrWhiteSpace(patch))
                return;
            string path = File.Exists(patch)
                ? patch
                : Path.Combine(RPGGame.Config.PatchProfileService.GetCategoryFolder(RPGGame.Config.PatchCategory.Balance), patch.EndsWith(".json") ? patch : patch + ".json");
            GameConfiguration.UseBalancePatchFile(path);
        }

        /// <summary>Applies every <c>--set path=value</c> flag to the live config and returns what was set.</summary>
        public static Dictionary<string, string> ApplySetFlags(string[] args)
        {
            var overrides = new Dictionary<string, string>();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!args[i].Equals("--set", StringComparison.OrdinalIgnoreCase))
                    continue;
                string assignment = args[i + 1];
                int eq = assignment.IndexOf('=');
                if (eq <= 0)
                    throw new ArgumentException($"--set expects path=value, got '{assignment}'.");
                string path = assignment[..eq].Trim();
                string value = assignment[(eq + 1)..].Trim();
                string before = ApplyConfigOverride(GameConfiguration.Instance, path, value);
                overrides[path] = value;
                Console.WriteLine($"  override {path}: {before} -> {value}");
            }

            return overrides;
        }

        /// <summary>
        /// Sets a dotted config path (case-insensitive, e.g. <c>enemySystem.baselineStats.health</c>) on the live config. Returns the old value.
        /// </summary>
        public static string ApplyConfigOverride(object root, string path, string value)
        {
            object target = root;
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                var prop = target.GetType().GetProperty(parts[i],
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                    ?? throw new ArgumentException($"Config path '{path}': no property '{parts[i]}' on {target.GetType().Name}.");
                if (i == parts.Length - 1)
                {
                    string before = Convert.ToString(prop.GetValue(target), CultureInfo.InvariantCulture) ?? "null";
                    var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(target, Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
                    return before;
                }

                target = prop.GetValue(target)
                    ?? throw new ArgumentException($"Config path '{path}': '{parts[i]}' is null.");
            }

            return "";
        }

        private static List<string> LoadDungeonEnemies()
        {
            string path = GameConstants.ResolveGameDataPath("Dungeons.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var list = new List<string>();
            foreach (var dungeon in doc.RootElement.EnumerateArray())
            {
                if (!dungeon.TryGetProperty("possibleEnemies", out var arr))
                    continue;
                foreach (var e in arr.EnumerateArray())
                {
                    string? name = e.GetString();
                    if (!string.IsNullOrWhiteSpace(name) && !list.Contains(name))
                        list.Add(name);
                }
            }

            return list;
        }

        private static string DefaultOutputPath(RunRecord rec) =>
            GameConstants.ResolveGameDataPath("TuningRuns", $"fundmatrix-L{rec.Level}-{rec.TimestampUtc:yyyyMMdd-HHmmss}-{rec.Label}.json");

        private static List<string>? SplitCsv(string? csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? null
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private static double? ParseDouble(string? s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : null;
    }
}
