using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Data;
using RPGGame.Entity.Services;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// SKILLVALUE: prices every skill-tree node. A typical-gear hero of the tree's weapon at <c>--level</c> gets one
    /// node at rank 1 and at max rank (nothing else learned) and fights the eval enemies; the result is compared with
    /// the same hero and strip without the node. Value 100 = no change; offense = fewer hero turns, defense = less
    /// net HP lost per hero turn. Each node is tried on a one-action strip (empty slots count for slot skills) and,
    /// for nodes that unlock an action, on strips that include it; the best strip is reported.
    /// </summary>
    /// <example>DF.exe SKILLVALUE --level 20 --encounters 60 --classes Barbarian,Wizard</example>
    public static class SkillValueRunner
    {
        public sealed class SkillValueRow
        {
            public string Class { get; init; } = "";
            public string Node { get; init; } = "";
            public string Name { get; init; } = "";
            public int Rank { get; init; }
            public int Points { get; init; }
            public double Power { get; init; }
            public string Strip { get; init; } = "";
            public double OffenseRatio { get; init; }
            public double DefenseRatio { get; init; }
            /// <summary>100 = same as without the node.</summary>
            public double ValueIndex { get; init; }
            /// <summary>Value per skill point spent: (ValueIndex / 100)^(1 / Points).</summary>
            public double ValuePerPoint { get; init; }
        }

        private readonly record struct Outcome(double HeroTurns, double HpLostPerHeroTurn, double WinRate);

        public static async Task<int> RunAsync(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 20;
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 60;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            int extraSlots = TuningCliArgs.GetIntFlag(args, "--slots") ?? 2;
            var enemies = (TuningCliArgs.GetFlagValue(args, "--enemies") ?? "Wolf,Goblin")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var classFilter = TuningCliArgs.GetFlagValue(args, "--classes")?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var nodeFilter = TuningCliArgs.GetFlagValue(args, "--nodes")?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ActionLoader.ReloadActions();
            var trees = GameConfiguration.Instance.SkillTrees.Trees
                .Where(t => classFilter == null || classFilter.Contains(t.ClassKey))
                .ToList();

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== SKILLVALUE L{level} vs {string.Join(",", enemies)} | {encounters} fights/enemy | +{extraSlots} strip slots | seed {seed} ===");

            var rows = new List<SkillValueRow>();
            foreach (var tree in trees)
            {
                if (!SequenceLabRunner.WeaponBasics.TryGetValue(tree.Weapon, out string? basic))
                    continue;
                var snapshot = FundamentalsCombatSetup.BuildSnapshot(new SimulationProfileConfig
                {
                    Mode = TuningSimulationModes.FundamentalsEncounter,
                    PlayerLevel = level,
                    EnemyLevel = level,
                    WeaponType = tree.Weapon,
                    EnemyType = enemies[0],
                    Gear = "typical"
                });
                string baseJson = snapshot.InitialPlayerJson;
                var baselines = new Dictionary<string, Outcome>();

                Console.WriteLine();
                Console.WriteLine($"[{tree.ClassKey} / {tree.Weapon}]");
                Console.WriteLine($"{"Node",-16}{"Rank",5}{"Power",7}{"Value",8}{"/pt",7}{"Off",7}{"Def",7}  Strip");

                async Task<SkillValueRow?> PriceAsync(string id, string name, Dictionary<string, int> learned, int points, double power, List<List<string>> strips)
                {
                    SkillValueRow? best = null;
                    foreach (var strip in strips)
                    {
                        string key = string.Join(">", strip);
                        if (!baselines.TryGetValue(key, out var baseline))
                        {
                            baseline = await EvaluateAsync(baseJson, new Dictionary<string, int>(), strip, level, enemies, encounters, seed, extraSlots).ConfigureAwait(false);
                            baselines[key] = baseline;
                        }
                        var r = await EvaluateAsync(baseJson, learned, strip, level, enemies, encounters, seed, extraSlots).ConfigureAwait(false);
                        if (r.HeroTurns <= 0 || baseline.HeroTurns <= 0)
                            continue;
                        double offense = baseline.HeroTurns / r.HeroTurns;
                        double defense = r.HpLostPerHeroTurn > 0 ? baseline.HpLostPerHeroTurn / r.HpLostPerHeroTurn : 2.0;
                        double value = offense * defense * 100;
                        var row = new SkillValueRow
                        {
                            Class = tree.ClassKey,
                            Node = id,
                            Name = name,
                            Rank = learned.Count == 1 ? learned.Values.First() : 0,
                            Points = points,
                            Power = power,
                            Strip = key,
                            OffenseRatio = offense,
                            DefenseRatio = defense,
                            ValueIndex = value,
                            ValuePerPoint = Math.Pow(Math.Max(0.01, value / 100.0), 1.0 / Math.Max(1, points))
                        };
                        if (best == null || row.ValueIndex > best.ValueIndex)
                            best = row;
                    }
                    if (best != null)
                    {
                        rows.Add(best);
                        Console.WriteLine($"{best.Node,-16}{best.Rank,5}{best.Power,7:F2}{best.ValueIndex,8:F0}{(best.ValuePerPoint - 1) * 100,6:F1}%{best.OffenseRatio,7:F2}{best.DefenseRatio,7:F2}  {best.Strip}");
                    }
                    return best;
                }

                var nodes = tree.Nodes.Where(n => nodeFilter == null || nodeFilter.Contains(n.Id)).ToList();
                var unlocked = new List<string>();
                foreach (var node in nodes)
                {
                    var strips = new List<List<string>> { new() { basic } };
                    string? action = UnlockedAction(node);
                    if (action != null)
                    {
                        unlocked.Add(action);
                        strips.Add(new List<string> { basic, action });
                        strips.Add(new List<string> { action, basic });
                    }

                    var ranks = node.MaxRank > 1 ? new[] { 1, node.MaxRank } : new[] { 1 };
                    foreach (int rank in ranks)
                    {
                        var learned = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [node.Id] = rank };
                        await PriceAsync(node.Id, node.Name, learned, Math.Max(1, node.Cost) * rank, node.Power, strips).ConfigureAwait(false);
                    }
                }

                // Whole tree at max rank: one-action strip, and the basic plus every unlocked action.
                if (nodeFilter == null)
                {
                    var all = nodes.ToDictionary(n => n.Id, n => Math.Max(1, n.MaxRank), StringComparer.OrdinalIgnoreCase);
                    int allPoints = nodes.Sum(n => Math.Max(1, n.Cost) * Math.Max(1, n.MaxRank));
                    var allStrips = new List<List<string>> { new() { basic } };
                    if (unlocked.Count > 0)
                        allStrips.Add(new List<string> { basic }.Concat(unlocked.Take(Math.Max(0, 1 + extraSlots))).ToList());
                    await PriceAsync("ALL", "Whole tree", all, allPoints, 1.0, allStrips).ConfigureAwait(false);
                }
            }

            string outPath = TuningCliArgs.GetFlagValue(args, "--json")
                ?? GameConstants.ResolveGameDataPath("TuningRuns", $"skillvalue-L{level}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Rows: {Path.GetFullPath(outPath)}");
            return 0;
        }

        /// <summary>Action granted by the node: <c>unlockActionName</c>, else "ACTION: NAME" in its effect text.</summary>
        private static string? UnlockedAction(SkillTreeNodeDefinition node)
        {
            if (!string.IsNullOrWhiteSpace(node.UnlockActionName))
                return node.UnlockActionName;
            var m = System.Text.RegularExpressions.Regex.Match(node.Effect ?? "", @"ACTION:\s*([A-Z][A-Z ]*[A-Z])");
            if (!m.Success)
                return null;
            string name = m.Groups[1].Value.Trim();
            return ActionLoader.GetActionData(name) != null ? name : null;
        }

        private static async Task<Outcome> EvaluateAsync(
            string baseJson, IReadOnlyDictionary<string, int> learned, IReadOnlyList<string> strip,
            int level, IReadOnlyList<string> enemies, int encounters, int seed, int extraSlots)
        {
            var hero = LabCombatEntityFactory.ClonePlayerFromJson(baseJson, "SKILLVALUE: hero clone failed.");
            foreach (var kv in learned)
                hero.Progression.LearnedSkillRanks[kv.Key] = kv.Value;
            string json = new CharacterSerializer().Serialize(hero);

            double turns = 0, hpLost = 0, wins = 0;
            int fights = 0;
            ActionLabEncounterSimulator.UseNaturalComboRouting = true;
            try
            {
                foreach (string enemy in enemies)
                {
                    var snapshot = new LabCombatSnapshot(
                        json, 0, 0, 0, 0, 0, 0, extraSlots,
                        sessionEnemyLoaderType: enemy,
                        enemyLevel: level,
                        comboStripActionNames: strip,
                        selectedCatalogActionName: strip[0],
                        labEnemyBattleConfig: null);
                    if (ActionLabEncounterSimulator.ValidateSnapshot(snapshot) != null)
                        continue;
                    var report = await ActionLabEncounterSimulator.RunBatchAsync(snapshot, encounters, new Random(seed), maxDegreeOfParallelism: 1)
                        .ConfigureAwait(false);
                    foreach (var e in report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)))
                    {
                        fights++;
                        wins += e.PlayerWon ? 1 : 0;
                        turns += Math.Max(1, e.PlayerTurns);
                        hpLost += Math.Max(0, e.PlayerMaxHealth - e.PlayerFinalHealth);
                    }
                }
            }
            finally
            {
                ActionLabEncounterSimulator.UseNaturalComboRouting = false;
            }

            return fights == 0 ? default : new Outcome(turns / fights, turns > 0 ? hpLost / turns : 0, wins / fights);
        }
    }
}
