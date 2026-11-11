using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Data;
using RPGGame.Utils;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Fast headless player (DUNGEONSIM --bot rules): skills, gear and strip by fixed rules, no simulated fights, so a
    /// shard's time goes to the dungeons instead of decisions. Skill order and strip actions come from
    /// <c>GameData/TuningProfiles/rules-bot.json</c>, mined from smart-bot runs by <c>Scripts/rules-bot-mine.py</c>.
    /// Gear: the home class's best-covered material set, the rest of the slots by <see cref="ItemValueModel"/>.
    /// The weapon stays the starting type (no class switching). Check it against the smart bot on paired seeds.
    /// </summary>
    public sealed class PlaythroughRulesPolicy
    {
        private sealed class RulesData
        {
            public Dictionary<string, List<string>> SkillOrder { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<string>> ActionPriority { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Dictionary<string, double>> ActionOrder { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>"Class@Level" → strip length the list tops up to (set by Scripts/rules-bot-climb.py); unset = 2.</summary>
            public Dictionary<string, int> StripLength { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Explore mode (DUNGEONSIM --explore-strips): every dungeon gets a random strip (set converts plus a random
        /// length of random owned actions); Scripts/rules-bot-climb.py --explore reads the owned actions from those fight rows.
        /// </summary>
        public static bool ExploreStrips { get; set; }

        /// <summary>
        /// Synergy mode (DUNGEONSIM --bot synergy): gear first, then skills and strip scored by <see cref="RulesSynergy"/>
        /// against the worn sets' WHEN triggers and the learned nodes, on top of the mined priority lists.
        /// </summary>
        public static bool Synergy { get; set; }

        private static RulesData? _data;
        private readonly Action<string>? _trace;
        private string _lastSignature = "";

        public int SkillsLearned { get; private set; }
        public int GearChanges { get; private set; }
        /// <summary>Per gear pass, every wearable item in the bag or worn counts once: "mat:Bone" and each affix label → item-passes offered / worn.</summary>
        public Dictionary<string, int> GearOffered { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> GearWorn { get; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Per gear pass and slot with 2+ candidates: runner-up value / best value (1 = a real choice, 0 = no contest).</summary>
        public List<double> SlotContest { get; } = new();
        public int SequenceChanges { get; private set; }
        public List<PlaythroughMilestone> Milestones { get; } = new();

        public PlaythroughRulesPolicy(Action<string>? trace) => _trace = trace;

        private static RulesData Data
        {
            get
            {
                if (_data != null)
                    return _data;
                string path = GameConstants.ResolveGameDataPath("TuningProfiles", "rules-bot.json");
                _data = File.Exists(path)
                    ? JsonSerializer.Deserialize<RulesData>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RulesData()
                    : new RulesData();
                return _data;
            }
        }

        public void Optimize(Character? hero, WeaponType homeType)
        {
            if (hero == null)
                return;
            string signature = Signature(hero, homeType);
            if (signature == _lastSignature && !ExploreStrips)
                return;
            string cls = ClassName(homeType);
            var milestone = new PlaythroughMilestone
            {
                HeroLevel = hero.Level,
                SlotMax = ComboSequenceMaxHelper.GetEffectiveMax(hero),
                InventoryCount = hero.Inventory.Count,
                StripBefore = StripOf(hero)
            };
            PlaythroughFightRecorder.Suspended = true;
            try
            {
                if (Synergy)
                {
                    // Gear first so skills can follow the set; strip twice so node picks see the strip and the strip
                    // sees the new nodes.
                    EquipGear(hero, homeType, milestone);
                    BuildStrip(hero, homeType, StripKey(cls, hero.Level));
                    LearnSkills(hero, homeType, cls, milestone);
                    BuildStrip(hero, homeType, StripKey(cls, hero.Level));
                }
                else
                {
                    LearnSkills(hero, homeType, cls, milestone);
                    EquipGear(hero, homeType, milestone);
                    BuildStrip(hero, homeType, StripKey(cls, hero.Level));
                }
            }
            finally
            {
                PlaythroughFightRecorder.Suspended = false;
            }
            milestone.StripAfter = StripOf(hero);
            Milestones.Add(milestone);
            _lastSignature = Signature(hero, homeType);
        }

        // ---------------------------------------------------------------- skills

        /// <summary>
        /// Walks the mined order: the k-th mention of a node means "rank k". Takes the first entry the hero can learn
        /// now; when none can, the first available node in tree order. Free nodes first.
        /// </summary>
        private void LearnSkills(Character hero, WeaponType homeType, string cls, PlaythroughMilestone milestone)
        {
            var order = Data.SkillOrder.GetValueOrDefault(cls) ?? new List<string>();
            for (int guard = 0; guard < 200; guard++)
            {
                var available = SkillTreeService.BuildDisplayModel(hero.Progression).AllNodes
                    .Where(n => SkillTreeService.GetNodeState(hero.Progression, n) == SkillTreeService.NodeViewState.Available)
                    .ToList();
                var free = available.FirstOrDefault(n => n.Cost == 0);
                SkillTreeNodeDefinition? pick = free;
                if (pick == null && Synergy && hero.Progression.GetAvailableSkillPoints(homeType) > 0)
                    pick = SynergySkillPick(hero, homeType, order, available);
                else if (pick == null && hero.Progression.GetAvailableSkillPoints(homeType) > 0)
                {
                    var byId = available.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
                    var mentions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (string id in order)
                    {
                        int rank = mentions[id] = mentions.GetValueOrDefault(id) + 1;
                        if (hero.Progression.GetSkillRank(id) < rank && byId.TryGetValue(id, out var node))
                        {
                            pick = node;
                            break;
                        }
                    }
                    pick ??= available.FirstOrDefault();
                }
                if (pick == null || SkillTreeService.TryLearn(hero, pick.Id) != CharacterProgression.LearnSkillResult.Success)
                    return;
                if (pick.Cost > 0)
                    milestone.SkillsLearned.Add(pick.Id);
                SkillsLearned++;
                _trace?.Invoke($"    rules: learned skill {pick.Id}");
            }
        }

        /// <summary>
        /// Synergy skill pick: the mined order's position (next mined pick scores 1, the end of the list 0, unlisted 0)
        /// plus a bonus for the main worn set's branch, for set-scaling nodes while a home set is worn, and for nodes
        /// whose "node:" weights match the current strip.
        /// </summary>
        private SkillTreeNodeDefinition? SynergySkillPick(Character hero, WeaponType homeType, List<string> order, List<SkillTreeNodeDefinition> available)
        {
            var sd = RulesSynergy.Data;
            var sets = RulesSynergy.FormedSets(hero);
            string home = PlaythroughSmartPolicy.ClassKeyOf(homeType);
            string? mainMaterial = sets.Count > 0 ? sets[0].Material : null;
            bool homeSet = sets.Any(s => string.Equals(MaterialBuildsLoader.FindByMaterial(s.Material)?.AssociatedClass, home, StringComparison.OrdinalIgnoreCase));
            var strip = StripOf(hero);

            var minedIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mentions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i];
                int rank = mentions[id] = mentions.GetValueOrDefault(id) + 1;
                if (rank == hero.Progression.GetSkillRank(id) + 1 && !minedIndex.ContainsKey(id))
                    minedIndex[id] = i;
            }
            int first = minedIndex.Count > 0 ? minedIndex.Values.Min() : 0;
            double span = Math.Max(1, order.Count - first);

            SkillTreeNodeDefinition? best = null;
            double bestScore = double.NegativeInfinity;
            string bestWhy = "";
            foreach (var node in available.Where(n => hero.Progression.GetSkillRank(n.Id) < n.MaxRank))
            {
                double mined = minedIndex.TryGetValue(node.Id, out int idx) ? 1 - (idx - first) / span : 0;
                double branch = mainMaterial != null && string.Equals(node.Branch, mainMaterial, StringComparison.OrdinalIgnoreCase)
                    && !node.Id.Contains("-sink-", StringComparison.OrdinalIgnoreCase) ? sd.BranchBonus : 0;
                double setNode = homeSet && sd.SetNodes.Contains(node.Id, StringComparer.OrdinalIgnoreCase) ? sd.SetNodeBonus : 0;
                double stripFit = sd.SkillFromStripScale * RulesSynergy.NodeStripSynergy(node.Id, strip);
                double score = mined + branch + setNode + stripFit;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = node;
                    bestWhy = $"mined {mined:0.00} branch {branch:0.00} set {setNode:0.00} strip {stripFit:0.00}";
                }
            }
            if (best != null)
                _trace?.Invoke($"    synergy: skill {best.Id} ({bestWhy})");
            return best;
        }

        // ---------------------------------------------------------------- gear

        /// <summary>
        /// The set material with the most coverable slots wins, the home class's materials first (a set needs two
        /// pieces). Its best piece per slot is worn; other slots take the best-value wearable item. A set weapon is
        /// used only while it keeps 75% of the best weapon's value.
        /// </summary>
        private void EquipGear(Character hero, WeaponType homeType, PlaythroughMilestone milestone)
        {
            string home = PlaythroughSmartPolicy.ClassKeyOf(homeType);
            var slots = new[] { "weapon", "head", "body", "legs", "feet" };
            var pool = hero.Inventory
                .Concat(new[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet }.Where(i => i != null).Select(i => i!))
                .Where(i => PlaythroughGearPolicy.SlotOf(i) != "")
                .Where(i => i is not WeaponItem w || w.WeaponType == homeType)
                .Where(i => i.GetEquipBlockedReason(hero) == null)
                .Distinct()
                .ToList();
            var bestBySlot = slots.ToDictionary(s => s,
                s => pool.Where(i => PlaythroughGearPolicy.SlotOf(i) == s).OrderByDescending(ItemValueModel.Score).FirstOrDefault());

            var sets = pool
                .Select(i => (item: i, material: ItemMaterialRules.RemapLegacyMaterial(i.Material)))
                .Where(x => !string.IsNullOrWhiteSpace(x.material) && MaterialBuildsLoader.FindByMaterial(x.material) != null)
                .GroupBy(x => x.material, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var pieces = g.GroupBy(x => PlaythroughGearPolicy.SlotOf(x.item))
                        .Select(s => s.Select(x => x.item).OrderByDescending(ItemValueModel.Score).First())
                        .Where(i => i is not WeaponItem || ItemValueModel.Score(i) >= 0.75 * ItemValueModel.Score(bestBySlot["weapon"]!))
                        .ToList();
                    string? owner = MaterialBuildsLoader.FindByMaterial(g.Key)?.AssociatedClass;
                    bool isHome = string.Equals(owner, home, StringComparison.OrdinalIgnoreCase);
                    // Class-less sets pay the wearer's top attribute, so they compete with home sets on piece count.
                    bool wearable = isHome || string.IsNullOrWhiteSpace(owner);
                    return (material: g.Key, pieces, isHome, wearable);
                })
                .Where(s => s.pieces.Count >= MaterialBuildData.StackUnlockCount)
                .OrderByDescending(s => s.wearable)
                .ThenByDescending(s => s.pieces.Count)
                .ThenByDescending(s => s.isHome)
                .ThenByDescending(s => s.pieces.Sum(ItemValueModel.Score))
                .ToList();

            var target = new Dictionary<string, Item?>(bestBySlot);
            if (sets.Count > 0)
                foreach (var piece in sets[0].pieces)
                    target[PlaythroughGearPolicy.SlotOf(piece)] = piece;

            foreach (string slot in slots)
            {
                var item = target[slot];
                if (item == null || ReferenceEquals(item, SlotItem(hero, slot)) || !hero.Inventory.Contains(item))
                    continue;
                if (PlaythroughSmartPolicy.TryEquipItem(hero, item))
                {
                    milestone.GearEquipped.Add(item.Name);
                    GearChanges++;
                }
            }
            if (sets.Count > 0)
                _trace?.Invoke($"    rules: set {sets[0].material} x{MaterialSetController.CountEquipped(hero, sets[0].material)}");
            RecordGearPass(hero, pool, slots);
        }

        private void RecordGearPass(Character hero, List<Item> pool, string[] slots)
        {
            var worn = new HashSet<Item>(new[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet }.Where(i => i != null).Select(i => i!));
            foreach (var item in pool)
            {
                string mat = ItemMaterialRules.RemapLegacyMaterial(item.Material);
                var keys = AffixLabels.Of(item);
                keys.Add("mat:" + (string.IsNullOrWhiteSpace(mat) ? "none" : mat));
                bool isWorn = worn.Contains(item);
                foreach (string k in keys.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    GearOffered[k] = GearOffered.GetValueOrDefault(k) + 1;
                    if (isWorn)
                        GearWorn[k] = GearWorn.GetValueOrDefault(k) + 1;
                }
            }
            foreach (string slot in slots)
            {
                var scores = pool.Where(i => PlaythroughGearPolicy.SlotOf(i) == slot).Select(ItemValueModel.Score)
                    .OrderByDescending(s => s).Take(2).ToList();
                if (scores.Count == 2 && scores[0] > 0)
                    SlotContest.Add(Math.Round(Math.Max(0, scores[1]) / scores[0], 3));
            }
        }

        private static Item? SlotItem(Character hero, string slot) => slot switch
        {
            "weapon" => hero.Weapon, "head" => hero.Head, "body" => hero.Body, "legs" => hero.Legs, _ => hero.Feet
        };

        // ---------------------------------------------------------------- strip

        /// <summary>
        /// Granted convert actions plus the class's most-run actions the hero owns (at the closest mined level), up to
        /// the slot cap, ordered by where the smart bot tends to place them. Checked 2026-11-01: full strips matched the
        /// smart bot best; mined set preference and filling with unlisted actions read worse.
        /// </summary>
        private void BuildStrip(Character hero, WeaponType homeType, string cls)
        {
            int max = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            var owned = hero.GetActionPool().Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            int length = Math.Max(1, max);
            var picks = MaterialSetController.GetGrantedConvertActionNames(hero).Where(owned.Contains).ToList();
            if (Synergy && !ExploreStrips)
            {
                SynergyStrip(hero, cls, owned, picks, length);
                return;
            }
            foreach (string a in Data.ActionPriority.GetValueOrDefault(cls) ?? new List<string>())
                if (picks.Count < length && owned.Contains(a) && !picks.Contains(a, StringComparer.OrdinalIgnoreCase))
                    picks.Add(a);
            if (ExploreStrips)
            {
                var rest = owned.Where(a => !picks.Contains(a, StringComparer.OrdinalIgnoreCase)).OrderBy(_ => RandomUtility.NextDouble()).ToList();
                int want = RandomUtility.Next(1, length + 1);
                picks = picks.Take(want).Concat(rest).Take(want).OrderBy(_ => RandomUtility.NextDouble()).ToList();
            }
            // A level list can name too few owned actions (smart-mined lists hold other classes' actions: L75 Barbarian
            // ran BONE WRATH alone). Top up to the self-mined StripLength (default 2) from the class's other lists,
            // nearest level first.
            int floor = ExploreStrips ? 0 : Math.Min(StripLengthFor(cls), length);
            if (picks.Count < floor)
                foreach (string key in FallbackKeys(cls, hero.Level).Skip(1))
                    foreach (string a in Data.ActionPriority.GetValueOrDefault(key) ?? new List<string>())
                        if (picks.Count < floor && owned.Contains(a) && !picks.Contains(a, StringComparer.OrdinalIgnoreCase))
                            picks.Add(a);
            if (picks.Count == 0)
                return;
            var position = Data.ActionOrder.GetValueOrDefault(cls) ?? new Dictionary<string, double>();
            var strip = ExploreStrips ? picks.Take(length).ToList() : picks.Take(length).OrderBy(a => position.GetValueOrDefault(a, 0.5)).ToList();
            if (strip.SequenceEqual(StripOf(hero), StringComparer.OrdinalIgnoreCase))
                return;
            hero.RestoreComboFromActionNames(strip);
            WeaponRequiredComboAction.EnsureRequiredBasicInCombo(hero);
            hero.ComboStep = 0;
            SequenceChanges++;
            _trace?.Invoke($"    rules: sequence {string.Join(" > ", StripOf(hero))}");
        }

        /// <summary>
        /// Synergy strip: set converts first, then owned actions by mined priority (nearest level list 1.0 down to 0.5,
        /// farther lists decayed) plus synergy points; actions below MinScore stay off unless a fill-slot node is learned
        /// or the self-mined strip length floor needs them. Ordered by the mined slot positions.
        /// </summary>
        private void SynergyStrip(Character hero, string cls, HashSet<string> owned, List<string> picks, int length)
        {
            var sources = RulesSynergy.ActiveSources(hero);
            var baseScore = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            double decay = 1;
            foreach (string key in FallbackKeys(cls, hero.Level))
            {
                var list = Data.ActionPriority.GetValueOrDefault(key) ?? new List<string>();
                for (int i = 0; i < list.Count; i++)
                {
                    double s = decay * (1 - 0.5 * i / Math.Max(1, list.Count));
                    if (s > baseScore.GetValueOrDefault(list[i]))
                        baseScore[list[i]] = s;
                }
                decay *= 0.85;
            }
            var scored = owned.Where(a => !picks.Contains(a, StringComparer.OrdinalIgnoreCase))
                .Select(a => (name: a, mined: baseScore.GetValueOrDefault(a),
                    syn: RulesSynergy.ActionSynergy(a, sources) + RulesSynergy.VerbSynergy(hero, a)))
                .Select(x => (x.name, x.mined, x.syn, total: x.mined + x.syn))
                .OrderByDescending(x => x.total)
                .ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            bool fill = RulesSynergy.FillsSlots(hero);
            int floor = Math.Min(StripLengthFor(cls), length);
            foreach (var x in scored)
            {
                if (picks.Count >= length)
                    break;
                if (x.total >= RulesSynergy.Data.MinScore || fill || picks.Count < floor)
                    picks.Add(x.name);
            }
            if (picks.Count == 0)
                return;
            var position = Data.ActionOrder.GetValueOrDefault(cls) ?? new Dictionary<string, double>();
            var strip = picks.Take(length).OrderBy(a => position.GetValueOrDefault(a, 0.5)).ToList();
            if (strip.SequenceEqual(StripOf(hero), StringComparer.OrdinalIgnoreCase))
                return;
            hero.RestoreComboFromActionNames(strip);
            WeaponRequiredComboAction.EnsureRequiredBasicInCombo(hero);
            hero.ComboStep = 0;
            SequenceChanges++;
            if (_trace != null)
            {
                var why = scored.Where(x => strip.Contains(x.name, StringComparer.OrdinalIgnoreCase))
                    .Select(x => $"{x.name} {x.mined:0.00}+{x.syn:0.00}");
                _trace($"    synergy: sources {string.Join(", ", sources.Select(s => $"{s.Source}x{s.Scale:0.##}"))}");
                _trace($"    synergy: sequence {string.Join(" > ", StripOf(hero))}  [{string.Join(", ", why)}]");
            }
        }

        /// <summary>The mined "Class@Level" strip key closest to <paramref name="level"/>, else the class key.</summary>
        private static string StripKey(string cls, int level)
        {
            string prefix = cls + "@";
            var levels = Data.ActionPriority.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && int.TryParse(k[prefix.Length..], out _))
                .Select(k => int.Parse(k[prefix.Length..]))
                .ToList();
            return levels.Count == 0 ? cls : prefix + levels.OrderBy(l => Math.Abs(l - level)).First();
        }

        /// <summary>Self-mined strip length for the "Class@Level" key, else the class key, else 2.</summary>
        private static int StripLengthFor(string key)
        {
            if (Data.StripLength.TryGetValue(key, out int n) && n > 0)
                return n;
            int at = key.IndexOf('@');
            return at > 0 && Data.StripLength.TryGetValue(key[..at], out n) && n > 0 ? n : 2;
        }

        /// <summary><paramref name="key"/> itself, then the same class's other "Class@Level" lists nearest first, then the class key.</summary>
        private static IEnumerable<string> FallbackKeys(string key, int level)
        {
            int at = key.IndexOf('@');
            string cls = at < 0 ? key : key[..at];
            string prefix = cls + "@";
            yield return key;
            var others = Data.ActionPriority.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && int.TryParse(k[prefix.Length..], out _))
                .Where(k => !string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => Math.Abs(int.Parse(k[prefix.Length..]) - level));
            foreach (string k in others)
                yield return k;
            if (!string.Equals(cls, key, StringComparison.OrdinalIgnoreCase))
                yield return cls;
        }

        private static string ClassName(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => "Barbarian",
            WeaponType.Sword => "Warrior",
            WeaponType.Dagger => "Rogue",
            WeaponType.Wand => "Wizard",
            _ => ""
        };

        private static List<string> StripOf(Character hero) => hero.GetComboActions().Select(a => a.Name).ToList();

        private static string Signature(Character hero, WeaponType weaponType) =>
            $"{hero.Level}|{hero.Inventory.Count}|{string.Join(",", hero.Inventory.Select(i => i.Name))}|{hero.GetActionPool().Count}|{ComboSequenceMaxHelper.GetEffectiveMax(hero)}|{hero.Progression.GetAvailableSkillPoints(weaponType)}";
    }
}
