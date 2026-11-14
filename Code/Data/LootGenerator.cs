using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;
using RPGGame.Data.ItemGeneration;

namespace RPGGame
{
    /// <summary>
    /// Loot entry point. Every item (dungeon rewards, travel, new-game armor, trade-ups, the Item Generation lab) is rolled
    /// by <see cref="ItemRoller"/>; see Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md.
    /// </summary>
    public class LootGenerator
    {
        private static Random _random = RandomUtility.Ambient;
        private static LootDataCache? _dataCache;

        /// <summary>
        /// Tools only: item seeds come from a seeded generator so loot sampling is reproducible (e.g. fundamentals
        /// "typical gear"). Each item's stage streams derive from its seed.
        /// </summary>
        public static void ReseedForTools(int seed) => _random = new Random(seed);

        private static LootDataCache DataCache => _dataCache ??= LootDataCache.Load();

        /// <summary>Reloads loot data (catalogs, rarity table, ItemMods.json).</summary>
        public static void Initialize()
        {
            _dataCache = null;
            _ = DataCache;
        }

        /// <summary>Rolls one item for <paramref name="request"/> from the shared loot data.</summary>
        public static Item? Roll(ItemRollRequest request) => new ItemRoller(DataCache).Roll(request, _random);

        /// <summary>Rolls one item and returns every decision the roller made.</summary>
        public static Item? Roll(ItemRollRequest request, out ItemRollSpec spec) =>
            new ItemRoller(DataCache).Roll(request, _random, out spec);

        /// <summary>
        /// One item for a hero at <paramref name="playerLevel"/> in a dungeon of <paramref name="dungeonLevel"/>
        /// (item level = dungeon level − (hero level − dungeon level), clamped 1–100). Always returns an item when the
        /// catalogs are loaded: drops happen at dungeon end, not per kill.
        /// </summary>
        public static Item? GenerateLoot(
            int playerLevel,
            int dungeonLevel,
            Character? player = null,
            string? dungeonTheme = null) =>
            Roll(new ItemRollRequest
            {
                HeroLevel = playerLevel,
                DungeonLevel = dungeonLevel,
                Hero = player,
                DungeonTheme = dungeonTheme
            });

        /// <summary>
        /// Trade-up result: an item of exactly <paramref name="rarity"/> at <paramref name="tier"/> and item level
        /// <paramref name="itemLevel"/> (weapon or armor). Lines still roll, but item level and rarity do not roll up.
        /// </summary>
        public static Item? GenerateTradeUp(int tier, int itemLevel, bool weapon, string rarity, Character? hero) =>
            Roll(new ItemRollRequest
            {
                HeroLevel = hero?.Level ?? itemLevel,
                DungeonLevel = itemLevel,
                ItemLevel = Math.Clamp(itemLevel, 1, 100),
                Hero = hero,
                Weapon = weapon,
                Tier = tier,
                Rarity = rarity
            });

        /// <summary>
        /// Rare charm catalog drop (~5%). Independent of armor/weapon affix rolls.
        /// </summary>
        public static CharmItem? TryRollCharmLoot(Random? rng = null)
        {
            if (CharmsLoader.GetAll().Count == 0)
                return null;
            rng ??= RandomUtility.Ambient;
            if (rng.NextDouble() >= 0.05)
                return null;
            return CharmsLoader.CreateRandomItem(rng);
        }

        /// <summary>
        /// One guaranteed piece of armor (never a weapon, never a starter base when anything else fits) for a new
        /// character after they pick a starting weapon. Same pipeline as dungeon loot, at dungeon level 1.
        /// </summary>
        public static Item? GenerateNewGameBonusLoot(Character player)
        {
            if (player == null)
                return null;
            Initialize();
            return Roll(new ItemRollRequest
            {
                HeroLevel = Math.Max(1, player.Level),
                DungeonLevel = 1,
                Hero = player,
                Weapon = false,
                ExcludeStarter = true
            });
        }
    }

    // Data classes for JSON deserialization
    public class TierDistribution
    {
        public int Level { get; set; }
        public double Tier1 { get; set; }
        public double Tier2 { get; set; }
        public double Tier3 { get; set; }
        public double Tier4 { get; set; }
        public double Tier5 { get; set; }
    }

    public class ArmorData
    {
        [JsonPropertyName("slot")]
        public string Slot { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("armor")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Armor { get; set; }
        
        [JsonPropertyName("tier")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Tier { get; set; }

        [JsonPropertyName("attributeRequirements")]
        public Dictionary<string, int>? AttributeRequirements { get; set; }

        /// <summary>Optional multi-value tags from <c>Armor.json</c> / armor sheet (JSON array or omitted).</summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("strength")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Strength { get; set; }

        [JsonPropertyName("agility")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Agility { get; set; }

        [JsonPropertyName("technique")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Technique { get; set; }

        [JsonPropertyName("intelligence")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Intelligence { get; set; }

        [JsonPropertyName("hit")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Hit { get; set; }

        [JsonPropertyName("combo")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Combo { get; set; }

        [JsonPropertyName("crit")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int Crit { get; set; }

        /// <summary>Optional catalog attack-time reduction (seconds) from <c>Armor.json</c>; copied to <see cref="Items.Item.CatalogAttackSpeed"/>.</summary>
        [JsonPropertyName("attackSpeed")]
        public double AttackSpeed { get; set; }

        /// <summary>Catalog combo strip slots when <see cref="ExtraActionSlotsMin"/> / <see cref="ExtraActionSlotsMax"/> are both zero (no range roll).</summary>
        [JsonPropertyName("extraActionSlots")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int ExtraActionSlots { get; set; }

        /// <summary>Inclusive lower bound for a one-time roll onto <see cref="Item.ExtraActionSlots"/> when generated; used when min/max indicate a range (any bound non-zero after normalization).</summary>
        [JsonPropertyName("extraActionSlotsMin")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int ExtraActionSlotsMin { get; set; }

        /// <summary>Inclusive upper bound for <see cref="Item.ExtraActionSlots"/> roll (same rules as <see cref="ExtraActionSlotsMin"/>).</summary>
        [JsonPropertyName("extraActionSlotsMax")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int ExtraActionSlotsMax { get; set; }

        /// <summary>Head: minimum number of granted-action lines after rarity affix rolls.</summary>
        [JsonPropertyName("minActionBonuses")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int MinActionBonuses { get; set; }

        /// <summary>Catalog identity name from <c>Triggers.json</c> / triggers sheet; resolved at generation.</summary>
        [JsonPropertyName("triggerName")]
        public string? TriggerName { get; set; }

        /// <summary>Legacy nested combat procs; prefer <see cref="TriggerName"/>.</summary>
        [JsonPropertyName("triggerBundles")]
        public List<ActionTriggerBundle>? TriggerBundles { get; set; }

        /// <summary>Legacy WHILE_EQUIPPED effects; prefer <see cref="TriggerName"/>.</summary>
        [JsonPropertyName("equipEffects")]
        public List<ActionTriggerBundle>? EquipEffects { get; set; }

        /// <summary>Class the base belongs to (barbarian / warrior / rogue / wizard).</summary>
        [JsonPropertyName("class")]
        public string? Class { get; set; }

        /// <summary>Defense range at item level 1; the roller rolls inside it and scales by item level.</summary>
        [JsonPropertyName("defenseMin")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int DefenseMin { get; set; }

        [JsonPropertyName("defenseMax")]
        [JsonConverter(typeof(JsonInt32NullAsZeroConverter))]
        public int DefenseMax { get; set; }
    }

    public class WeaponData
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";

        /// <summary>Weapon sheet / <c>Weapons.json</c> use camelCase <c>baseDamage</c> (whole-number hit points in combat).</summary>
        [JsonPropertyName("baseDamage")]
        public int BaseDamage { get; set; }

        /// <summary>Weapon sheet / <c>Weapons.json</c> use camelCase <c>attackSpeed</c> (fractional allowed).</summary>
        [JsonPropertyName("attackSpeed")]
        public double AttackSpeed { get; set; }

        /// <summary>Inclusive lower bound for a one-time roll stored on <see cref="WeaponItem.RolledDamageBonus"/> when a weapon is created (loot, starters, lab).</summary>
        [JsonPropertyName("damageBonusMin")]
        public int DamageBonusMin { get; set; }

        /// <summary>Inclusive upper bound for <see cref="WeaponItem.RolledDamageBonus"/> (sheet column often named &quot;Max Bonus&quot;).</summary>
        [JsonPropertyName("damageBonusMax")]
        public int DamageBonusMax { get; set; }

        public int Tier { get; set; }
        
        [JsonPropertyName("attributeRequirements")]
        public Dictionary<string, int>? AttributeRequirements { get; set; }

        /// <summary>Optional multi-value tags from <c>Weapons.json</c> / weapons sheet (JSON array or omitted).</summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>When min/max are both zero, use this fixed value on the weapon instance.</summary>
        [JsonPropertyName("extraActionSlots")]
        public int ExtraActionSlots { get; set; }

        [JsonPropertyName("extraActionSlotsMin")]
        public int ExtraActionSlotsMin { get; set; }

        [JsonPropertyName("extraActionSlotsMax")]
        public int ExtraActionSlotsMax { get; set; }

        /// <summary>Catalog identity name from <c>Triggers.json</c> / triggers sheet; resolved at generation.</summary>
        [JsonPropertyName("triggerName")]
        public string? TriggerName { get; set; }

        /// <summary>Legacy nested combat procs; prefer <see cref="TriggerName"/>.</summary>
        [JsonPropertyName("triggerBundles")]
        public List<ActionTriggerBundle>? TriggerBundles { get; set; }

        /// <summary>Legacy WHILE_EQUIPPED effects; prefer <see cref="TriggerName"/>.</summary>
        [JsonPropertyName("equipEffects")]
        public List<ActionTriggerBundle>? EquipEffects { get; set; }

        /// <summary>Class the base belongs to; when empty, the class tag in <see cref="Tags"/>.</summary>
        [JsonPropertyName("class")]
        public string? Class { get; set; }

        /// <summary>Damage range at item level 1 (catalog damage + bonus range + tier bonus); rolled once, scaled by item level.</summary>
        [JsonPropertyName("damageMin")]
        public int DamageMin { get; set; }

        [JsonPropertyName("damageMax")]
        public int DamageMax { get; set; }
    }

    /// <summary>One row of <c>RarityTable.json</c>.</summary>
    public class RarityData
    {
        public string Name { get; set; } = "";

        /// <summary>Base roll weight (the balance patch's <c>itemAffixByRarity.rarityWeights</c> overrides it).</summary>
        public double Weight { get; set; }

        /// <summary>Lowest item level this rarity can drop at (and be rolled up to).</summary>
        public int MinLevel { get; set; } = 1;

        /// <summary>Line counts per pool (material / stat / mechanic / action): [min, max]. Min lines always roll.</summary>
        public Dictionary<string, int[]> Pools { get; set; } = new();

        /// <summary>Most lines an item of this rarity can hold (exploding extras stop here).</summary>
        public int MaxLines { get; set; }

        /// <summary>Chance of each exploding extra line; every success rolls again.</summary>
        public double Explode { get; set; }

        public (int Min, int Max) PoolRange(string pool)
        {
            // Keys are lower-case pool names (the deserializer drops any comparer set here).
            if (Pools != null && Pools.TryGetValue(pool.ToLowerInvariant(), out var r) && r != null && r.Length > 0)
            {
                int min = Math.Max(0, r[0]);
                int max = r.Length > 1 ? Math.Max(min, r[1]) : min;
                return (min, max);
            }
            return (0, 0);
        }
    }
}
