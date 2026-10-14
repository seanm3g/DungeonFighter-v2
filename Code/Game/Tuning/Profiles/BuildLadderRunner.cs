using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Data;
using RPGGame.Entity.Services;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// BUILDLAB: walks a build up its stepping stones and measures what each step is worth. A build is a list of
    /// cumulative steps (each step adds material pieces, strip actions, skill ranks, stats or a gear upgrade on top of
    /// the previous one); every step fights the eval enemies with a typical-gear hero at the build's level.
    /// Reports hero turns, kill time, HP lost per fight, win rate, and each step's speed-up over the previous step
    /// and over step 0. Builds live in <c>GameData/TuningProfiles/build-ladders.json</c>.
    /// </summary>
    /// <example>DF.exe BUILDLAB --builds B1,B3 --level 30 --encounters 40</example>
    public static class BuildLadderRunner
    {
        public sealed class LadderFile
        {
            [JsonPropertyName("builds")] public List<LadderBuild> Builds { get; set; } = new();
        }

        public sealed class LadderBuild
        {
            [JsonPropertyName("id")] public string Id { get; set; } = "";
            [JsonPropertyName("name")] public string Name { get; set; } = "";
            [JsonPropertyName("weapon")] public string Weapon { get; set; } = "Mace";
            /// <summary>Second class for hybrids (class points split between the two).</summary>
            [JsonPropertyName("secondWeapon")] public string? SecondWeapon { get; set; }
            [JsonPropertyName("levels")] public List<int>? Levels { get; set; }
            [JsonPropertyName("steps")] public List<LadderStep> Steps { get; set; } = new();
        }

        public sealed class LadderStep
        {
            [JsonPropertyName("label")] public string Label { get; set; } = "";
            /// <summary>Material → pieces. Pieces fill weapon, then body, legs, head, feet (later steps override counts).</summary>
            [JsonPropertyName("materials")] public Dictionary<string, int>? Materials { get; set; }
            /// <summary>Replaces the strip (slot order). The weapon basic is added if missing.</summary>
            [JsonPropertyName("strip")] public List<string>? Strip { get; set; }
            /// <summary>Skill node id → rank (added to earlier steps; rank 0 removes).</summary>
            [JsonPropertyName("skills")] public Dictionary<string, int>? Skills { get; set; }
            /// <summary>Flat attribute bonuses added to the hero (STR / AGI / TEC / INT).</summary>
            [JsonPropertyName("stats")] public Dictionary<string, int>? Stats { get; set; }
            /// <summary>Typical gear from this many levels above the build level (a gear upgrade step).</summary>
            [JsonPropertyName("gearLevelOffset")] public int? GearLevelOffset { get; set; }
            /// <summary>Extra strip slots beyond the strip length.</summary>
            [JsonPropertyName("extraSlots")] public int? ExtraSlots { get; set; }
            /// <summary>Material trigger names (MaterialTriggerCatalog) stamped on build pieces in order; replaces earlier steps' list.</summary>
            [JsonPropertyName("triggers")] public List<string>? Triggers { get; set; }
            /// <summary>Modifications.json names added to the weapon (e.g. Poisonous, Flaming, serrated); replaces earlier steps' list.</summary>
            [JsonPropertyName("weaponMods")] public List<string>? WeaponMods { get; set; }
        }

        public sealed class LadderRow
        {
            public string Build { get; init; } = "";
            public int Level { get; init; }
            public int Step { get; init; }
            public string Label { get; init; } = "";
            public double HeroTurns { get; init; }
            /// <summary>Enemy swings while the hero removes a full enemy health bar (game time; losses extrapolated).</summary>
            public double KillTime { get; init; }
            public double DamagePerTurn { get; init; }
            public double HpLostPct { get; init; }
            public int Armor { get; init; }
            public int Slots { get; init; }
            public double WinRate { get; init; }
            /// <summary>Kill-time speed-up vs the previous step (1.10 = 10% faster).</summary>
            public double StepGain { get; init; }
            /// <summary>Kill-time speed-up vs step 0.</summary>
            public double TotalGain { get; init; }
            /// <summary>HP-lost reduction vs step 0 (1.5 = loses a third less HP).</summary>
            public double DefenseGain { get; init; }
            public string Strip { get; init; } = "";
            /// <summary>Per enemy: mean / 90th-percentile hero turns and win rate (one slow enemy hides in the mean).</summary>
            public Dictionary<string, EnemyOutcome> PerEnemy { get; init; } = new();
        }

        public sealed class EnemyOutcome
        {
            public double HeroTurns { get; init; }
            public double HeroTurnsP90 { get; init; }
            public double WinRate { get; init; }
        }

        private readonly record struct Outcome(double HeroTurns, double KillTime, double DamagePerTurn, double HpLostPct, double WinRate, int Armor = 0, int Slots = 0,
            Dictionary<string, EnemyOutcome>? PerEnemy = null);

        public static async Task<int> RunAsync(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 40;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            int? levelOverride = TuningCliArgs.GetIntFlag(args, "--level");
            _traceStep = TuningCliArgs.GetIntFlag(args, "--trace-step");
            _levelSlots = args.Any(a => string.Equals(a, "--level-slots", StringComparison.OrdinalIgnoreCase));
            // --bank TROPHY=6,RAGE=10: material currency banked at the start of every fight (mid-dungeon state).
            ActionLabEncounterSimulator.PlayerMaterialBankSeed.Clear();
            foreach (string pair in (TuningCliArgs.GetFlagValue(args, "--bank") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=');
                if (kv.Length == 2 && int.TryParse(kv[1], out int amount) && amount > 0)
                    ActionLabEncounterSimulator.PlayerMaterialBankSeed[kv[0].Trim()] = amount;
            }
            // Leave-one-out: after the ladder, remove each learned skill node from this step and price what it was worth.
            int? looStep = TuningCliArgs.GetIntFlag(args, "--loo-step");
            int maxStep = TuningCliArgs.GetIntFlag(args, "--max-step") ?? int.MaxValue;
            // Add-one: on top of step --aoo-base, price each skill node learned by steps base+1..--aoo-upto on its own.
            int? aooBase = TuningCliArgs.GetIntFlag(args, "--aoo-base");
            int aooUpto = TuningCliArgs.GetIntFlag(args, "--aoo-upto") ?? int.MaxValue;
            // Long fights: enemy HP × N and the hero cannot die, so every step is measured on sustained damage per
            // hero turn and HP lost per hero turn instead of whole-turn kill counts.
            _longFight = TuningCliArgs.GetFlagValue(args, "--long") is string lf && double.TryParse(lf,
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lfv) ? lfv : 0;
            var enemies = (TuningCliArgs.GetFlagValue(args, "--enemies") ?? "Wolf,Goblin,Skeleton")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var buildFilter = TuningCliArgs.GetFlagValue(args, "--builds")?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            string file = TuningCliArgs.GetFlagValue(args, "--file")
                ?? GameConstants.ResolveGameDataPath("TuningProfiles", "build-ladders.json");

            var ladder = JsonSerializer.Deserialize<LadderFile>(File.ReadAllText(file),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })
                ?? new LadderFile();

            ActionLoader.ReloadActions();
            // --power-all X / --power node=X,node=Y: multiply skill node power for this run (find dead vs weak nodes).
            var trees = GameConfiguration.Instance.SkillTrees.Trees;
            if (TuningCliArgs.GetFlagValue(args, "--power-all") is string pa && double.TryParse(pa,
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double paMult))
                foreach (var node in trees.SelectMany(t => t.Nodes))
                    node.Power *= paMult;
            foreach (string pair in (TuningCliArgs.GetFlagValue(args, "--power") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=');
                if (kv.Length == 2 && double.TryParse(kv[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double mult))
                    foreach (var node in trees.SelectMany(t => t.Nodes).Where(n => string.Equals(n.Id, kv[0].Trim(), StringComparison.OrdinalIgnoreCase)))
                        node.Power *= mult;
            }
            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== BUILDLAB vs {string.Join(",", enemies)} | {encounters} fights/enemy | seed {seed} ===");

            var rows = new List<LadderRow>();
            foreach (var build in ladder.Builds.Where(b => buildFilter == null || buildFilter.Contains(b.Id)))
            {
                var levels = levelOverride.HasValue ? new List<int> { levelOverride.Value } : (build.Levels ?? new List<int> { 30 });
                foreach (int level in levels)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[{build.Id} {build.Name} / {build.Weapon}{(build.SecondWeapon != null ? "+" + build.SecondWeapon : "")} / L{level}]");
                    Console.WriteLine($"{"#",3} {"Step",-34}{"Turns",7}{"KillT",8}{"Dmg/t",8}{"HP%",6}{"Win",6}{"Step×",7}{"Total×",8}{"Def×",6}{"Arm",5}{"Slt",4}  Strip");
                    Outcome? first = null, prev = null;
                    var state = new AccumulatedStep();
                    for (int i = 0; i < build.Steps.Count && i <= maxStep; i++)
                    {
                        state.Apply(build.Steps[i]);
                        _currentStep = i;
                        Outcome r;
                        List<string> strip;
                        try
                        {
                            (r, strip) = await EvaluateAsync(build, state, level, enemies, encounters, seed).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{i,3} {Trunc(build.Steps[i].Label, 33),-34}ERROR {ex.GetType().Name}: {ex.Message}");
                            continue;
                        }
                        first ??= r;
                        double stepGain = prev is { } p && r.KillTime > 0 ? p.KillTime / r.KillTime : 1.0;
                        double totalGain = r.KillTime > 0 ? first.Value.KillTime / r.KillTime : 1.0;
                        double defenseGain = first.Value.HpLostPct / Math.Max(1.0, r.HpLostPct);
                        var row = new LadderRow
                        {
                            Build = build.Id, Level = level, Step = i, Label = build.Steps[i].Label,
                            HeroTurns = r.HeroTurns, KillTime = r.KillTime, DamagePerTurn = r.DamagePerTurn,
                            HpLostPct = r.HpLostPct, WinRate = r.WinRate, StepGain = stepGain, TotalGain = totalGain, DefenseGain = defenseGain,
                            Armor = r.Armor, Slots = r.Slots, Strip = string.Join(">", strip),
                            PerEnemy = r.PerEnemy ?? new()
                        };
                        rows.Add(row);
                        Console.WriteLine($"{i,3} {Trunc(row.Label, 33),-34}{row.HeroTurns,7:F1}{row.KillTime,8:F1}{row.DamagePerTurn,8:F1}{row.HpLostPct,6:F0}{row.WinRate * 100,6:F0}{row.StepGain,7:F2}{row.TotalGain,8:F2}{row.DefenseGain,6:F2}{row.Armor,5}{row.Slots,4}  {row.Strip}");
                        prev = r;
                    }

                    if (looStep is int ls && ls >= 0 && ls < build.Steps.Count && first.HasValue)
                        await LeaveOneOutAsync(build, ls, level, enemies, encounters, seed, rows).ConfigureAwait(false);
                    if (aooBase is int ab && ab >= 0 && ab < build.Steps.Count)
                        await AddOneAsync(build, ab, Math.Min(aooUpto, build.Steps.Count - 1), level, enemies, encounters, seed, rows).ConfigureAwait(false);
                }
            }

            string outPath = TuningCliArgs.GetFlagValue(args, "--json")
                ?? GameConstants.ResolveGameDataPath("TuningRuns", $"buildlab-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Rows: {Path.GetFullPath(outPath)}");
            return 0;
        }

        /// <summary>Print the first fight's event log for this step (debugging mints and converts).</summary>
        private static int? _traceStep;
        /// <summary>--level-slots: trim strips to the hero's real slot max (lootSystem.comboSlotCapByLevel etc.).</summary>
        private static bool _levelSlots;
        private static int _currentStep;
        private static double _longFight;

        private static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        private sealed class AccumulatedStep
        {
            public Dictionary<string, int> Materials { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<string> Strip { get; private set; } = new();
            public Dictionary<string, int> Skills { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Stats { get; } = new(StringComparer.OrdinalIgnoreCase);
            public int GearLevelOffset { get; private set; }
            public int ExtraSlots { get; private set; }
            public List<string> Triggers { get; private set; } = new();
            public List<string> WeaponMods { get; private set; } = new();

            public void Apply(LadderStep step)
            {
                if (step.Materials != null)
                    foreach (var kv in step.Materials)
                        Materials[kv.Key] = kv.Value;
                if (step.Strip != null)
                    Strip = step.Strip.ToList();
                if (step.Skills != null)
                    foreach (var kv in step.Skills)
                    {
                        if (kv.Value <= 0) Skills.Remove(kv.Key);
                        else Skills[kv.Key] = kv.Value;
                    }
                if (step.Stats != null)
                    foreach (var kv in step.Stats)
                        Stats[kv.Key] = (Stats.TryGetValue(kv.Key, out int v) ? v : 0) + kv.Value;
                if (step.GearLevelOffset.HasValue)
                    GearLevelOffset = step.GearLevelOffset.Value;
                if (step.ExtraSlots.HasValue)
                    ExtraSlots = step.ExtraSlots.Value;
                if (step.Triggers != null)
                    Triggers = step.Triggers.ToList();
                if (step.WeaponMods != null)
                    WeaponMods = step.WeaponMods.ToList();
            }
        }

        private const string NeutralMaterial = "Cloth";

        /// <summary>
        /// Prices every skill node learned by steps <paramref name="baseStep"/>+1 … <paramref name="upto"/> on its own, added
        /// to the <paramref name="baseStep"/> state (materials, strip, mods). Value 100 = no effect. Rows: Step -2, Label "AOO id".
        /// </summary>
        private static async Task AddOneAsync(LadderBuild build, int baseStep, int upto, int level, IReadOnlyList<string> enemies,
            int encounters, int seed, List<LadderRow> rows)
        {
            var baseState = new AccumulatedStep();
            for (int i = 0; i <= baseStep; i++)
                baseState.Apply(build.Steps[i]);
            var nodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = baseStep + 1; i <= upto; i++)
                foreach (var kv in build.Steps[i].Skills ?? new Dictionary<string, int>())
                    if (kv.Value > 0) nodes[kv.Key] = kv.Value;
            var (without, _) = await EvaluateAsync(build, baseState, level, enemies, encounters, seed).ConfigureAwait(false);
            Console.WriteLine($"    add-one on step {baseStep} (value = with node / without; 100 = no effect)");
            Console.WriteLine($"    {"Node",-18}{"Rank",5}{"Value",7}{"Off",7}{"Def",7}");
            foreach (var kv in nodes)
            {
                var with = new AccumulatedStep();
                for (int i = 0; i <= baseStep; i++)
                    with.Apply(build.Steps[i]);
                with.Apply(new LadderStep { Skills = new Dictionary<string, int> { [kv.Key] = kv.Value } });
                var (r, _) = await EvaluateAsync(build, with, level, enemies, encounters, seed).ConfigureAwait(false);
                double off = r.KillTime > 0 ? without.KillTime / r.KillTime : 1.0;
                double def = Math.Max(1.0, without.HpLostPct) / Math.Max(1.0, r.HpLostPct);
                double value = off * def * 100;
                Console.WriteLine($"    {kv.Key,-18}{kv.Value,5}{value,7:F0}{off,7:F2}{def,7:F2}");
                rows.Add(new LadderRow
                {
                    Build = build.Id, Level = level, Step = -2, Label = "AOO " + kv.Key, HeroTurns = kv.Value,
                    KillTime = r.KillTime, HpLostPct = r.HpLostPct, StepGain = off, DefenseGain = def, TotalGain = value / 100
                });
            }
        }

        /// <summary>
        /// Prices every skill node learned at step <paramref name="stepIndex"/> in that build's context: the step with the
        /// node removed vs the full step. Value 100 = node does nothing here; offense = kill time, defense = HP lost.
        /// </summary>
        private static async Task LeaveOneOutAsync(LadderBuild build, int stepIndex, int level, IReadOnlyList<string> enemies,
            int encounters, int seed, List<LadderRow> rows)
        {
            var full = new AccumulatedStep();
            for (int i = 0; i <= stepIndex; i++)
                full.Apply(build.Steps[i]);
            var (with, _) = await EvaluateAsync(build, full, level, enemies, encounters, seed).ConfigureAwait(false);
            Console.WriteLine($"    leave-one-out at step {stepIndex} (value = with / without; 100 = no effect)");
            Console.WriteLine($"    {"Node",-18}{"Rank",5}{"Value",7}{"Off",7}{"Def",7}");
            foreach (var kv in full.Skills.ToList())
            {
                var without = new AccumulatedStep();
                for (int i = 0; i <= stepIndex; i++)
                    without.Apply(build.Steps[i]);
                without.Apply(new LadderStep { Skills = new Dictionary<string, int> { [kv.Key] = 0 } });
                var (r, _) = await EvaluateAsync(build, without, level, enemies, encounters, seed).ConfigureAwait(false);
                double off = with.KillTime > 0 ? r.KillTime / with.KillTime : 1.0;
                double def = Math.Max(1.0, r.HpLostPct) / Math.Max(1.0, with.HpLostPct);
                double value = off * def * 100;
                Console.WriteLine($"    {kv.Key,-18}{kv.Value,5}{value,7:F0}{off,7:F2}{def,7:F2}");
                rows.Add(new LadderRow
                {
                    Build = build.Id, Level = level, Step = -1, Label = "LOO " + kv.Key, HeroTurns = kv.Value,
                    KillTime = r.KillTime, HpLostPct = r.HpLostPct, StepGain = off, DefenseGain = def, TotalGain = value / 100
                });
            }
        }

        private static WeaponType ParseWeapon(string s) =>
            Enum.TryParse<WeaponType>(s, true, out var w) ? w : WeaponType.Mace;

        private static async Task<(Outcome, List<string>)> EvaluateAsync(
            LadderBuild build, AccumulatedStep state, int level, IReadOnlyList<string> enemies, int encounters, int seed)
        {
            var weaponType = ParseWeapon(build.Weapon);
            int gearLevel = Math.Clamp(level + state.GearLevelOffset, 1, 100);
            var snapshot = FundamentalsCombatSetup.BuildSnapshot(new SimulationProfileConfig
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                PlayerLevel = gearLevel,
                EnemyLevel = level,
                WeaponType = build.Weapon,
                EnemyType = enemies[0],
                Gear = "typical"
            });
            var hero = LabCombatEntityFactory.ClonePlayerFromJson(snapshot.InitialPlayerJson, "BUILDLAB: hero clone failed.");
            if (gearLevel != level)
            {
                // Gear from a higher level, hero stats from the build level.
                var levelHero = LabCombatEntityFactory.ClonePlayerFromJson(
                    FundamentalsCombatSetup.BuildSnapshot(new SimulationProfileConfig
                    {
                        Mode = TuningSimulationModes.FundamentalsEncounter, PlayerLevel = level, EnemyLevel = level,
                        WeaponType = build.Weapon, EnemyType = enemies[0], Gear = "none"
                    }).InitialPlayerJson, "BUILDLAB: level hero clone failed.");
                hero.Progression.Level = level;
                hero.Stats.Strength = levelHero.Stats.Strength;
                hero.Stats.Agility = levelHero.Stats.Agility;
                hero.Stats.Technique = levelHero.Stats.Technique;
                hero.Stats.Intelligence = levelHero.Stats.Intelligence;
            }

            // Class points: all levels on the build weapon, or split for a hybrid.
            int points = Math.Max(0, level - 1);
            SetClassPoints(hero, weaponType, build.SecondWeapon == null ? points : points - points / 2);
            if (build.SecondWeapon != null)
                SetClassPoints(hero, ParseWeapon(build.SecondWeapon), points / 2);

            foreach (var kv in state.Stats)
            {
                switch (kv.Key.ToUpperInvariant())
                {
                    case "STR": hero.Stats.Strength += kv.Value; break;
                    case "AGI": hero.Stats.Agility += kv.Value; break;
                    case "TEC": hero.Stats.Technique += kv.Value; break;
                    case "INT": hero.Stats.Intelligence += kv.Value; break;
                }
            }

            // Material pieces: weapon first, then body, legs, head, feet.
            // The Material prefix modification is what survives serialization, so stamp it the way loot does.
            // Pieces outside the build get Cloth (no set), so typical gear never forms an accidental set.
            var applier = new LootBonusApplier(LootDataCache.Load(), new Random(seed));
            var slots = new[] { hero.Weapon, hero.Body, hero.Legs, hero.Head, hero.Feet };
            int slot = 0;
            // Named triggers go on the first pieces of their own material; with lootSystem.materialPieceTriggers on, every
            // other build piece rolls one from its pool (seeded), as loot does.
            // "NONE" = pieces carry no trigger (no random roll either): a clean baseline for trigger pricing.
            bool noTriggers = state.Triggers.Any(n => string.Equals(n, "NONE", StringComparison.OrdinalIgnoreCase));
            var namedTriggers = state.Triggers.Where(n => !string.Equals(n, "NONE", StringComparison.OrdinalIgnoreCase)).Select(n =>
            {
                int i = MaterialTriggerCatalog.All.ToList().FindIndex(d => string.Equals(d.TriggerName, n, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(d.TriggerName, d.Material + n, StringComparison.OrdinalIgnoreCase));
                if (i < 0)
                    throw new InvalidOperationException($"Unknown material trigger '{n}'.");
                return MaterialTriggerCatalog.All[i];
            }).ToList();
            bool pieceTriggers = GameConfiguration.Instance.LootSystem?.MaterialPieceTriggers == true && !noTriggers;
            var triggerRng = new Random(seed * 31 + 7);
            foreach (var kv in state.Materials)
            {
                string material = MaterialBuildData.CanonicalMaterialName(kv.Key);
                for (int n = 0; n < kv.Value && slot < slots.Length; n++, slot++)
                {
                    if (slots[slot] == null)
                        continue;
                    applier.ApplySpecificMaterial(slots[slot]!, material);
                    int named = namedTriggers.FindIndex(d => string.Equals(d.Material, material, StringComparison.OrdinalIgnoreCase));
                    if (named >= 0)
                    {
                        MaterialTriggerMerge.AppendDef(slots[slot]!, namedTriggers[named]);
                        namedTriggers.RemoveAt(named);
                    }
                    else if (pieceTriggers)
                        MaterialTriggerMerge.ApplyMaterialTrigger(slots[slot]!, triggerRng);
                }
            }
            for (; slot < slots.Length; slot++)
                if (slots[slot] != null)
                    applier.ApplySpecificMaterial(slots[slot]!, NeutralMaterial);
            MaterialSetController.SyncConvertActionsToPool(hero);

            if (state.WeaponMods.Count > 0 && hero.Weapon != null)
            {
                var table = LootDataCache.Load().Modifications ?? new List<Modification>();
                foreach (string modName in state.WeaponMods)
                {
                    var template = table.FirstOrDefault(m => string.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException($"Unknown modification '{modName}'.");
                    var mod = JsonSerializer.Deserialize<Modification>(JsonSerializer.Serialize(template))!;
                    mod.RolledValue = Math.Max(mod.RolledValue, mod.MaxValue);
                    hero.Weapon.Modifications.Add(mod);
                }
            }

            hero.Progression.LearnedSkillRanks.Clear();
            foreach (var kv in state.Skills)
                hero.Progression.LearnedSkillRanks[kv.Key] = kv.Value;

            var strip = state.Strip.Count > 0 ? state.Strip.ToList() : hero.GetComboActions().Select(a => a.Name).ToList();
            if (SequenceLabRunner.WeaponBasics.TryGetValue(build.Weapon, out string? basic)
                && !strip.Contains(basic, StringComparer.OrdinalIgnoreCase))
                strip.Insert(0, basic);
            if (_levelSlots)
                strip = TrimStripToSlots(strip, ComboSequenceMaxHelper.GetEffectiveMax(hero), basic);
            foreach (string name in strip.Where(n => ActionLoader.GetAction(n) == null))
                throw new InvalidOperationException($"Unknown action '{name}'.");

            int heroArmor = hero.GetTotalArmor();
            int heroSlots = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            string json = new CharacterSerializer().Serialize(hero);
            double turns = 0, time = 0, dmgPerTurn = 0, hpLostPct = 0, wins = 0;
            int fights = 0, timedFights = 0;
            var perEnemy = new Dictionary<string, EnemyOutcome>(StringComparer.OrdinalIgnoreCase);
            ActionLabEncounterSimulator.UseNaturalComboRouting = true;
            ActionLabEncounterSimulator.EnemyHealthScale = _longFight > 0 ? _longFight : 1.0;
            ActionLabEncounterSimulator.PlayerHealthScale = _longFight > 0 ? _longFight * 10 : 1.0;
            try
            {
                foreach (string enemy in enemies)
                {
                    var snap = new LabCombatSnapshot(
                        json, 0, 0, 0, 0, 0, 0, Math.Max(0, strip.Count - 1 + state.ExtraSlots),
                        sessionEnemyLoaderType: enemy,
                        enemyLevel: level,
                        comboStripActionNames: strip,
                        selectedCatalogActionName: strip[0],
                        labEnemyBattleConfig: null);
                    string? invalid = ActionLabEncounterSimulator.ValidateSnapshot(snap);
                    if (invalid != null)
                        throw new InvalidOperationException(invalid);
                    bool trace = _traceStep == _currentStep && enemy == enemies[0];
                    ActionLabEncounterSimulator.CaptureEventLog = trace;
                    var report = await ActionLabEncounterSimulator.RunBatchAsync(snap, trace ? 1 : encounters, new Random(seed), maxDegreeOfParallelism: 1)
                        .ConfigureAwait(false);
                    ActionLabEncounterSimulator.CaptureEventLog = false;
                    if (trace)
                    {
                        foreach (string line in report.Encounters.FirstOrDefault()?.EventLog ?? new List<string>())
                            Console.WriteLine("    | " + line);
                        report = await ActionLabEncounterSimulator.RunBatchAsync(snap, encounters, new Random(seed), maxDegreeOfParallelism: 1)
                            .ConfigureAwait(false);
                    }
                    var enemyTurns = new List<int>();
                    int enemyWins = 0;
                    foreach (var e in report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)))
                    {
                        enemyTurns.Add(Math.Max(1, e.PlayerTurns));
                        enemyWins += e.PlayerWon ? 1 : 0;
                        fights++;
                        wins += e.PlayerWon ? 1 : 0;
                        int t = Math.Max(1, e.PlayerTurns);
                        turns += t;
                        dmgPerTurn += e.PlayerDamageDealt / (double)t;
                        double lost = e.PlayerMaxHealth > 0 ? 100.0 * Math.Max(0, e.PlayerMaxHealth - e.PlayerFinalHealth) / e.PlayerMaxHealth : 0;
                        // Long fights: % of real max HP lost per enemy swing × 10 (about one normal fight's swings).
                        int swings = Math.Max(1, e.EnemyTurns);
                        hpLostPct += _longFight > 0 ? lost * (_longFight * 10) / swings * 10 : lost;
                        // Kill time = enemy swings during the kill (enemy swing time is the same every step, so this is
                        // game time and counts hero speed), scaled to a full health bar (losses count too).
                        double removed = Math.Max(1, e.EnemyMaxHealth - Math.Max(0, e.EnemyFinalHealth));
                        time += swings * (double)e.EnemyMaxHealth / removed;
                        timedFights++;
                    }
                    if (enemyTurns.Count > 0)
                    {
                        enemyTurns.Sort();
                        perEnemy[enemy] = new EnemyOutcome
                        {
                            HeroTurns = enemyTurns.Average(),
                            HeroTurnsP90 = enemyTurns[Math.Min(enemyTurns.Count - 1, (int)Math.Ceiling(0.9 * enemyTurns.Count) - 1)],
                            WinRate = enemyWins / (double)enemyTurns.Count
                        };
                    }
                }
            }
            finally
            {
                ActionLabEncounterSimulator.UseNaturalComboRouting = false;
                ActionLabEncounterSimulator.EnemyHealthScale = 1.0;
                ActionLabEncounterSimulator.PlayerHealthScale = 1.0;
            }

            if (fights == 0)
                return (default, strip);
            return (new Outcome(turns / fights, timedFights > 0 ? time / timedFights : 0, dmgPerTurn / fights, hpLostPct / fights, wins / fights, heroArmor, heroSlots, perEnemy), strip);
        }

        /// <summary>
        /// Keeps at most <paramref name="max"/> actions: the weapon basic first, then material converts (the step's new
        /// engine), then the rest in strip order; the kept actions stay in their original order.
        /// </summary>
        private static List<string> TrimStripToSlots(List<string> strip, int max, string? basic)
        {
            max = Math.Max(1, max);
            if (strip.Count <= max)
                return strip;
            bool IsConvert(string n) => ActionLoader.GetActionData(n) is { } d && !string.IsNullOrWhiteSpace(d.ScaleFormula);
            var order = Enumerable.Range(0, strip.Count)
                .OrderBy(i => string.Equals(strip[i], basic, StringComparison.OrdinalIgnoreCase) ? 0 : IsConvert(strip[i]) ? 1 : 2)
                .ThenBy(i => i)
                .Take(max)
                .OrderBy(i => i);
            return order.Select(i => strip[i]).ToList();
        }

        private static void SetClassPoints(Character hero, WeaponType weapon, int points)
        {
            switch (weapon)
            {
                case WeaponType.Mace: hero.Progression.BarbarianPoints = points; break;
                case WeaponType.Sword: hero.Progression.WarriorPoints = points; break;
                case WeaponType.Dagger: hero.Progression.RoguePoints = points; break;
                case WeaponType.Wand: hero.Progression.WizardPoints = points; break;
            }
        }
    }
}
