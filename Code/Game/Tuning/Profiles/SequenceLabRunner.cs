using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Action-sequence testing on the fundamentals sim with real combo routing (strip slots, position AMP, resets on miss).
    /// SEQLAB compares explicit sequences; ACTIONVALUE prices every hero action against the weapon-basic-only baseline.
    /// </summary>
    /// <example>
    /// DF.exe SEQLAB --weapon Sword --seq STRIKE --seq "STRIKE,SLASH" --seq "SLASH,STRIKE"
    /// DF.exe ACTIONVALUE --weapon Sword --encounters 150 --seed 1 --shard 0/4
    /// </example>
    public static class SequenceLabRunner
    {
        public sealed class SequenceResult
        {
            public string Weapon { get; init; } = "";
            public string Enemy { get; init; } = "";
            public int Level { get; init; }
            public List<string> Sequence { get; init; } = new();
            public double HeroTurns { get; init; }
            public double EnemyTurns { get; init; }
            public double HeroDamagePerTurn { get; init; }
            /// <summary>Damage the enemy dealt per hero turn — lower means the sequence also defends (block stance, debuffs, speed).</summary>
            public double EnemyDamagePerHeroTurn { get; init; }
            public double WinRate { get; init; }
            public double ComboEventsPerFight { get; init; }
            public double MaxComboChain { get; init; }
            public string? Error { get; init; }
        }

        public sealed class ActionValueRow
        {
            public string Action { get; init; } = "";
            public string Weapon { get; init; } = "";
            public string Position { get; init; } = "";
            public int Tier { get; init; }
            public string Rarity { get; init; } = "";
            public string Tags { get; init; } = "";
            public double OffenseRatio { get; init; }
            public double DefenseRatio { get; init; }
            /// <summary>100 = same as the weapon-basic baseline. Offense ratio × defense ratio.</summary>
            public double ValueIndex { get; init; }
            public double HeroTurns { get; init; }
            public double WinRate { get; init; }
            public string? Error { get; init; }
        }

        /// <summary>Hero gear profile for sequence runs (<c>--gear none|typical</c>).</summary>
        public static string Gear { get; set; } = "none";

        public static readonly Dictionary<string, string> WeaponBasics = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Mace"] = "SLAM", ["Sword"] = "STRIKE", ["Dagger"] = "STAB", ["Wand"] = "MAGIC MISSLE"
        };

        public static async Task<SequenceResult> RunSequenceAsync(
            string weapon, string enemy, int level, IReadOnlyList<string> sequence, int encounters, int seed)
        {
            var config = new SimulationProfileConfig
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                PlayerLevel = level,
                EnemyLevel = level,
                WeaponType = weapon,
                EnemyType = enemy,
                Gear = Gear
            };

            try
            {
                var snapshot = FundamentalsCombatSetup.BuildSnapshot(config, sequence);
                ActionLabEncounterSimulator.UseNaturalComboRouting = true;
                ActionLabEncounterSimulationReport report;
                try
                {
                    report = await ActionLabEncounterSimulator.RunBatchAsync(snapshot, encounters, new Random(seed), maxDegreeOfParallelism: 1)
                        .ConfigureAwait(false);
                }
                finally
                {
                    ActionLabEncounterSimulator.UseNaturalComboRouting = false;
                }

                var r = FundamentalsSimulationResult.FromReport(report, snapshot, config);
                var ok = report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)).ToList();
                int heroTurns = ok.Sum(e => e.PlayerTurns);
                return new SequenceResult
                {
                    Weapon = weapon,
                    Enemy = enemy,
                    Level = level,
                    Sequence = sequence.ToList(),
                    HeroTurns = r.AveragePlayerTurnsPerEncounter,
                    EnemyTurns = r.AverageEnemyTurnsPerEncounter,
                    HeroDamagePerTurn = r.AveragePlayerDamagePerTurn,
                    // Net HP lost (damage taken minus healing) per hero turn, so heals and lifesteal count as defense.
                    EnemyDamagePerHeroTurn = heroTurns > 0 ? ok.Sum(e => Math.Max(0, e.PlayerMaxHealth - e.PlayerFinalHealth)) / (double)heroTurns : 0,
                    WinRate = r.WinRate,
                    ComboEventsPerFight = r.AverageComboPlusEventsPerEncounter,
                    MaxComboChain = r.AverageMaxComboStreak,
                    Error = r.ErroredEncounters > 0 ? ok.Count == 0 ? report.Encounters.FirstOrDefault()?.ErrorMessage : $"{r.ErroredEncounters} errored fights" : null
                };
            }
            catch (Exception ex)
            {
                return new SequenceResult { Weapon = weapon, Enemy = enemy, Level = level, Sequence = sequence.ToList(), Error = ex.Message };
            }
        }

        public static async Task<int> RunSeqLabAsync(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            Gear = TuningCliArgs.GetFlagValue(args, "--gear") ?? "none";
            string weapon = TuningCliArgs.GetFlagValue(args, "--weapon", "-w") ?? "Sword";
            string enemy = TuningCliArgs.GetFlagValue(args, "--enemy") ?? "Goblin";
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 1;
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 200;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;

            var sequences = new List<List<string>>();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals("--seq", StringComparison.OrdinalIgnoreCase))
                    sequences.Add(args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
            if (sequences.Count == 0)
                sequences.Add(new List<string> { WeaponBasics[weapon] });

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== SEQLAB L{level} {weapon} vs {enemy} | {encounters} fights | seed {seed} ===");
            Console.WriteLine($"{"Sequence",-44}{"Turns",7}{"Dmg/t",8}{"EDmg/t",8}{"Combo",7}{"Chain",7}{"WR",6}");
            foreach (var seq in sequences)
            {
                var r = await RunSequenceAsync(weapon, enemy, level, seq, encounters, seed).ConfigureAwait(false);
                string name = string.Join(" > ", seq);
                Console.WriteLine(r.Error != null && r.HeroTurns == 0
                    ? $"{name,-44} ERROR {r.Error}"
                    : $"{name,-44}{r.HeroTurns,7:F2}{r.HeroDamagePerTurn,8:F2}{r.EnemyDamagePerHeroTurn,8:F2}{r.ComboEventsPerFight,7:F2}{r.MaxComboChain,7:F2}{r.WinRate * 100,5:F0}%"
                      + (r.Error != null ? $"  ({r.Error})" : ""));
            }

            return 0;
        }

        /// <summary>Hero-usable actions: not enemy/environment rows.</summary>
        public static List<Action> HeroActions() =>
            ActionLoader.GetAllActions()
                .Where(a => !a.Tags.Any(t => t.Equals("enemy", StringComparison.OrdinalIgnoreCase)
                                          || t.Equals("environment", StringComparison.OrdinalIgnoreCase)))
                .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(a => a.Name)
                .ToList();

        public static async Task<int> RunActionValueAsync(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            Gear = TuningCliArgs.GetFlagValue(args, "--gear") ?? "none";
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 1;
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 150;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            // Wolf only has plain attacks; Goblin's JAB resets the hero's chain and hides sequence value.
            string enemy = TuningCliArgs.GetFlagValue(args, "--enemy") ?? "Wolf";
            var weapons = (TuningCliArgs.GetFlagValue(args, "--weapons") ?? TuningCliArgs.GetFlagValue(args, "--weapon") ?? "Mace,Sword,Dagger,Wand")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            string? only = TuningCliArgs.GetFlagValue(args, "--actions");
            (int shard, int shards) = ParseShard(TuningCliArgs.GetFlagValue(args, "--shard"));

            ActionLoader.ReloadActions();
            var actions = HeroActions();
            if (!string.IsNullOrWhiteSpace(only))
            {
                var set = new HashSet<string>(only.Split(',', StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);
                actions = actions.Where(a => set.Contains(a.Name)).ToList();
            }
            actions = actions.Where((a, i) => i % shards == shard).ToList();

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== ACTIONVALUE L{level} vs {enemy} | {actions.Count} actions x {weapons.Count} weapons | {encounters} fights | seed {seed} | shard {shard}/{shards} ===");

            var rows = new List<ActionValueRow>();
            foreach (string weapon in weapons)
            {
                string basic = WeaponBasics[weapon];
                var baseline = await RunSequenceAsync(weapon, enemy, level, new[] { basic }, encounters, seed).ConfigureAwait(false);
                Console.WriteLine($"[{weapon}] baseline {basic}: turns {baseline.HeroTurns:F2} dmg/t {baseline.HeroDamagePerTurn:F2} enemy dmg/hero turn {baseline.EnemyDamagePerHeroTurn:F2}");

                foreach (var action in actions)
                {
                    if (action.Name.Equals(basic, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var data = ActionLoader.GetActionData(action.Name);
                    foreach (string position in new[] { "after", "before" })
                    {
                        var seq = position == "after" ? new[] { basic, action.Name } : new[] { action.Name, basic };
                        var r = await RunSequenceAsync(weapon, enemy, level, seq, encounters, seed).ConfigureAwait(false);
                        double offense = r.HeroTurns > 0 ? baseline.HeroTurns / r.HeroTurns : 0;
                        double defense = r.EnemyDamagePerHeroTurn > 0 ? baseline.EnemyDamagePerHeroTurn / r.EnemyDamagePerHeroTurn : 2.0;
                        rows.Add(new ActionValueRow
                        {
                            Action = action.Name,
                            Weapon = weapon,
                            Position = position,
                            Tier = data?.Tier ?? 0,
                            Rarity = data?.Rarity ?? "",
                            Tags = string.Join(",", action.Tags),
                            OffenseRatio = offense,
                            DefenseRatio = defense,
                            ValueIndex = offense * defense * 100,
                            HeroTurns = r.HeroTurns,
                            WinRate = r.WinRate,
                            Error = r.Error
                        });
                    }
                }
            }

            // Best position per action per weapon, then averaged across weapons.
            var summary = rows.Where(r => r.Error == null || r.HeroTurns > 0)
                .GroupBy(r => r.Action)
                .Select(g => new
                {
                    Action = g.Key,
                    Value = g.GroupBy(r => r.Weapon).Average(w => w.Max(r => r.ValueIndex)),
                    Offense = g.GroupBy(r => r.Weapon).Average(w => w.Max(r => r.OffenseRatio)),
                    Defense = g.GroupBy(r => r.Weapon).Average(w => w.Max(r => r.DefenseRatio)),
                    g.First().Tier,
                    g.First().Rarity
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            Console.WriteLine();
            Console.WriteLine($"{"Action",-22}{"Tier",5}{"Rarity",9}{"Value",8}{"Off",7}{"Def",7}");
            foreach (var s in summary)
                Console.WriteLine($"{s.Action,-22}{s.Tier,5}{s.Rarity,9}{s.Value,8:F0}{s.Offense,7:F2}{s.Defense,7:F2}");
            foreach (var bad in rows.Where(r => r.Error != null && r.HeroTurns == 0).GroupBy(r => r.Action))
                Console.WriteLine($"  ERROR {bad.Key}: {bad.First().Error}");

            string outPath = TuningCliArgs.GetFlagValue(args, "--json")
                ?? GameConstants.ResolveGameDataPath("TuningRuns", $"actionvalue-L{level}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-s{shard}of{shards}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Rows: {Path.GetFullPath(outPath)}");
            return 0;
        }

        private static (int shard, int shards) ParseShard(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (0, 1);
            var parts = value.Split('/');
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), Math.Max(1, int.Parse(parts[1], CultureInfo.InvariantCulture)));
        }
    }
}
