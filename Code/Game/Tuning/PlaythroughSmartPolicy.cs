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

        private string _lastSignature = "";
        private readonly Action<string>? _trace;

        public int Evaluations { get; private set; }

        /// <summary>DF_SMARTPROF=1: time split of smart decisions (whole process), printed by the batch runner.</summary>
        public static readonly System.Diagnostics.Stopwatch ProfSerialize = new(), ProfSim = new(), ProfClone = new(), ProfOptimize = new();
        public static long ProfEvaluations;
        public int SkillsLearned { get; private set; }
        public int GearChanges { get; private set; }
        public int SequenceChanges { get; private set; }

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
            }

            _lastSignature = Signature(hero, weaponType);
            if (System.Environment.GetEnvironmentVariable("DF_MEMTRACE") == "1")
                Console.Error.WriteLine($"[memtrace] L{hero.Level} evals {Evaluations} heap {GC.GetTotalMemory(true) / (1024 * 1024)} MB");
        }

        // ---------------------------------------------------------------- skills

        private async Task SpendSkillPointsAsync(Character hero, WeaponType weaponType)
        {
            for (int guard = 0; guard < 20 && hero.Progression.GetAvailableSkillPoints(weaponType) > 0; guard++)
            {
                var available = SkillTreeService.BuildDisplayModel(hero.Progression).AllNodes
                    .Where(n => SkillTreeService.GetNodeState(hero.Progression, n) == SkillTreeService.NodeViewState.Available)
                    .ToList();
                if (available.Count == 0)
                    return;

                string? bestId = null;
                double best = double.MinValue;
                foreach (var node in available)
                {
                    var clone = Clone(hero);
                    if (SkillTreeService.TryLearn(clone, node.Id) != CharacterProgression.LearnSkillResult.Success)
                        continue;
                    double score = await ScoreAsync(clone, StripOf(clone)).ConfigureAwait(false);
                    if (score > best)
                    {
                        best = score;
                        bestId = node.Id;
                    }
                }

                if (bestId == null || SkillTreeService.TryLearn(hero, bestId) != CharacterProgression.LearnSkillResult.Success)
                    return;
                SkillsLearned++;
                _trace?.Invoke($"    smart: learned skill {bestId}");
            }
        }

        // ---------------------------------------------------------------- gear

        private async Task OptimizeGearAsync(Character hero, WeaponType weaponType)
        {
            for (int pass = 0; pass < 5; pass++)
            {
                double current = await ScoreAsync(Clone(hero), StripOf(hero)).ConfigureAwait(false);
                var candidates = GearCandidates(hero, weaponType);
                int bestIndex = -1;
                double best = current * ImprovementThreshold;
                foreach (int index in candidates)
                {
                    var clone = Clone(hero);
                    if (index >= clone.Inventory.Count)
                        continue;
                    var item = clone.Inventory[index];
                    if (!TryEquip(clone, item))
                        continue;
                    double score = await ScoreAsync(clone, StripOf(clone)).ConfigureAwait(false);
                    if (score > best)
                    {
                        best = score;
                        bestIndex = index;
                    }
                }

                if (bestIndex < 0)
                    return;
                var chosen = hero.Inventory[bestIndex];
                if (!TryEquip(hero, chosen))
                    return;
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
                screened.Add((a, await ScoreAsync(Clone(hero), v).ConfigureAwait(false)));
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
        }

        /// <summary>Unused actions kept after screening for the full sequence search.</summary>
        public static int ShortlistSize { get; set; } = 6;

        // ---------------------------------------------------------------- scoring

        /// <summary>
        /// Short seeded fights at the hero's level with real combo routing. Score = win rate × kill speed × HP kept:
        /// winRate / (heroTurns × (netHpLostPerFight / maxHp + 0.05)).
        /// </summary>
        private async Task<double> ScoreAsync(Character candidate, IReadOnlyList<string> strip)
        {
            Evaluations++;
            ProfEvaluations++;
            ProfSerialize.Start();
            string json = new CharacterSerializer().Serialize(candidate);
            ProfSerialize.Stop();
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
                    var report = await ActionLabEncounterSimulator.RunBatchAsync(snapshot, FightsPerEnemy, new Random(17), maxDegreeOfParallelism: 1)
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

            if (fights == 0)
                return 0;
            double winRate = wins / fights;
            return winRate / ((turns / fights) * (hpLost / fights + 0.05));
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
