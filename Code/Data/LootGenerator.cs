using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Centralized loot generation system
    /// 
    /// This is now a facade coordinating specialized managers:
    /// - LootDataCache: Centralized data loading and caching
    /// - LootTierCalculator: Tier determination logic
    /// - LootItemSelector: Item selection logic
    /// - LootRarityProcessor: Rarity determination and scaling
    /// - LootBonusApplier: Bonus and modification application
    /// </summary>
    public class LootGenerator
    {
        private static Random _random = new Random();
        
        // Shared data cache
        private static LootDataCache? _dataCache;
        
        // Specialized managers
        private static LootTierCalculator? _tierCalculator;
        private static LootItemSelector? _itemSelector;
        private static LootRarityProcessor? _rarityProcessor;
        private static LootBonusApplier? _bonusApplier;

        /// <summary>
        /// Gets or creates the shared data cache
        /// </summary>
        private static LootDataCache DataCache
        {
            get
            {
                if (_dataCache == null)
                {
                    _dataCache = LootDataCache.Load();
                }
                return _dataCache;
            }
        }

        /// <summary>
        /// Gets or creates the tier calculator
        /// </summary>
        private static LootTierCalculator TierCalculator
        {
            get
            {
                if (_tierCalculator == null)
                {
                    _tierCalculator = new LootTierCalculator(DataCache, _random);
                }
                return _tierCalculator;
            }
        }

        /// <summary>
        /// Gets or creates the item selector
        /// </summary>
        private static LootItemSelector ItemSelector
        {
            get
            {
                if (_itemSelector == null)
                {
                    _itemSelector = new LootItemSelector(DataCache, _random);
                }
                return _itemSelector;
            }
        }

        /// <summary>
        /// Gets or creates the rarity processor
        /// </summary>
        private static LootRarityProcessor RarityProcessor
        {
            get
            {
                if (_rarityProcessor == null)
                {
                    _rarityProcessor = new LootRarityProcessor(DataCache, _random);
                }
                return _rarityProcessor;
            }
        }

        /// <summary>
        /// Gets or creates the bonus applier
        /// </summary>
        private static LootBonusApplier BonusApplier
        {
            get
            {
                if (_bonusApplier == null)
                {
                    _bonusApplier = new LootBonusApplier(DataCache, _random);
                }
                return _bonusApplier;
            }
        }

        /// <summary>
        /// Initializes the loot generation system by loading all data
        /// </summary>
        public static void Initialize()
        {
            // Reset cache and managers to force reload
            _dataCache = null;
            _tierCalculator = null;
            _itemSelector = null;
            _rarityProcessor = null;
            _bonusApplier = null;
            
            // Load data (will be created on first access)
            _ = DataCache;
        }

        /// <summary>
        /// Generates a loot item based on player level, dungeon level, and other factors
        /// </summary>
        public static Item? GenerateLoot(
            int playerLevel,
            int dungeonLevel,
            Character? player = null,
            bool guaranteedLoot = false,
            string? dungeonTheme = null,
            string? enemyArchetype = null,
            double bonusMagicFind = 0)
        {
            var tuning = GameConfiguration.Instance;
            
            // Calculate loot chance based on tuning config
            double lootChance = CalculateLootChance(playerLevel, player, guaranteedLoot, tuning, bonusMagicFind);
            
            // Roll for loot chance (skip if guaranteed loot)
            if (!guaranteedLoot)
            {
                double roll = _random.NextDouble();
                if (roll >= lootChance) 
                {
                    return null;
                }
            }

            // ROLL 1: Calculate loot level
            int lootLevel = TierCalculator.CalculateLootLevel(playerLevel, dungeonLevel);
            
            // ROLL 2: 50% weapon, 12.5% each armor slot (head / chest / legs / feet)
            var (isWeapon, armorJsonSlot) = ItemSelector.RollLootCategory();

            // ROLL 3: Item tier based on loot level
            int tier = TierCalculator.RollTier(lootLevel);

            // ROLL 4: Specific item selection
            // For guaranteed loot, retry with different tiers/types if needed
            Item? item = ItemSelector.SelectItem(tier, isWeapon, armorJsonSlot);

            if (item == null && guaranteedLoot)
            {
                // Try all tiers from 1 to 5, and both weapon and armor types
                for (int fallbackTier = 1; fallbackTier <= 5; fallbackTier++)
                {
                    // Try current item type first
                    item = ItemSelector.SelectItem(fallbackTier, isWeapon, armorJsonSlot);
                    if (item != null)
                    {
                        tier = fallbackTier;
                        break;
                    }

                    // Try opposite item type (fresh armor slot if we were weapon)
                    bool oppositeWeapon = !isWeapon;
                    string? slotWhenArmor = oppositeWeapon
                        ? null
                        : (isWeapon ? ItemSelector.RollArmorJsonSlotUniform() : armorJsonSlot);
                    item = ItemSelector.SelectItem(fallbackTier, oppositeWeapon, slotWhenArmor);
                    if (item != null)
                    {
                        tier = fallbackTier;
                        isWeapon = oppositeWeapon;
                        if (!isWeapon)
                            armorJsonSlot = slotWhenArmor;
                        break;
                    }
                }
                
                // If still no item found and guaranteed loot is requested, create a fallback item
                if (item == null)
                {
                    // Use tier 1 as fallback if no items were found in any tier
                    int fallbackTier = tier > 0 ? tier : 1;
                    
                    // Create a basic fallback item to ensure guaranteed loot always generates something
                    if (isWeapon)
                    {
                        item = new WeaponItem("Basic Sword", fallbackTier, 5 + playerLevel, 1.0, WeaponType.Sword);
                    }
                    else
                    {
                        item = new ChestItem("Basic Armor", fallbackTier, 5);
                    }
                    item.Level = Math.Max(1, lootLevel);
                    item.Tier = fallbackTier;
                    item.Rarity = "Common";
                }
            }
            
            if (item == null) 
            {
                return null;
            }

            // ROLL 4.5: Roll item level based on dungeon level
            // 50% chance: same level, 25% chance: +1 level, 25% chance: -1 level
            double levelRoll = _random.NextDouble();
            if (levelRoll < 0.5)
            {
                item.Level = dungeonLevel;
            }
            else if (levelRoll < 0.75)
            {
                item.Level = dungeonLevel + 1;
            }
            else
            {
                item.Level = Math.Max(1, dungeonLevel - 1);
            }

            // ROLL 5: Apply scaling formulas to base stats
            ApplyItemScaling(item, tuning);

            // ROLL 6: Rarity (determines number of bonuses) — MF does not alter base rarity roll
            double lootMf = Math.Clamp(bonusMagicFind, 0, 1);
            var rarity = RarityProcessor.RollRarity(lootMf, playerLevel);
            item.Rarity = rarity.Name?.Trim() ?? "Common";
            RarityProcessor.ApplyRarityScaling(item, rarity);

            // Create context for contextual modifications and actions
            var context = LootContext.Create(player, dungeonTheme, enemyArchetype);

            // Set weapon type on context if this is a weapon
            if (item is WeaponItem weapon)
            {
                context.WeaponType = weapon.WeaponType.ToString();
            }

            // ROLL 7: Bonus selection (MF affects affix-line tiers and optional extra affix counts only)
            int affixMagicFind = Math.Clamp(player?.GetMagicFind() ?? 0, 0, 100);
            BonusApplier.ApplyBonuses(item, rarity, context, affixMagicFind);

            return item;
        }

        private static readonly string[] NewGameBonusArmorJsonSlots = { "head", "chest", "legs", "feet" };

        /// <summary>
        /// One guaranteed piece of armor loot (head, chest, legs, or feet — never a weapon) for a new character
        /// after they pick a starting weapon. Uses the same tier/rarity/affix pipeline as dungeon armor drops.
        /// Re-rolls when the result is <see cref="Item.IsStarterItem"/> when the catalog allows a non-starter piece.
        /// </summary>
        public static Item? GenerateNewGameBonusLoot(Character player)
        {
            if (player == null)
                return null;

            Initialize();

            int playerLevel = Math.Max(1, player.Level);
            const int dungeonLevel = 1;

            const int maxAttempts = 16;
            Item? last = null;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                last = GenerateGuaranteedArmorLootOnce(player, playerLevel, dungeonLevel);
                if (last == null || last is WeaponItem)
                    continue;
                if (!last.IsStarterItem)
                    return last;
            }

            return last is WeaponItem ? null : last;
        }

        /// <summary>
        /// Guaranteed armor selection + the same post-selection steps as <see cref="GenerateLoot"/> (level, scaling, rarity, bonuses).
        /// </summary>
        private static Item? GenerateGuaranteedArmorLootOnce(Character player, int playerLevel, int dungeonLevel)
        {
            var tuning = GameConfiguration.Instance;
            int lootLevel = TierCalculator.CalculateLootLevel(playerLevel, dungeonLevel);
            string armorJsonSlot = ItemSelector.RollArmorJsonSlotUniform();
            int tier = TierCalculator.RollTier(lootLevel);
            Item? item = ItemSelector.SelectItem(tier, isWeapon: false, armorJsonSlot);

            if (item == null)
            {
                for (int fallbackTier = 1; fallbackTier <= 5; fallbackTier++)
                {
                    item = ItemSelector.SelectItem(fallbackTier, isWeapon: false, armorJsonSlot);
                    if (item != null)
                    {
                        tier = fallbackTier;
                        break;
                    }

                    foreach (string slot in NewGameBonusArmorJsonSlots)
                    {
                        item = ItemSelector.SelectItem(fallbackTier, isWeapon: false, slot);
                        if (item != null)
                        {
                            tier = fallbackTier;
                            armorJsonSlot = slot;
                            break;
                        }
                    }

                    if (item != null)
                        break;
                }
            }

            if (item == null)
            {
                int fallbackTier = tier > 0 ? tier : 1;
                armorJsonSlot = ItemSelector.RollArmorJsonSlotUniform();
                item = armorJsonSlot switch
                {
                    "head" => new HeadItem("Basic Helmet", fallbackTier, 3),
                    "chest" => new ChestItem("Basic Armor", fallbackTier, 5),
                    "legs" => new LegsItem("Basic Legs", fallbackTier, 3),
                    "feet" => new FeetItem("Basic Boots", fallbackTier, 3),
                    _ => new ChestItem("Basic Armor", fallbackTier, 5)
                };
                item.Level = Math.Max(1, lootLevel);
                item.Tier = fallbackTier;
                item.Rarity = "Common";
            }

            if (item is WeaponItem)
                return null;

            double levelRoll = _random.NextDouble();
            if (levelRoll < 0.5)
                item.Level = dungeonLevel;
            else if (levelRoll < 0.75)
                item.Level = dungeonLevel + 1;
            else
                item.Level = Math.Max(1, dungeonLevel - 1);

            ApplyItemScaling(item, tuning);

            var rarity = RarityProcessor.RollRarity(0.0, playerLevel);
            item.Rarity = rarity.Name?.Trim() ?? "Common";
            RarityProcessor.ApplyRarityScaling(item, rarity);

            var context = LootContext.Create(player, dungeonTheme: null, enemyArchetype: null);
            int affixMagicFind = Math.Clamp(player.GetMagicFind(), 0, 100);
            BonusApplier.ApplyBonuses(item, rarity, context, affixMagicFind);

            return item;
        }

        /// <summary>
        /// Calculates the loot drop chance based on player level, magic find, and guaranteed flag
        /// </summary>
        private static double CalculateLootChance(int playerLevel, Character? player, bool guaranteedLoot, GameConfiguration tuning, double bonusMagicFind = 0)
        {
            if (guaranteedLoot)
            {
                // Use configured drop chance for guaranteed loot (dungeon completion)
                return tuning.LootSystem.GuaranteedLootChance;
            }
            else
            {
                // Calculate normal loot chance
                double lootChance = tuning.LootSystem.BaseDropChance + (playerLevel * tuning.LootSystem.DropChancePerLevel);
                
                // Apply magic find modifier to loot chance
                double magicFind = (player?.GetMagicFind() ?? 0.0) + Math.Max(0, bonusMagicFind);
                lootChance += magicFind * tuning.LootSystem.MagicFindEffectiveness;
                
                return Math.Min(lootChance, tuning.LootSystem.MaxDropChance);
            }
        }

        /// <summary>
        /// Applies scaling formulas to base stats based on item type and tier
        /// </summary>
        private static void ApplyItemScaling(Item item, GameConfiguration tuning)
        {
            if (item is WeaponItem weapon)
            {
                // Apply bonus damage and attack speed based on tuning config
                var equipmentScaling = tuning.EquipmentScaling;
                if (equipmentScaling != null)
                {
                    // Use new EquipmentScaling configuration
                    weapon.BonusDamage = (int)(weapon.Tier * equipmentScaling.WeaponDamagePerTier);
                    weapon.BonusAttackSpeed = (int)(weapon.Tier * equipmentScaling.SpeedBonusPerTier);
                }
                else
                {
                    // Fallback to simple system
                    weapon.BonusDamage = weapon.Tier <= 1 ? 1 : Dice.Roll(1, Math.Max(2, weapon.Tier));
                    weapon.BonusAttackSpeed = (int)(weapon.Tier * 0.1); // Simple fallback
                }
            }
            // Note: Armor scaling is currently handled by base values (no additional scaling needed)
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
    }

    public class RarityData
    {
        public string Name { get; set; } = "";
        public double Weight { get; set; }
        public int StatBonuses { get; set; }
        public int ActionBonuses { get; set; }
        public int Modifications { get; set; }
    }
}
