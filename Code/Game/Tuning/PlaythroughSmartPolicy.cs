using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Data;
using RPGGame.Entity.Services;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// "Smart" headless player: at the hub it spends skill points, picks gear and builds its action sequence by
    /// <b>simulation</b> — every candidate is scored with short seeded fights at the hero's level, so set bonuses,
    /// triggers and action synergies count because they actually fire. Re-evaluates only when something changed.
    /// </summary>
    public sealed class PlaythroughSmartPolicy
    {
        /// <summary>Enemies used for candidate scoring (plain attackers plus one chain-resetter).</summary>
        public static string[] EvalEnemies { get; set; } = { "Wolf", "Goblin" };
        public static int FightsPerEnemy { get; set; } = 6;
        /// <summary>A candidate must beat the current setup by this factor to be adopted (noise guard).</summary>
        public static double ImprovementThreshold { get; set; } = 1.02;
        /// <summary>
        /// Gear moves into the hero's own class sets (a whole-set package, or a piece that grows a worn class set) are
        /// adopted at this factor of the current score instead of <see cref="ImprovementThreshold"/>: class sets are how
        /// heroes scale, and their payoff (convert banks, stack 3/5 feeds) grows past what short eval fights see.
        /// </summary>
        public static double ClassSetThreshold { get; set; } = 0.97;
        /// <summary>Hero turns a lost eval fight counts as (well above the slowest wins, ~25 turns).</summary>
        public static int LostFightTurns { get; set; } = 50;
        /// <summary>
        /// Skill and gear picks with more than <see cref="ScreenKeep"/> candidates screen them at this many fights per
        /// enemy first; only the top <see cref="ScreenKeep"/> are scored in full and can be picked. The sequence search's
        /// one-pass action screen uses it too (its shortlist is re-scored in full). 0 = all in full. Checked 2026-10-27:
        /// first-hub setup score 0.995 of full scoring [0.968-1.022] over 48 paired heroes, ~28% less sim time.
        /// </summary>
        public static int ScreenFightsPerEnemy { get; set; } = 2;
        public static int ScreenKeep { get; set; } = 3;
        // Scores within one hub re-optimization by fights + strip + serialized hero: the setup a gear pass equips is
        // re-scored as the next pass's baseline and as the sequence search's starting strip.
        private readonly Dictionary<string, double> _scoreCache = new(StringComparer.Ordinal);

        private string _lastSignature = "";
        private readonly Action<string>? _trace;

        public int Evaluations { get; private set; }

        /// <summary>DF_SMARTPROF=1: time split of smart decisions (whole process), printed by the batch runner.</summary>
        public static readonly System.Diagnostics.Stopwatch ProfSerialize = new(), ProfSim = new(), ProfClone = new(), ProfOptimize = new();
        public static long ProfEvaluations;
        public int SkillsLearned { get; private set; }
        public int GearChanges { get; private set; }
        public int SequenceChanges { get; private set; }
        /// <summary>Every sequence search, with how close each owned action came to the best strip (variety reads).</summary>
        public List<PlaythroughSequenceDecision> SequenceDecisions { get; } = new();
        /// <summary>Every paid skill pick: each available node's score over the best node's.</summary>
        public List<PlaythroughChoiceDecision> SkillDecisions { get; } = new();
        /// <summary>Every gear pass: "keep" and each candidate item's score over the best.</summary>
        public List<PlaythroughChoiceDecision> GearDecisions { get; } = new();
        /// <summary>One row per hub re-optimization: what changed and the setup's score after skills and at the end.</summary>
        public List<PlaythroughMilestone> Milestones { get; } = new();
        private PlaythroughMilestone? _milestone;

        public PlaythroughSmartPolicy(Action<string>? trace) => _trace = trace;

        public async Task OptimizeAsync(Character? hero, WeaponType weaponType)
        {
            if (hero == null)
                return;
            // weaponType is the starting class; the hero may switch weapons (and so classes) whenever gear is better.
            // Set preference stays with the starting class, so a Barbarian on a found dagger still builds Bone/Iron/Steel.
            string homeClass = ClassKeyOf(weaponType);
            weaponType = CurrentWeaponType(hero, weaponType);
            string signature = Signature(hero, weaponType);
            if (signature == _lastSignature)
                return;

            PlaythroughFightRecorder.Suspended = true;
            ProfOptimize.Start();
            _scoreCache.Clear();
            _milestone = new PlaythroughMilestone
            {
                HeroLevel = hero.Level,
                SlotMax = ComboSequenceMaxHelper.GetEffectiveMax(hero),
                InventoryCount = hero.Inventory.Count,
                StripBefore = StripOf(hero)
            };
            try
            {
                await SpendSkillPointsAsync(hero, weaponType).ConfigureAwait(false);
                await OptimizeGearAsync(hero, homeClass).ConfigureAwait(false);
                weaponType = CurrentWeaponType(hero, weaponType);
                await OptimizeSequenceAsync(hero, weaponType).ConfigureAwait(false);
            }
            finally
            {
                ProfOptimize.Stop();
                PlaythroughFightRecorder.Suspended = false;
                _milestone.StripAfter = StripOf(hero);
                Milestones.Add(_milestone);
                _milestone = null;
            }

            _lastSignature = Signature(hero, weaponType);
            if (System.Environment.GetEnvironmentVariable("DF_MEMTRACE") == "1")
                Console.Error.WriteLine($"[memtrace] L{hero.Level} evals {Evaluations} heap {GC.GetTotalMemory(true) / (1024 * 1024)} MB");
        }

        // ---------------------------------------------------------------- skills

        private async Task SpendSkillPointsAsync(Character hero, WeaponType weaponType)
        {
            LearnFreeNodes(hero);
            for (int guard = 0; guard < 20 && hero.Progression.GetAvailableSkillPoints(weaponType) > 0; guard++)
            {
                LearnFreeNodes(hero);
                var available = SkillTreeService.BuildDisplayModel(hero.Progression).AllNodes
                    .Where(n => SkillTreeService.GetNodeState(hero.Progression, n) == SkillTreeService.NodeViewState.Available)
                    .ToList();
                // One-step scoring never credits a trunk step for the branches it opens, so it sank points into trunk
                // ranks behind a weak tip. Walk the trunk at rank 1 first; ranks compete with branches once it is done.
                available = available.Where(n => !IsTrunkRankUpBeforeTip(hero, n)).ToList();
                if (available.Count == 0)
                    return;

                var candidates = new List<(string Key, Character Clone)>();
                foreach (var node in available)
                {
                    var clone = Clone(hero);
                    if (SkillTreeService.TryLearn(clone, node.Id) == CharacterProgression.LearnSkillResult.Success)
                        candidates.Add((node.Id, clone));
                }
                var (scores, finalists) = await ScoreCandidatesAsync(candidates).ConfigureAwait(false);
                string? bestId = null;
                double best = double.MinValue;
                foreach (string id in finalists)
                {
                    if (scores[id] > best)
                    {
                        best = scores[id];
                        bestId = id;
                    }
                }

                if (bestId == null || SkillTreeService.TryLearn(hero, bestId) != CharacterProgression.LearnSkillResult.Success)
                    return;
                AddChoice(SkillDecisions, hero, bestId, scores);
                _milestone?.SkillsLearned.Add(bestId);
                SkillsLearned++;
                _trace?.Invoke($"    smart: learned skill {bestId}");
                if (System.Environment.GetEnvironmentVariable("DF_SKILLTRACE") == "1")
                    Console.Error.WriteLine($"[skilltrace] L{hero.Level} {bestId}");
            }
            LearnFreeNodes(hero);
        }

        /// <summary>An extra rank on a learned Core trunk node while its tree's trunk tip is still unlearned.</summary>
        private static bool IsTrunkRankUpBeforeTip(Character hero, SkillTreeNodeDefinition node)
        {
            if (node.Tier <= 0 || hero.Progression.GetSkillRank(node.Id) < 1
                || !string.Equals(node.Branch, "Core", StringComparison.OrdinalIgnoreCase))
                return false;
            var tree = SkillTreePrerequisites.FindOwnerTree(node);
            var tip = tree != null ? SkillTreePrerequisites.GetTrunkTip(tree) : null;
            return tip != null && tip.Tier > 0 && hero.Progression.GetSkillRank(tip.Id) < 1;
        }

        /// <summary>Free nodes (cost 0) cost nothing, so take every available one before weighing paid picks.</summary>
        private void LearnFreeNodes(Character hero)
        {
            for (int guard = 0; guard < 20; guard++)
            {
                var free = SkillTreeService.BuildDisplayModel(hero.Progression).AllNodes
                    .FirstOrDefault(n => n.Cost == 0 && n.Tier > 0
                        && SkillTreeService.GetNodeState(hero.Progression, n) == SkillTreeService.NodeViewState.Available);
                if (free == null || SkillTreeService.TryLearn(hero, free.Id) != CharacterProgression.LearnSkillResult.Success)
                    return;
                SkillsLearned++;
                _trace?.Invoke($"    smart: learned free skill {free.Id}");
                if (System.Environment.GetEnvironmentVariable("DF_SKILLTRACE") == "1")
                    Console.Error.WriteLine($"[skilltrace] L{hero.Level} {free.Id}");
            }
        }

        // ---------------------------------------------------------------- gear

        private static WeaponType CurrentWeaponType(Character hero, WeaponType fallback) =>
            hero.Weapon is WeaponItem w ? w.WeaponType : fallback;

        private async Task OptimizeGearAsync(Character hero, string heroClass)
        {
            for (int pass = 0; pass < 5; pass++)
            {
                double current = await ScoreAsync(Clone(hero), StripOf(hero)).ConfigureAwait(false);
                if (pass == 0 && _milestone != null)
                    _milestone.ScoreAfterSkills = Math.Round(current, 4);
                var candidates = GearCandidates(hero);
                var tried = new List<(string Key, Character Clone)>();
                var moves = new Dictionary<string, (List<int> Indices, double Threshold)>(StringComparer.OrdinalIgnoreCase);
                foreach (int index in candidates)
                {
                    var clone = Clone(hero);
                    if (index >= clone.Inventory.Count)
                        continue;
                    var item = clone.Inventory[index];
                    if (!TryEquip(clone, item))
                        continue;
                    string key = $"{PlaythroughGearPolicy.SlotOf(item)}: {item.Name} #{index}";
                    tried.Add((key, clone));
                    // A piece that grows a class set already worn counts as a class-set move.
                    bool growsClassSet = IsClassMaterial(item.Material, heroClass) && MaterialSetController.CountEquipped(hero, item.Material) > 0;
                    moves[key] = (new List<int> { index }, growsClassSet ? ClassSetThreshold : ImprovementThreshold);
                }
                // Whole-set packages: one piece at a time never crosses the gap where the first piece breaks the
                // worn set before the new one reaches its stack-2 unlock, so sets were never assembled.
                foreach (var (material, indices) in SetPackages(hero))
                {
                    var clone = Clone(hero);
                    if (!TryEquipAll(clone, indices))
                        continue;
                    PutConvertInStrip(clone, material);
                    string key = $"set: {material} x{MaterialSetController.CountEquipped(clone, material)}";
                    tried.Add((key, clone));
                    moves[key] = (indices, IsClassMaterial(material, heroClass) ? ClassSetThreshold : ImprovementThreshold);
                }
                var (candidateScores, finalists) = await ScoreCandidatesAsync(tried).ConfigureAwait(false);
                var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["keep"] = current };
                foreach (var (key, score) in candidateScores)
                    scores[key] = score;
                // Each move is judged against its own adoption bar (score / threshold), so a class set wins near-ties.
                string? bestKey = null;
                double bestRatio = 1.0;
                foreach (string key in finalists)
                {
                    // A setup that loses every eval fight scores 0: then any positive score is adopted, best first.
                    double ratio = current > 0 ? candidateScores[key] / (current * moves[key].Threshold) : 1.0 + candidateScores[key];
                    if (candidateScores[key] > 0 && ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestKey = key;
                    }
                }

                if (scores.Count > 1)
                    AddChoice(GearDecisions, hero, bestKey ?? "keep", scores);
                if (bestKey == null)
                    return;
                var chosenItems = moves[bestKey].Indices.Where(i => i < hero.Inventory.Count).Select(i => hero.Inventory[i]).ToList();
                if (!TryEquipAll(hero, moves[bestKey].Indices))
                    return;
                if (bestKey.StartsWith("set: ", StringComparison.Ordinal))
                    PutConvertInStrip(hero, chosenItems[0].Material);
                foreach (var chosen in chosenItems)
                    _milestone?.GearEquipped.Add(chosen.Name);
                GearChanges++;
                _trace?.Invoke($"    smart: equipped {bestKey} score {current:F3} -> {candidateScores[bestKey]:F3}");
            }
        }

        internal static string ClassKeyOf(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => "BARBARIAN",
            WeaponType.Sword => "WARRIOR",
            WeaponType.Dagger => "ROGUE",
            WeaponType.Wand => "WIZARD",
            _ => ""
        };

        private static bool IsClassMaterial(string? material, string heroClass)
        {
            var build = MaterialBuildsLoader.FindByMaterial(ItemMaterialRules.RemapLegacyMaterial(material));
            return build != null && heroClass.Length > 0 && string.Equals(build.AssociatedClass, heroClass, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Per set material in the bag: the best wearable piece for each slot not already that material, when wearing
        /// them reaches the stack-2 unlock (or grows a set already unlocked by two or more pieces). Weapons only of the
        /// current type, so a package never switches class.
        /// </summary>
        private static List<(string Material, List<int> Indices)> SetPackages(Character hero)
        {
            var packages = new List<(string, List<int>)>();
            var currentType = hero.Weapon is WeaponItem w ? w.WeaponType : (WeaponType?)null;
            var wearable = hero.Inventory
                .Select((item, index) => (item, index, material: ItemMaterialRules.RemapLegacyMaterial(item.Material)))
                .Where(x => PlaythroughGearPolicy.SlotOf(x.item) != "")
                .Where(x => x.item.GetEquipBlockedReason(hero) == null)
                .Where(x => x.item is not WeaponItem wi || wi.WeaponType == currentType)
                .Where(x => !string.IsNullOrWhiteSpace(x.material) && MaterialBuildsLoader.FindByMaterial(x.material) != null)
                .ToList();
            foreach (var group in wearable.GroupBy(x => x.material, StringComparer.OrdinalIgnoreCase))
            {
                var indices = new List<int>();
                foreach (var slot in group.GroupBy(x => PlaythroughGearPolicy.SlotOf(x.item)))
                {
                    var worn = slot.Key switch
                    {
                        "weapon" => hero.Weapon, "head" => hero.Head, "body" => hero.Body, "legs" => hero.Legs, _ => hero.Feet
                    };
                    if (worn != null && string.Equals(ItemMaterialRules.RemapLegacyMaterial(worn.Material), group.Key, StringComparison.OrdinalIgnoreCase))
                        continue;
                    indices.Add(slot.OrderByDescending(x => ItemValueModel.Score(x.item)).First().index);
                }
                int worn2 = MaterialSetController.CountEquipped(hero, group.Key);
                if (indices.Count >= 2 || (indices.Count == 1 && worn2 == 1))
                    packages.Add((group.Key, indices));
            }
            return packages;
        }

        /// <summary>Equips the bag items at <paramref name="indices"/> (resolved before any equip shifts the bag).</summary>
        private static bool TryEquipAll(Character hero, List<int> indices)
        {
            var items = indices.Where(i => i < hero.Inventory.Count).Select(i => hero.Inventory[i]).ToList();
            bool any = false;
            foreach (var item in items)
                any |= TryEquip(hero, item);
            return any;
        }

        /// <summary>Puts the set's convert action at the front of the strip once the set unlocks it (it only reaches the pool).</summary>
        private static void PutConvertInStrip(Character hero, string? material)
        {
            var build = MaterialBuildsLoader.FindByMaterial(ItemMaterialRules.RemapLegacyMaterial(material));
            string? convert = build?.ConvertAction?.Trim();
            if (string.IsNullOrEmpty(convert)
                || !MaterialSetController.GetGrantedConvertActionNames(hero).Contains(convert, StringComparer.OrdinalIgnoreCase))
                return;
            var strip = StripOf(hero);
            if (strip.Contains(convert, StringComparer.OrdinalIgnoreCase))
                return;
            var weaponType = CurrentWeaponType(hero, WeaponType.Mace);
            string? required = WeaponRequiredComboAction.IsRequirementActiveForWeaponPath(hero, weaponType)
                ? WeaponRequiredComboAction.TryGetRequiredBasicActionName(weaponType)
                : null;
            strip.Insert(0, convert);
            int max = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            while (strip.Count > max)
            {
                int drop = strip.FindLastIndex(x => !string.Equals(x, convert, StringComparison.OrdinalIgnoreCase)
                    && (required == null || !string.Equals(x, required, StringComparison.OrdinalIgnoreCase)));
                if (drop < 0)
                    return;
                strip.RemoveAt(drop);
            }
            hero.RestoreComboFromActionNames(strip);
            WeaponRequiredComboAction.EnsureRequiredBasicInCombo(hero);
            hero.ComboStep = 0;
        }

        /// <summary>
        /// Bag indices worth simulating: the top two per slot by static value, plus any item whose material matches
        /// equipped gear (set pieces can be worth more than their raw stats). Weapons of any type count: switching
        /// weapons switches class, which a player may do at any point.
        /// </summary>
        private static List<int> GearCandidates(Character hero)
        {
            var equippedMaterials = new[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet }
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i!.Material))
                .Select(i => i!.Material)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var indexed = hero.Inventory
                .Select((item, index) => (item, index))
                .Where(x => PlaythroughGearPolicy.SlotOf(x.item) != "")
                // Only gear the hero can wear: the top-2-per-slot cut below must not be spent on items equip refuses.
                .Where(x => x.item.GetEquipBlockedReason(hero) == null)
                .ToList();

            var picks = new HashSet<int>();
            foreach (var slot in indexed.GroupBy(x => PlaythroughGearPolicy.SlotOf(x.item)))
                foreach (var x in slot.OrderByDescending(x => ItemValueModel.Score(x.item)).Take(2))
                    picks.Add(x.index);
            foreach (var x in indexed.Where(x => !string.IsNullOrWhiteSpace(x.item.Material) && equippedMaterials.Contains(x.item.Material)))
                picks.Add(x.index);
            return picks.ToList();
        }

        internal static bool TryEquipItem(Character hero, Item item) => TryEquip(hero, item);

        private static bool TryEquip(Character hero, Item item)
        {
            string slot = PlaythroughGearPolicy.SlotOf(item);
            if (slot == "")
                return false;
            try
            {
                var before = slot switch
                {
                    "weapon" => hero.Weapon, "head" => hero.Head, "body" => hero.Body, "legs" => hero.Legs, _ => hero.Feet
                };
                hero.EquipItem(item, slot);
                var after = slot switch
                {
                    "weapon" => hero.Weapon, "head" => hero.Head, "body" => hero.Body, "legs" => hero.Legs, _ => hero.Feet
                };
                return !ReferenceEquals(before, after);
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------------- sequence

        private async Task OptimizeSequenceAsync(Character hero, WeaponType weaponType)
        {
            int max = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            var owned = hero.GetActionPool()
                .Select(a => a.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            // The game re-adds the weapon's required basic, so only consider sequences that keep it.
            string? required = WeaponRequiredComboAction.IsRequirementActiveForWeaponPath(hero, weaponType)
                ? WeaponRequiredComboAction.TryGetRequiredBasicActionName(weaponType)
                : null;
            bool Valid(List<string> v) =>
                v.Count > 0 && v.Count <= max
                && (required == null || v.Contains(required, StringComparer.OrdinalIgnoreCase));

            var strip = StripOf(hero);
            double current = await ScoreAsync(Clone(hero), strip).ConfigureAwait(false);
            // Best score of any evaluated strip holding each action: how close an action came to making the cut.
            var bestWith = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            void Note(IEnumerable<string> v, double score)
            {
                foreach (string a in v)
                    bestWith[a] = Math.Max(bestWith.GetValueOrDefault(a), score);
            }
            Note(strip, current);

            // Screen every unused action once (front of the sequence), keep the most promising few.
            var screened = new List<(string action, double score)>();
            foreach (string a in owned.Where(n => !strip.Contains(n, StringComparer.OrdinalIgnoreCase)))
            {
                var v = new List<string>(strip);
                v.Insert(0, a);
                if (v.Count > max)
                    v.RemoveAt(v.FindLastIndex(x => required == null || !string.Equals(x, required, StringComparison.OrdinalIgnoreCase)));
                if (!Valid(v))
                    continue;
                double screenScore = await ScoreAsync(Clone(hero), v,
                    ScreenFightsPerEnemy > 0 && ScreenFightsPerEnemy < FightsPerEnemy ? ScreenFightsPerEnemy : 0).ConfigureAwait(false);
                screened.Add((a, screenScore));
                Note(v, screenScore);
            }
            var shortlist = screened.OrderByDescending(x => x.score).Take(ShortlistSize).Select(x => x.action).ToList();

            for (int iteration = 0; iteration < 4; iteration++)
            {
                var variants = new List<List<string>>();
                foreach (string a in shortlist.Where(n => !strip.Contains(n, StringComparer.OrdinalIgnoreCase)))
                {
                    if (strip.Count < max)
                    {
                        variants.Add(new List<string>(strip) { a });
                        var front = new List<string>(strip);
                        front.Insert(0, a);
                        variants.Add(front);
                    }
                    for (int slot = 0; slot < strip.Count; slot++)
                        variants.Add(new List<string>(strip) { [slot] = a });
                }
                for (int slot = 1; slot < strip.Count; slot++)
                {
                    var swapped = new List<string>(strip);
                    (swapped[slot - 1], swapped[slot]) = (swapped[slot], swapped[slot - 1]);
                    variants.Add(swapped);
                }

                List<string>? bestVariant = null;
                double best = current * ImprovementThreshold;
                foreach (var v in variants.Where(Valid))
                {
                    double score = await ScoreAsync(Clone(hero), v).ConfigureAwait(false);
                    Note(v, score);
                    if (score > best)
                    {
                        best = score;
                        bestVariant = v;
                    }
                }

                if (bestVariant == null)
                    break;
                strip = bestVariant;
                current = best;
            }

            if (!strip.SequenceEqual(StripOf(hero), StringComparer.OrdinalIgnoreCase))
            {
                hero.RestoreComboFromActionNames(strip);
                WeaponRequiredComboAction.EnsureRequiredBasicInCombo(hero);
                hero.ComboStep = 0;
                SequenceChanges++;
                _trace?.Invoke($"    smart: sequence {string.Join(" > ", StripOf(hero))} (score {current:F3})");
            }

            if (_milestone != null)
                _milestone.ScoreFinal = Math.Round(current, 4);
            double top = bestWith.Count > 0 ? bestWith.Values.Max() : 0;
            if (top > 0)
                SequenceDecisions.Add(new PlaythroughSequenceDecision
                {
                    HeroLevel = hero.Level,
                    SlotMax = max,
                    Required = required ?? "",
                    Strip = StripOf(hero),
                    ScoreVsBest = owned.ToDictionary(a => a, a => Math.Round(bestWith.GetValueOrDefault(a) / top, 3), StringComparer.OrdinalIgnoreCase)
                });
        }

        private static void AddChoice(List<PlaythroughChoiceDecision> list, Character hero, string chosen, Dictionary<string, double> scores)
        {
            double top = scores.Values.DefaultIfEmpty(0).Max();
            if (top <= 0)
                return;
            list.Add(new PlaythroughChoiceDecision
            {
                HeroLevel = hero.Level,
                Chosen = chosen,
                ScoreVsBest = scores.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value / top, 3), StringComparer.OrdinalIgnoreCase)
            });
        }

        /// <summary>Unused actions kept after screening for the full sequence search.</summary>
        public static int ShortlistSize { get; set; } = 6;

        // ---------------------------------------------------------------- scoring

        /// <summary>
        /// Scores every candidate (key → score). Above <see cref="ScreenKeep"/> candidates, a quick screen at
        /// <see cref="ScreenFightsPerEnemy"/> fights per enemy picks the finalists, which are re-scored in full; screened-out
        /// candidates keep their screen score (decision records) but are not finalists, so they can't be picked.
        /// Finalists keep the candidates' order, so ties break as before.
        /// </summary>
        private async Task<(Dictionary<string, double> Scores, List<string> Finalists)> ScoreCandidatesAsync(
            List<(string Key, Character Clone)> candidates)
        {
            var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var finalists = candidates.Select(c => c.Key).ToList();
            if (ScreenFightsPerEnemy > 0 && ScreenFightsPerEnemy < FightsPerEnemy && candidates.Count > ScreenKeep)
            {
                foreach (var (key, clone) in candidates)
                    scores[key] = await ScoreAsync(clone, StripOf(clone), ScreenFightsPerEnemy).ConfigureAwait(false);
                var keep = candidates.Select(c => c.Key).OrderByDescending(k => scores[k]).Take(ScreenKeep)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                finalists = finalists.Where(keep.Contains).ToList();
            }
            foreach (var (key, clone) in candidates.Where(c => finalists.Contains(c.Key, StringComparer.OrdinalIgnoreCase)))
                scores[key] = await ScoreAsync(clone, StripOf(clone)).ConfigureAwait(false);
            return (scores, finalists);
        }

        /// <summary>
        /// Short seeded fights at the hero's level with real combo routing. Score = win rate × kill speed × HP kept:
        /// winRate / (heroTurns × (netHpLostPerFight / maxHp + 0.05)); a lost fight counts <see cref="LostFightTurns"/> turns.
        /// </summary>
        private async Task<double> ScoreAsync(Character candidate, IReadOnlyList<string> strip, int fightsPerEnemy = 0)
        {
            int perEnemy = fightsPerEnemy > 0 ? fightsPerEnemy : FightsPerEnemy;
            ProfSerialize.Start();
            string json = new CharacterSerializer().Serialize(candidate);
            ProfSerialize.Stop();
            string cacheKey = $"{perEnemy}|{string.Join(",", strip)}|{json}";
            if (_scoreCache.TryGetValue(cacheKey, out double cached))
                return cached;
            Evaluations++;
            ProfEvaluations++;
            double wins = 0, turns = 0, hpLost = 0;
            int fights = 0;
            ActionLabEncounterSimulator.UseNaturalComboRouting = true;
            try
            {
                foreach (string enemy in EvalEnemies)
                {
                    var snapshot = new LabCombatSnapshot(
                        json, 0, 0, 0, 0, 0, 0, 0,
                        sessionEnemyLoaderType: enemy,
                        enemyLevel: Math.Clamp(candidate.Level, 1, 100),
                        comboStripActionNames: strip,
                        selectedCatalogActionName: strip.Count > 0 ? strip[0] : "",
                        labEnemyBattleConfig: null);
                    if (ActionLabEncounterSimulator.ValidateSnapshot(snapshot) != null)
                        continue;
                    ProfSim.Start();
                    var report = await ActionLabEncounterSimulator.RunBatchAsync(snapshot, perEnemy, new Random(17), maxDegreeOfParallelism: 1)
                        .ConfigureAwait(false);
                    ProfSim.Stop();
                    foreach (var e in report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)))
                    {
                        fights++;
                        wins += e.PlayerWon ? 1 : 0;
                        // A loss counts as the slowest fight: a quick death must not read as a quick kill.
                        turns += e.PlayerWon ? Math.Max(1, e.PlayerTurns) : LostFightTurns;
                        hpLost += e.PlayerMaxHealth > 0 ? Math.Max(0, e.PlayerMaxHealth - e.PlayerFinalHealth) / (double)e.PlayerMaxHealth : 1;
                    }
                }
            }
            finally
            {
                ActionLabEncounterSimulator.UseNaturalComboRouting = false;
            }

            double result = fights == 0 ? 0 : wins / fights / ((turns / fights) * (hpLost / fights + 0.05));
            _scoreCache[cacheKey] = result;
            return result;
        }

        private static Character Clone(Character hero)
        {
            ProfClone.Start();
            try { return LabCombatEntityFactory.ClonePlayerFromJson(new CharacterSerializer().Serialize(hero), "Smart policy: clone failed."); }
            finally { ProfClone.Stop(); }
        }

        private static List<string> StripOf(Character hero) => hero.GetComboActions().Select(a => a.Name).ToList();

        private static string Signature(Character hero, WeaponType weaponType) =>
            $"{hero.Level}|{hero.Inventory.Count}|{string.Join(",", hero.Inventory.Select(i => i.Name))}|{hero.GetActionPool().Count}|{ComboSequenceMaxHelper.GetEffectiveMax(hero)}|{hero.Progression.GetAvailableSkillPoints(weaponType)}";
    }
}
