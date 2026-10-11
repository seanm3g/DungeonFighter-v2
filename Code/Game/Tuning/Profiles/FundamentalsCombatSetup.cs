using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Config;
using RPGGame.Data;
using RPGGame.Entity.Services;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Builds a no-gear (weapon-only) lab snapshot for fundamentals combat sims.
    /// </summary>
    public static class FundamentalsCombatSetup
    {
        /// <summary>Default loader enemy for fundamentals sims (uses global 1× health scaling via <see cref="EnemyLoader"/>).</summary>
        public const string DefaultFundamentalsEnemyType = "Goblin";

        /// <summary>Hero stat multiplier for fundamentals sims (1.0 = normal level stats).</summary>
        public const double FundamentalsHeroPowerScale = 1.0;

        /// <summary>Same as <see cref="BuildSnapshot(SimulationProfileConfig)"/> with an explicit combo strip (action names in slot order).</summary>
        public static LabCombatSnapshot BuildSnapshot(SimulationProfileConfig config, IReadOnlyList<string> comboStrip)
        {
            var baseSnap = BuildSnapshot(config);
            return new LabCombatSnapshot(
                // Open enough strip slots for the tested sequence (level 1 heroes start with one slot).
                baseSnap.InitialPlayerJson, 0, 0, 0, 0, 0, 0, Math.Max(0, comboStrip.Count - 1),
                sessionEnemyLoaderType: baseSnap.SessionEnemyLoaderType,
                enemyLevel: baseSnap.EnemyLevel,
                comboStripActionNames: comboStrip,
                selectedCatalogActionName: comboStrip.Count > 0 ? comboStrip[0] : baseSnap.SelectedCatalogActionName,
                labEnemyBattleConfig: null);
        }

        public static LabCombatSnapshot BuildSnapshot(SimulationProfileConfig config)
        {
            long prof = RPGGame.Diagnostics.ToolProfiler.Start();
            try { return BuildSnapshotCore(config); }
            finally { RPGGame.Diagnostics.ToolProfiler.Stop("Setup.BuildSnapshot", prof); }
        }

        private static LabCombatSnapshot BuildSnapshotCore(SimulationProfileConfig config)
        {
            ActionLoader.ReloadActions();
            EnemyLoader.LoadEnemies();

            int level = Math.Clamp(config.PlayerLevel, 1, 100);
            var character = new Character("FundamentalsHero", level);
            StripNonWeaponGear(character);
            ApplyFundamentalsHeroPowerScale(character);

            var weaponType = ParseWeaponType(config.WeaponType);
            var weapon = CreateBaselineWeapon(weaponType);
            character.EquipItem(weapon, "weapon");
            if (string.Equals(config.Gear, "typical", StringComparison.OrdinalIgnoreCase))
                EquipTypicalGear(character, weaponType, level);
            // STATVALUE: synthetic stat lines on the weapon to price one stat in isolation. The typical-gear weapon is a
            // shared cached item, so the lines are removed again once the hero is serialized (else every later row inherits them).
            var syntheticStatLines = new List<StatBonus>();
            if (ExtraWeaponStatBonuses.Count > 0 && character.Weapon is WeaponItem statWeapon)
            {
                foreach (var extra in ExtraWeaponStatBonuses)
                {
                    var line = extra.CloneForItemInstance();
                    statWeapon.StatBonuses.Add(line);
                    syntheticStatLines.Add(line);
                }
            }
            character.InitializeDefaultCombo();

            var strip = character.GetComboActions().Select(a => a.Name).ToList();
            if (strip.Count == 0)
                throw new InvalidOperationException("Fundamentals hero has no combo actions after InitializeDefaultCombo.");

            string forced = config.ForcedCatalogAction?.Trim() ?? strip[0];
            if (ActionLoader.GetAction(forced) == null)
                throw new InvalidOperationException($"Unknown forced catalog action '{forced}'.");

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(character);
            if (character.Weapon is WeaponItem syntheticWeapon)
                foreach (var line in syntheticStatLines)
                    syntheticWeapon.StatBonuses.Remove(line);

            string enemyType = ResolveEnemyType(config);

            return new LabCombatSnapshot(
                json,
                labPanelStrDelta: 0,
                labPanelAgiDelta: 0,
                labPanelTecDelta: 0,
                labPanelIntDelta: 0,
                labPanelLevelDelta: 0,
                labPanelArmorDelta: 0,
                labPanelActionSlotDelta: 0,
                sessionEnemyLoaderType: enemyType,
                enemyLevel: Math.Clamp(config.EnemyLevel, 1, 100),
                comboStripActionNames: strip,
                selectedCatalogActionName: forced,
                labEnemyBattleConfig: null);
        }

        private static string ResolveEnemyType(SimulationProfileConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.EnemyType))
            {
                string requested = config.EnemyType.Trim();
                if (EnemyLoader.CreateEnemy(requested, level: 1) != null)
                    return requested;
                throw new InvalidOperationException($"Unknown fundamentals enemy type '{requested}'.");
            }

            var types = EnemyLoader.GetAllEnemyTypes();
            string? match = types.FirstOrDefault(t =>
                string.Equals(t, DefaultFundamentalsEnemyType, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;

            match = types.FirstOrDefault();
            if (match != null)
                return match;

            throw new InvalidOperationException("No enemies loaded for fundamentals simulation.");
        }

        /// <summary>Stat lines added to the fundamentals hero's weapon (STATVALUE).</summary>
        public static List<StatBonus> ExtraWeaponStatBonuses { get; } = new();

        private static readonly Dictionary<(WeaponType, int), List<Item>> TypicalGearCache = new();

        /// <summary>
        /// Equips the median real loot item for <paramref name="level"/> in every slot (weapon of the hero's type by damage/speed,
        /// armor by armor value). Level 1 keeps the starter weapon, as a new hero would.
        /// </summary>
        public static void EquipTypicalGear(Character character, WeaponType weaponType, int level)
        {
            if (!TypicalGearCache.TryGetValue((weaponType, level), out var picks))
            {
                var pool = GetTypicalGearPool(level);
                picks = new List<Item>();
                if (level > 1 && pool.Weapons.TryGetValue(weaponType, out var weapons) && weapons.Count > 0)
                    picks.Add(weapons.OrderBy(w => w.GetTotalDamage() / Math.Max(0.1, w.GetTotalAttackSpeed())).ElementAt(weapons.Count / 2));
                foreach (var list in pool.Armor.Values.Where(l => l.Count > 0))
                    picks.Add(list.OrderBy(ArmorValue).ElementAt(list.Count / 2));
                TypicalGearCache[(weaponType, level)] = picks;
            }

            foreach (var item in picks)
            {
                item.AttributeRequirements = new AttributeRequirements();
                string slot = item switch
                {
                    WeaponItem => "weapon",
                    HeadItem => "head",
                    ChestItem => "body",
                    LegsItem => "legs",
                    FeetItem => "feet",
                    _ => ""
                };
                if (slot.Length > 0)
                {
                    try { character.EquipItem(item, slot); }
                    catch { /* incompatible slot: skip */ }
                }
            }
        }

        private sealed class TypicalGearPool
        {
            public Dictionary<WeaponType, List<WeaponItem>> Weapons { get; } = new();
            public Dictionary<ItemType, List<Item>> Armor { get; } = new();
        }

        private static readonly Dictionary<int, TypicalGearPool> TypicalGearPools = new();

        /// <summary>
        /// One seeded loot sample per level, shared by every weapon type: rolls until each weapon type and each armor slot
        /// has <see cref="TypicalGearSamples"/> items (or a roll cap), so the medians are stable and cheap to compute.
        /// </summary>
        private static TypicalGearPool GetTypicalGearPool(int level)
        {
            if (TypicalGearPools.TryGetValue(level, out var cached))
                return cached;

            long profPool = RPGGame.Diagnostics.ToolProfiler.Start();
            var pool = new TypicalGearPool();
            LootGenerator.ReseedForTools(1000 * level);
            using var gearScope = RandomUtility.BeginSeededScope(new Random(7919 * level));
            var armorSlots = new[] { ItemType.Head, ItemType.Chest, ItemType.Legs, ItemType.Feet };
            bool Full() =>
                Enum.GetValues<WeaponType>().All(t => pool.Weapons.TryGetValue(t, out var l) && l.Count >= TypicalGearSamples)
                && armorSlots.All(t => pool.Armor.TryGetValue(t, out var l) && l.Count >= TypicalGearSamples);

            for (int i = 0; i < 4000 && !Full(); i++)
            {
                var item = LootGenerator.GenerateLoot(level, level, null, guaranteedLoot: true);
                if (item is WeaponItem w)
                {
                    if (!pool.Weapons.TryGetValue(w.WeaponType, out var wl))
                        pool.Weapons[w.WeaponType] = wl = new List<WeaponItem>();
                    if (wl.Count < TypicalGearSamples)
                        wl.Add(w);
                }
                else if (item != null && ArmorValue(item) >= 0)
                {
                    if (!pool.Armor.TryGetValue(item.Type, out var al))
                        pool.Armor[item.Type] = al = new List<Item>();
                    if (al.Count < TypicalGearSamples)
                        al.Add(item);
                }
            }

            TypicalGearPools[level] = pool;
            RPGGame.Diagnostics.ToolProfiler.Stop("Setup.TypicalGearPool", profPool);
            return pool;
        }

        private const int TypicalGearSamples = 101;

        private static int ArmorValue(Item item) => item switch
        {
            HeadItem h => h.GetTotalArmor(),
            ChestItem c => c.GetTotalArmor(),
            LegsItem l => l.GetTotalArmor(),
            FeetItem f => f.GetTotalArmor(),
            _ => -1
        };

        private static void ApplyFundamentalsHeroPowerScale(Character character)
        {
            if (Math.Abs(FundamentalsHeroPowerScale - 1.0) < 0.0001)
                return;

            double scale = FundamentalsHeroPowerScale;
            character.Stats.Strength = Math.Max(1, (int)(character.Stats.Strength * scale));
            character.Stats.Agility = Math.Max(1, (int)(character.Stats.Agility * scale));
            character.Stats.Technique = Math.Max(1, (int)(character.Stats.Technique * scale));
            character.Stats.Intelligence = Math.Max(1, (int)(character.Stats.Intelligence * scale));
        }

        private static void StripNonWeaponGear(Character character)
        {
            foreach (string slot in new[] { "weapon", "head", "body", "legs", "feet", "charm" })
            {
                try { character.UnequipItem(slot); }
                catch { /* slot may already be empty */ }
            }
        }

        private static WeaponType ParseWeaponType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return WeaponType.Sword;
            return Enum.TryParse<WeaponType>(value.Trim(), true, out var wt) ? wt : WeaponType.Sword;
        }

        private static WeaponItem CreateBaselineWeapon(WeaponType weaponType)
        {
            int baseDamage = 1;
            var scaling = GameConfiguration.Instance.WeaponScaling?.StartingWeaponDamage;
            if (scaling != null)
            {
                baseDamage = weaponType switch
                {
                    WeaponType.Mace => scaling.Mace,
                    WeaponType.Sword => scaling.Sword,
                    WeaponType.Dagger => scaling.Dagger,
                    WeaponType.Wand => scaling.Wand,
                    _ => baseDamage
                };
            }

            // Use the starter weapon's real attack speed so weapon speed differences show up in fundamentals sims.
            var starterRow = StarterCatalogItems.LoadStarterTaggedWeaponRows()
                .FirstOrDefault(r => string.Equals(r.Type?.Trim(), weaponType.ToString(), StringComparison.OrdinalIgnoreCase));
            double attackSpeed = starterRow != null && starterRow.AttackSpeed > 0 ? starterRow.AttackSpeed : 1.0;

            return new WeaponItem(
                name: starterRow?.Name ?? $"Fundamentals {weaponType}",
                tier: 1,
                baseDamage: Math.Max(1, baseDamage),
                baseAttackSpeed: attackSpeed,
                weaponType: weaponType);
        }
    }
}
