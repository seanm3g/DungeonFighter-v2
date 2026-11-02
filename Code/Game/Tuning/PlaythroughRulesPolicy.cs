using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Data;
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
        }

        private static RulesData? _data;
        private readonly Action<string>? _trace;
        private string _lastSignature = "";

        public int SkillsLearned { get; private set; }
        public int GearChanges { get; private set; }
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
            if (signature == _lastSignature)
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
                LearnSkills(hero, homeType, cls, milestone);
                EquipGear(hero, homeType, milestone);
                BuildStrip(hero, homeType, StripKey(cls, hero.Level));
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
                    .Where(n => n.Tier > 0 && SkillTreeService.GetNodeState(hero.Progression, n) == SkillTreeService.NodeViewState.Available)
                    .ToList();
                var free = available.FirstOrDefault(n => n.Cost == 0);
                SkillTreeNodeDefinition? pick = free;
                if (pick == null && hero.Progression.GetAvailableSkillPoints(homeType) > 0)
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
                    bool isHome = string.Equals(MaterialBuildsLoader.FindByMaterial(g.Key)?.AssociatedClass, home, StringComparison.OrdinalIgnoreCase);
                    return (material: g.Key, pieces, isHome);
                })
                .Where(s => s.pieces.Count >= MaterialBuildData.StackUnlockCount)
                .OrderByDescending(s => s.isHome)
                .ThenByDescending(s => s.pieces.Count)
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
            foreach (string a in Data.ActionPriority.GetValueOrDefault(cls) ?? new List<string>())
                if (picks.Count < length && owned.Contains(a) && !picks.Contains(a, StringComparer.OrdinalIgnoreCase))
                    picks.Add(a);
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
            _trace?.Invoke($"    rules: sequence {string.Join(" > ", StripOf(hero))}");
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
