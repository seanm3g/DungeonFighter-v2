using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// DUNGEONSIM: real dungeons at a fixed hero level, HP carried between fights.
    /// Each run raises a fresh hero to <c>--level</c> (normal level-up rules plus a bag of drops from the levels below,
    /// see <see cref="ClassPlaythroughRunner.StartAtLevel"/>), lets the bot pick gear / skills / sequence, then plays
    /// <c>--dungeons</c> dungeons through the real game loop (rooms, room search, exit prompt, retreat rule) with XP held at 0.
    /// Reports dungeon clear / retreat / death rates with 95% confidence intervals.
    /// </summary>
    public static class DungeonSimRunner
    {
        public sealed class Outcome
        {
            public string Class { get; set; } = "";
            public int Runs { get; set; }
            public int Dungeons { get; set; }
            public int Cleared { get; set; }
            public int Retreated { get; set; }
            public int Died { get; set; }
            public int Fights { get; set; }
            public double HpLostPerFight { get; set; }
            /// <summary>Mean hero actions per fight: compare with the median catalog build's hero turns at this level.</summary>
            public double HeroActionsPerFight { get; set; }
            /// <summary>Mean enemy actions per fight: enemy attacks the hero takes per kill (class-parity gauge).</summary>
            public double EnemyActionsPerFight { get; set; }
            public int LevelDrift { get; set; }
        }

        public static async Task<int> RunAsync(string[] args)
        {
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? throw new ArgumentException("DUNGEONSIM needs --level L.");
            int runs = Math.Max(1, TuningCliArgs.GetIntFlag(args, "--runs") ?? 6);
            int dungeons = Math.Max(1, TuningCliArgs.GetIntFlag(args, "--dungeons") ?? 8);
            int retreat = TuningCliArgs.GetIntFlag(args, "--retreat-below") ?? 50;
            // --bot synergy (default, the calibration bot: rules + set/skill synergy) | rules (no simulated picks) | smart | basic; --basic is the old spelling of --bot basic.
            string bot = (TuningCliArgs.GetFlagValue(args, "--bot") ?? (TuningCliArgs.HasFlag(args, "--basic") ? "basic" : "synergy")).ToLowerInvariant();
            if (bot is not ("smart" or "rules" or "synergy" or "basic"))
                throw new ArgumentException("DUNGEONSIM --bot must be smart, rules, synergy or basic.");
            string? classes = TuningCliArgs.GetFlagValue(args, "--classes");
            string? jsonPath = TuningCliArgs.GetFlagValue(args, "--json");
            int? seed = TuningCliArgs.GetSeedFlag(args);

            ClassPlaythroughRunner.StartAtLevel = level;
            ClassPlaythroughRunner.StartLootPerLevel = TuningCliArgs.GetIntFlag(args, "--start-loot-per-level") ?? ClassPlaythroughRunner.StartLootPerLevel;
            PlaythroughSmartPolicy.ScreenFightsPerEnemy = TuningCliArgs.GetIntFlag(args, "--screen-fights") ?? PlaythroughSmartPolicy.ScreenFightsPerEnemy;
            if (TuningCliArgs.GetFlagValue(args, "--eval-enemies") is { Length: > 0 } evalEnemies)
                PlaythroughSmartPolicy.EvalEnemies = evalEnemies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ClassPlaythroughRunner.HoldLevel = true;
            ClassPlaythroughRunner.MaxDungeons = dungeons;
            ClassPlaythroughRunner.StopAtLevel = 0;
            ClassPlaythroughRunner.SmartPolicy = bot == "smart";
            ClassPlaythroughRunner.RulesPolicy = bot is "rules" or "synergy";
            PlaythroughRulesPolicy.Synergy = bot == "synergy";
            PlaythroughRulesPolicy.ExploreStrips = TuningCliArgs.HasFlag(args, "--explore-strips");
            ClassPlaythroughRunner.RegionMix = TuningCliArgs.HasFlag(args, "--region-mix");
            // Shards are separate processes whose run counters all start at 0: offset them by the shard seed so the runs
            // spread over every region theme.
            ClassPlaythroughRunner.RegionMixOffset = ClassPlaythroughRunner.RegionMix && seed is int mixSeed ? Math.Abs(mixSeed % 4) : 0;
            // Every sim rotates the dungeon slot (a player picks any of the region's three); --dungeon-slot N pins one.
            int? pinnedSlot = TuningCliArgs.GetIntFlag(args, "--dungeon-slot");
            ClassPlaythroughPolicy.DungeonMix = pinnedSlot == null;
            ClassPlaythroughPolicy.PinnedDungeonSlot = pinnedSlot;
            ClassPlaythroughPolicy.DungeonMixOffset = seed is int dSeed ? Math.Abs(dSeed % 3) : 0;
            ClassPlaythroughRunner.RegionTheme = TuningCliArgs.GetFlagValue(args, "--region-theme");
            ClassPlaythroughRunner.RetreatBelowHpPercent = retreat;
            // --material Bone: every loot roll (bag and drops) is that material, so the run's set is that material.
            LootBonusApplier.ToolForcedMaterial = TuningCliArgs.GetFlagValue(args, "--material") is { Length: > 0 } forced
                ? ItemMaterialRules.RemapLegacyMaterial(forced) : null;
            // --features-off region,dungeon,prefix,room,terrain,wander,demon,kits: switch enemy / room affix features off (pricing each one).
            AffixFeatures.SetOff(TuningCliArgs.GetFlagValue(args, "--features-off"));
            // --affix stat:CRIT / mod:Heavy: every loot roll also carries that affix line (prices one affix vs an unforced run).
            LootBonusApplier.ToolForcedAffix = TuningCliArgs.GetFlagValue(args, "--affix") is { Length: > 0 } affix ? affix : null;

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== DUNGEONSIM L{level} | {runs} run(s) x {dungeons} dungeon(s) per class | {bot} bot | retreat below {retreat}% HP | start loot {ClassPlaythroughRunner.ResolveStartLootPerLevel(level)}/level{(level > 15 ? " (bag calibrated to L10-L15 only)" : "")} | seed {(seed?.ToString() ?? "none")} ===");

            var batch = await ClassPlaythroughBatchRunner.RunAsync(runs, classes, maxActionsPerRun: 60 * dungeons + 300, seed: seed);

            foreach (var agg in batch.ClassAggregates)
                foreach (var run in agg.Runs.Where(r => r.StopReason == PlaythroughStopReason.Error))
                    Console.WriteLine($"ERROR: {agg.ClassDisplayName} run stopped: {run.ErrorMessage}");

            var outcomes = new List<Outcome>();
            foreach (var agg in batch.ClassAggregates)
            {
                var fights = agg.Runs.SelectMany(r => r.Fights).Where(f => f.HeroLevel >= level).ToList();
                outcomes.Add(new Outcome
                {
                    Class = agg.ClassDisplayName,
                    Runs = agg.Runs.Count,
                    Dungeons = agg.Runs.Sum(r => r.DungeonsAttempted),
                    Cleared = agg.Runs.Sum(r => r.DungeonsCompleted),
                    Retreated = agg.Runs.Sum(r => r.DungeonsRetreated),
                    Died = agg.Runs.Count(r => r.ReachedDeath),
                    Fights = fights.Count,
                    HpLostPerFight = fights.Count == 0 ? 0 : fights.Average(f => f.HeroMaxHp > 0 ? Math.Max(0, f.HeroHpStart - f.HeroHpEnd) / (double)f.HeroMaxHp : 0),
                    HeroActionsPerFight = fights.Count == 0 ? 0 : fights.Average(f => f.HeroActions),
                    EnemyActionsPerFight = fights.Count == 0 ? 0 : fights.Average(f => f.EnemyActions),
                    LevelDrift = agg.Runs.Count(r => r.FinalLevel != level)
                });
            }

            var all = new Outcome
            {
                Class = "all",
                Runs = outcomes.Sum(o => o.Runs),
                Dungeons = outcomes.Sum(o => o.Dungeons),
                Cleared = outcomes.Sum(o => o.Cleared),
                Retreated = outcomes.Sum(o => o.Retreated),
                Died = outcomes.Sum(o => o.Died),
                Fights = outcomes.Sum(o => o.Fights),
                HpLostPerFight = outcomes.Sum(o => o.Fights) == 0 ? 0 : outcomes.Sum(o => o.HpLostPerFight * o.Fights) / outcomes.Sum(o => o.Fights),
                HeroActionsPerFight = outcomes.Sum(o => o.Fights) == 0 ? 0 : outcomes.Sum(o => o.HeroActionsPerFight * o.Fights) / outcomes.Sum(o => o.Fights),
                EnemyActionsPerFight = outcomes.Sum(o => o.Fights) == 0 ? 0 : outcomes.Sum(o => o.EnemyActionsPerFight * o.Fights) / outcomes.Sum(o => o.Fights),
                LevelDrift = outcomes.Sum(o => o.LevelDrift)
            };

            Console.WriteLine(FormatReport(level, outcomes.Append(all).ToList()));
            foreach (var agg in batch.ClassAggregates)
            {
                var deaths = agg.Runs.Where(r => r.Death != null).Select(r => r.Death!).ToList();
                if (deaths.Count == 0)
                    continue;
                string killers = string.Join(", ", deaths.GroupBy(d => d.EnemyName).OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} x{g.Count()}"));
                Console.WriteLine($"  {agg.ClassDisplayName} deaths: max HP at entry {deaths.Average(d => d.MaxHpAtDungeonEntry):F0}, " +
                                  $"fight # in dungeon {deaths.Average(d => d.FightsInDungeon):F1}, killers {killers}");
            }
            if (all.LevelDrift > 0)
                Console.WriteLine($"WARNING: {all.LevelDrift} run(s) ended at a level other than L{level} (level-up inside a dungeon); their later dungeons are not at L{level}.");

            if (!string.IsNullOrWhiteSpace(jsonPath))
            {
                var doc = new
                {
                    Provenance = RunProvenance.Describe(),
                    Level = level,
                    Seed = seed,
                    RunsPerClass = runs,
                    DungeonsPerRun = dungeons,
                    RetreatBelowHpPercent = retreat,
                    Bot = bot,
                    Outcomes = outcomes.Append(all).ToList(),
                    // Per dungeon theme and class (dungeon-sim.py per-dungeon table, Scripts/dungeon-calibrate.py).
                    DungeonOutcomes = batch.ClassAggregates.SelectMany(a => a.Runs.SelectMany(r => r.DungeonOutcomes)
                        .GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(g => new
                        {
                            Class = a.ClassDisplayName, Theme = g.Key,
                            Dungeons = g.Sum(kv => kv.Value.Attempted), Cleared = g.Sum(kv => kv.Value.Cleared),
                            Retreated = g.Sum(kv => kv.Value.Retreated), Died = g.Sum(kv => kv.Value.Died)
                        })).ToList(),
                    // Every real fight at the level (per-enemy reads in Scripts/balance-scan.py).
                    Fights = batch.ClassAggregates.SelectMany(a => a.Runs.SelectMany(r => r.Fights.Where(f => f.HeroLevel >= level).Select(f => new
                    {
                        Class = a.ClassDisplayName, Enemy = f.EnemyName, f.DungeonTheme, f.EnemyLevel, f.HeroActions, f.EnemyActions, f.FightInDungeon, f.HeroDied,
                        HpLost = f.HeroMaxHp > 0 ? Math.Round(Math.Max(0, f.HeroHpStart - f.HeroHpEnd) / (double)f.HeroMaxHp, 3) : 0,
                        f.Strip, f.OwnedActions, f.ActionUses, HpMinPct = Math.Round(f.HpMinPct, 3), f.LeadChanges, f.HeroMisses, f.LongestMissStreak, f.HeroHits, f.ComboHits, f.BossFight, f.StatusApplied, f.StatusSkillStat, f.StatusSkillFirstAction,
                        f.DamageBySource, f.HeroDamageBySource, f.CritDamage, f.ComboDamage, f.ConvertDamage,
                        f.SlotSwings, f.SlotHitDamage, f.SlotMax, f.HeroTechnique, f.HeroIntelligence,
                        f.StripBreaks, f.StripCompletions, f.CarriedInStrip,
                        f.Gear, f.Affixes, f.Sets, f.Skills, f.HeroStrength, f.HeroAgility, f.HeroMaxHp,
                        f.EnemyPrefix, f.EnemyActionUses, f.HeroDamageByEnemyAction,
                        f.EnemyAffixes, f.EnemyWanderer, f.RoomAffix, f.RoomTerrain, f.RoomWanderer, f.DemonPhase
                    }))).ToList(),
                    // Smart bot sequence searches at the level (viable-set reads in Scripts/balance-scan.py).
                    Decisions = batch.ClassAggregates.SelectMany(a => a.Runs.SelectMany(r => r.SequenceDecisions.Where(d => d.HeroLevel >= level).Select(d => new
                    {
                        Class = a.ClassDisplayName, d.HeroLevel, d.SlotMax, d.Required, d.Strip, d.ScoreVsBest
                    }))).ToList(),
                    // Skill picks, gear passes and hub milestones (choice / progression reads). Runs start at the level, so
                    // milestones here are the start-level setup plus any drops picked up between dungeons.
                    SkillDecisions = batch.ClassAggregates.SelectMany(a => a.Runs.SelectMany(r => r.SkillDecisions.Where(d => d.HeroLevel >= level)
                        .Select(d => new { Class = a.ClassDisplayName, d.HeroLevel, d.Chosen, d.ScoreVsBest }))).ToList(),
                    GearDecisions = batch.ClassAggregates.SelectMany(a => a.Runs.SelectMany(r => r.GearDecisions.Where(d => d.HeroLevel >= level)
                        .Select(d => new { Class = a.ClassDisplayName, d.HeroLevel, d.Chosen, d.ScoreVsBest }))).ToList(),
                    Milestones = batch.ClassAggregates.SelectMany(a => a.Runs.SelectMany(r => r.Milestones.Where(m => m.HeroLevel >= level)
                        .Select(m => new { Class = a.ClassDisplayName, m.HeroLevel, m.SlotMax, m.InventoryCount, m.SkillsLearned, m.GearEquipped,
                                           m.StripBefore, m.StripAfter, m.ScoreAfterSkills, m.ScoreFinal }))).ToList(),
                    // One row per hero: gear-pass offers vs wear per material / affix label (adoption reads in Scripts/diversity-report.py).
                    GearPasses = batch.ClassAggregates.SelectMany(a => a.Runs.Select(r => new
                    {
                        Class = a.ClassDisplayName, r.GearOffered, r.GearWorn, r.SlotContest, r.UpgradesEquipped
                    })).ToList()
                };
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
            }

            return 0;
        }

        private static string FormatReport(int level, List<Outcome> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Dungeons at L{level} (95% CI in brackets):");
            sb.AppendLine("  Class        Runs  Dungeons  Clear                 Retreat               Death/dungeon         HPlost/fight  Hero acts");
            foreach (var o in rows)
            {
                sb.AppendLine($"  {o.Class,-11} {o.Runs,5}  {o.Dungeons,8}  {Rate(o.Cleared, o.Dungeons),-20}  {Rate(o.Retreated, o.Dungeons),-20}  {Rate(o.Died, o.Dungeons),-20}  {o.HpLostPerFight * 100,10:F1}%  {o.HeroActionsPerFight,9:F1}");
            }
            return sb.ToString();
        }

        private static string Rate(int k, int n)
        {
            if (n == 0)
                return "-";
            var (lo, hi) = Wilson(k, n);
            return $"{100.0 * k / n:F1}% [{lo * 100:F0}-{hi * 100:F0}]";
        }

        /// <summary>Wilson score interval for k successes in n trials (95%).</summary>
        public static (double lo, double hi) Wilson(int k, int n, double z = 1.96)
        {
            if (n == 0)
                return (0, 1);
            double p = k / (double)n;
            double denom = 1 + z * z / n;
            double centre = (p + z * z / (2 * n)) / denom;
            double half = z * Math.Sqrt(p * (1 - p) / n + z * z / (4.0 * n * n)) / denom;
            return (Math.Max(0, centre - half), Math.Min(1, centre + half));
        }
    }
}
