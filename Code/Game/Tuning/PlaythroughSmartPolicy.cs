using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
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
                await OptimizeGearAsync(hero, weaponType).ConfigureAwait(false);
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

        private async Task OptimizeGearAsync(Character hero, WeaponType weaponType)
        {
            for (int pass = 0; pass < 5; pass++)
            {
                double current = await ScoreAsync(Clone(hero), StripOf(hero)).ConfigureAwait(false);
                if (pass == 0 && _milestone != null)
                    _milestone.ScoreAfterSkills = Math.Round(current, 4);
                var candidates = GearCandidates(hero, weaponType);
                int bestIndex = -1;
                double best = current * ImprovementThreshold;
                var tried = new List<(string Key, Character Clone)>();
                var indexOf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
                    indexOf[key] = index;
                }
                var (candidateScores, finalists) = await ScoreCandidatesAsync(tried).ConfigureAwait(false);
                var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["keep"] = current };
                foreach (var (key, score) in candidateScores)
                    scores[key] = score;
                foreach (string key in finalists)
                {
                    if (candidateScores[key] > best)
                    {
                        best = candidateScores[key];
                        bestIndex = indexOf[key];
                    }
                }

                if (scores.Count > 1)
                    AddChoice(GearDecisions, hero, bestIndex < 0 ? "keep" : $"{PlaythroughGearPolicy.SlotOf(hero.Inventory[bestIndex])}: {hero.Inventory[bestIndex].Name} #{bestIndex}", scores);
                if (bestIndex < 0)
                    return;
                var chosen = hero.Inventory[bestIndex];
                if (!TryEquip(hero, chosen))
                    return;
                _milestone?.GearEquipped.Add(chosen.Name);
                GearChanges++;
                _trace?.Invoke($"    smart: equipped {chosen.Name} ({chosen.Material}) score {current:F3} -> {best:F3}");
            }
        }

        /// <summary>
        /// Bag indices worth simulating: the top two per slot by static value, plus any item whose material matches
        /// equipped gear (set pieces can be worth more than their raw stats).
        /// </summary>
        private static List<int> GearCandidates(Character hero, WeaponType weaponType)
        {
            var equippedMaterials = new[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet }
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i!.Material))
                .Select(i => i!.Material)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var indexed = hero.Inventory
                .Select((item, index) => (item, index))
                .Where(x => PlaythroughGearPolicy.SlotOf(x.item) != "")
                .Where(x => x.item is not WeaponItem w || w.WeaponType == weaponType)
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
        /// winRate / (heroTurns × (netHpLostPerFight / maxHp + 0.05)).
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
                        turns += Math.Max(1, e.PlayerTurns);
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
