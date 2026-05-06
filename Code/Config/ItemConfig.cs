using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Item scaling configuration
    /// </summary>
    public class ItemScalingConfig
    {
        public Dictionary<string, double> StartingWeaponDamage { get; set; } = new();
        public Dictionary<string, double> StartingWeaponSpeed { get; set; } = new();
        public Dictionary<string, TierRange> TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; }
        public int WeaponDamagePerTier { get; set; }
        public int ArmorValuePerTier { get; set; }
        public double SpeedBonusPerTier { get; set; }
        public int MaxTier { get; set; }
        public double EnchantmentChance { get; set; }

        public void EnsureSanitizedWeaponScalingDefaults()
        {
            if (GlobalDamageMultiplier <= 0)
                GlobalDamageMultiplier = 1.0;
            if (WeaponDamagePerTier <= 0)
                WeaponDamagePerTier = 3;
            if (ArmorValuePerTier <= 0)
                ArmorValuePerTier = 2;
            if (MaxTier <= 0)
                MaxTier = 10;
            if (SpeedBonusPerTier <= 0)
                SpeedBonusPerTier = 0.05;
            if (EnchantmentChance < 0)
                EnchantmentChance = 0;
        }
    }

    /// <summary>
    /// Weapon scaling configuration
    /// </summary>
    public class WeaponScalingConfig
    {
        public StartingWeaponDamageConfig StartingWeaponDamage { get; set; } = new();
        public TierDamageRangesConfig TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; }
        public string Description { get; set; } = "";

        public void EnsureSanitizedDefaults()
        {
            if (GlobalDamageMultiplier <= 0)
                GlobalDamageMultiplier = 1.0;
        }
    }

    /// <summary>
    /// Equipment scaling configuration
    /// </summary>
    public class EquipmentScalingConfig
    {
        public int WeaponDamagePerTier { get; set; }
        public int ArmorValuePerTier { get; set; }
        public double SpeedBonusPerTier { get; set; }
        public int MaxTier { get; set; }
        public double EnchantmentChance { get; set; }
        public string Description { get; set; } = "";

        public void EnsureSensibleDefaults()
        {
            if (WeaponDamagePerTier <= 0)
                WeaponDamagePerTier = 3;
            if (ArmorValuePerTier <= 0)
                ArmorValuePerTier = 2;
            if (SpeedBonusPerTier <= 0)
                SpeedBonusPerTier = 0.05;
            if (MaxTier <= 0)
                MaxTier = 10;
            if (EnchantmentChance < 0)
                EnchantmentChance = 0;
        }
    }

    /// <summary>
    /// Rarity scaling configuration
    /// </summary>
    public class RarityScalingConfig
    {
        public RarityMultipliers StatBonusMultipliers { get; set; } = new();
        public RollChanceFormulas RollChanceFormulas { get; set; } = new();
        public MagicFindScalingConfig MagicFindScaling { get; set; } = new();
        public LevelBasedRarityScalingConfig LevelBasedRarityScaling { get; set; } = new();

        /// <summary>
        /// Global strength for magic find on the <b>initial</b> rarity weight roll:
        /// adjustedWeight = baseWeight * Exp(alpha * t * k_r) with t = clamp(MF,0,100)/100 and k_r from <see cref="MagicFindScaling"/> (or built-in defaults when all per-point values are zero).
        /// Values &lt;= 0 disable MF tilt (weights unchanged).
        /// </summary>
        public double MagicFindDistributionAlpha { get; set; } = 0.5;
    }

    /// <summary>
    /// Per-rarity affix tuning for generated loot (dungeon drops, trade-up, item lab).
    /// When <see cref="ItemAffixByRaritySettings.PerRarity"/> contains a key matching the rolled rarity
    /// (case-insensitive), these bounds override <c>RarityTable.json</c> stat/action columns and the
    /// built-in prefix-slot rules for that rarity only. Rarities omitted from the map keep legacy behavior.
    /// <see cref="PrefixSlots"/>, <see cref="StatSuffixes"/>, <see cref="ActionBonuses"/>, and <see cref="ExtraComboSlots"/> are minimums;
    /// optional max + extra chance roll additional affixes (or extra strip slots) up to the cap (see <see cref="ItemAffixByRaritySettings.GetResolvedAffixRule"/>).
    /// </summary>
    public sealed class ItemAffixPerRarityEntry
    {
        /// <summary>Minimum prefix-slot modifications (Quality / Adjective / Material), 0–3.</summary>
        public int PrefixSlots { get; set; }

        /// <summary>Minimum random stat bonuses (name suffixes).</summary>
        public int StatSuffixes { get; set; }

        /// <summary>Minimum random action bonuses.</summary>
        public int ActionBonuses { get; set; }

        /// <summary>Maximum prefix slots (0–3). When null and <see cref="PrefixExtraChance"/> is 0, effective max equals min.</summary>
        public int? PrefixSlotsMax { get; set; }

        /// <summary>Per-step chance (0–1) to add one more prefix slot while below max.</summary>
        public double PrefixExtraChance { get; set; }

        public int? StatSuffixesMax { get; set; }

        /// <summary>Per-step chance (0–1) to add one more stat suffix while below max.</summary>
        public double StatSuffixExtraChance { get; set; }

        public int? ActionBonusesMax { get; set; }

        /// <summary>Per-step chance (0–1) to add one more action bonus while below max.</summary>
        public double ActionExtraChance { get; set; }

        /// <summary>Minimum extra combo strip slots added in <see cref="LootBonusApplier"/> (added to <c>Item.ExtraActionSlots</c> after catalog roll).</summary>
        public int ExtraComboSlots { get; set; }

        public int? ExtraComboSlotsMax { get; set; }

        /// <summary>Per-step chance (0–1) to add one more extra combo slot while below max.</summary>
        public double ExtraComboSlotsExtraChance { get; set; }
    }

    /// <summary>
    /// Resolved min/max and per-step extra chance for one rarity (before random rolls).
    /// </summary>
    public readonly struct ItemAffixRollRule
    {
        public ItemAffixRollRule(
            int prefixMin,
            int prefixMax,
            double prefixExtraChance,
            int statMin,
            int statMax,
            double statExtraChance,
            int actionMin,
            int actionMax,
            double actionExtraChance,
            int extraComboSlotsMin = 0,
            int extraComboSlotsMax = 0,
            double extraComboSlotsExtraChance = 0)
        {
            PrefixMin = prefixMin;
            PrefixMax = prefixMax;
            PrefixExtraChance = prefixExtraChance;
            StatMin = statMin;
            StatMax = statMax;
            StatExtraChance = statExtraChance;
            ActionMin = actionMin;
            ActionMax = actionMax;
            ActionExtraChance = actionExtraChance;
            ExtraComboSlotsMin = extraComboSlotsMin;
            ExtraComboSlotsMax = extraComboSlotsMax;
            ExtraComboSlotsExtraChance = extraComboSlotsExtraChance;
        }

        public int PrefixMin { get; }
        public int PrefixMax { get; }
        public double PrefixExtraChance { get; }
        public int StatMin { get; }
        public int StatMax { get; }
        public double StatExtraChance { get; }
        public int ActionMin { get; }
        public int ActionMax { get; }
        public double ActionExtraChance { get; }
        public int ExtraComboSlotsMin { get; }
        public int ExtraComboSlotsMax { get; }
        public double ExtraComboSlotsExtraChance { get; }
    }

    /// <summary>
    /// Optional tuning overrides for affix counts by item rarity (<c>TuningConfig.json</c> → <c>GameConfiguration</c>).
    /// </summary>
    public sealed class ItemAffixByRaritySettings
    {
        /// <summary>Rarities shown in settings UI and written as a full block to tuning when saving.</summary>
        public static readonly string[] StandardLootRarities =
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
        };

        public string Description { get; set; } =
            "Per-rarity affix bounds. PrefixSlots/StatSuffixes/ActionBonuses/ExtraComboSlots = minimums; *Max and *ExtraChance = optional random extras up to max (extra combo slots add to Item.ExtraActionSlots in LootBonusApplier). Omit a rarity to use RarityTable + built-in prefix rules for that tier.";

        /// <summary>Keys are rarity names, e.g. Common, Rare (matched case-insensitively).</summary>
        public Dictionary<string, ItemAffixPerRarityEntry> PerRarity { get; set; } = new();

        /// <summary>
        /// Optional per–item-category overrides (keys: Head, Chest, Feet, Weapon — case-insensitive).
        /// When present for <paramref name="itemType"/> and rarity, overrides <see cref="PerRarity"/>.
        /// </summary>
        public Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>> PerItemType { get; set; } = new();

        /// <summary>Maps <see cref="ItemType"/> to <see cref="PerItemType"/> outer dictionary key.</summary>
        public static string ItemTypeToAffixCategoryKey(ItemType type) =>
            type switch
            {
                ItemType.Head => "Head",
                ItemType.Chest => "Chest",
                ItemType.Feet => "Feet",
                ItemType.Weapon => "Weapon",
                _ => "Chest"
            };

        /// <summary>Built-in prefix-slot count when tuning does not override this rarity.</summary>
        public static int DefaultPrefixSlotsForRarity(string rarityName)
        {
            if (rarityName.Equals("Common", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (rarityName.Equals("Uncommon", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 3;
        }

        /// <summary>
        /// Resolves affix roll rule (min/max/chance per axis). No tuning entry uses rarity table + default prefix slots; all extra chances are 0 and max equals min.
        /// </summary>
        public static ItemAffixRollRule GetResolvedAffixRule(
            string? rarityName,
            RarityData? rarityTableRow,
            ItemAffixByRaritySettings? tuning,
            ItemType? itemType = null)
        {
            string name = rarityName?.Trim() ?? "Common";
            if (tuning != null && itemType.HasValue &&
                tuning.TryGetForItemTypeAndRarity(itemType.Value, name, out var typedEntry))
                return BuildRuleFromTuningEntry(typedEntry, rarityTableRow);

            if (tuning != null && tuning.TryGetForRarity(name, out var entry))
                return BuildRuleFromTuningEntry(entry, rarityTableRow);

            int p = DefaultPrefixSlotsForRarity(name);
            int s = Math.Max(0, rarityTableRow?.StatBonuses ?? 0);
            int a = Math.Max(0, rarityTableRow?.ActionBonuses ?? 0);
            return new ItemAffixRollRule(p, p, 0, s, s, 0, a, a, 0, 0, 0, 0);
        }

        /// <summary>
        /// Builds a roll rule from a tuning row: mins from <see cref="ItemAffixPerRarityEntry"/>; max defaults to min when extra chance is 0 (legacy fixed counts).
        /// </summary>
        public static ItemAffixRollRule BuildRuleFromTuningEntry(
            ItemAffixPerRarityEntry entry,
            RarityData? rarityTableRow)
        {
            int pMin = Math.Clamp(entry.PrefixSlots, 0, 3);
            double pCh = Clamp01(entry.PrefixExtraChance);
            int pMax = entry.PrefixSlotsMax.HasValue
                ? Math.Clamp(entry.PrefixSlotsMax.Value, pMin, 3)
                : (pCh <= 0 ? pMin : 3);

            int sMin = Math.Max(0, entry.StatSuffixes);
            double sCh = Clamp01(entry.StatSuffixExtraChance);
            int sDefaultCeiling = Math.Max(sMin, rarityTableRow?.StatBonuses ?? sMin);
            if (sDefaultCeiling == sMin && sCh > 0)
                sDefaultCeiling = Math.Max(sMin, 8);
            int sMax = entry.StatSuffixesMax.HasValue
                ? Math.Max(sMin, entry.StatSuffixesMax.Value)
                : (sCh <= 0 ? sMin : sDefaultCeiling);

            int aMin = Math.Max(0, entry.ActionBonuses);
            double aCh = Clamp01(entry.ActionExtraChance);
            int aDefaultCeiling = Math.Max(aMin, rarityTableRow?.ActionBonuses ?? aMin);
            // When max is omitted, allow at least one step above min (e.g. 0→1 optional action);
            // avoid legacy default of 5 which made "25% one action" JSON without max behave like many rolls.
            if (aDefaultCeiling == aMin && aCh > 0)
                aDefaultCeiling = Math.Max(aMin + 1, rarityTableRow?.ActionBonuses ?? 0);
            int aMax = entry.ActionBonusesMax.HasValue
                ? Math.Max(aMin, entry.ActionBonusesMax.Value)
                : (aCh <= 0 ? aMin : aDefaultCeiling);

            int eMin = Math.Max(0, entry.ExtraComboSlots);
            double eCh = Clamp01(entry.ExtraComboSlotsExtraChance);
            int eDefaultCeiling = eMin;
            if (eDefaultCeiling == eMin && eCh > 0)
                eDefaultCeiling = Math.Max(eMin + 1, 1);
            int eMax = entry.ExtraComboSlotsMax.HasValue
                ? Math.Max(eMin, entry.ExtraComboSlotsMax.Value)
                : (eCh <= 0 ? eMin : eDefaultCeiling);

            return new ItemAffixRollRule(pMin, pMax, pCh, sMin, sMax, sCh, aMin, aMax, aCh, eMin, eMax, eCh);
        }

        /// <summary>
        /// Rolls final affix counts from a rule using <paramref name="random"/> (no magic find on extra chances).
        /// </summary>
        public static void RollAffixCounts(Random random, in ItemAffixRollRule rule, out int prefixSlots, out int statSuffixes, out int actionBonuses)
        {
            RollAffixCounts(random, rule, 0, null, out prefixSlots, out statSuffixes, out actionBonuses, out _);
        }

        /// <summary>
        /// Rolls affix counts; when <paramref name="magicFind0To100"/> &gt; 0, scales only the *extra* step chances (prefix/stat/action) toward up to <see cref="LootSystemConfig.AffixMagicFindMaxExtraChanceBoost"/> at MF 100, each capped at 1.0.
        /// </summary>
        public static void RollAffixCounts(
            Random random,
            in ItemAffixRollRule rule,
            int magicFind0To100,
            LootSystemConfig? loot,
            out int prefixSlots,
            out int statSuffixes,
            out int actionBonuses,
            out int extraComboSlots)
        {
            double t = Math.Clamp(magicFind0To100, 0, 100) / 100.0;
            double maxBoost = loot?.AffixMagicFindMaxExtraChanceBoost ?? 2.0;
            if (maxBoost < 1.0)
                maxBoost = 1.0;
            double chanceMult = 1.0 + t * (maxBoost - 1.0);

            double pCh = Clamp01(rule.PrefixExtraChance * chanceMult);
            double sCh = Clamp01(rule.StatExtraChance * chanceMult);
            double aCh = Clamp01(rule.ActionExtraChance * chanceMult);
            double eCh = Clamp01(rule.ExtraComboSlotsExtraChance * chanceMult);

            prefixSlots = RollAxis(random, rule.PrefixMin, rule.PrefixMax, pCh);
            statSuffixes = RollAxis(random, rule.StatMin, rule.StatMax, sCh);
            actionBonuses = RollAxis(random, rule.ActionMin, rule.ActionMax, aCh);
            extraComboSlots = RollAxis(random, rule.ExtraComboSlotsMin, rule.ExtraComboSlotsMax, eCh);
        }

        /// <summary>
        /// Starts at <paramref name="min"/> and increments while below <paramref name="max"/> and <paramref name="extraChance"/> succeeds.
        /// </summary>
        public static int RollAxis(Random random, int min, int max, double extraChance)
        {
            min = Math.Max(0, min);
            if (max < min)
                max = min;
            extraChance = Clamp01(extraChance);
            int c = min;
            while (c < max && random.NextDouble() < extraChance)
                c++;
            return c;
        }

        private static double Clamp01(double v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }

        /// <summary>
        /// Returns minimum affix counts from the resolved rule (same as rolling with zero extra chance on all axes).
        /// </summary>
        public static void GetResolvedAffixCounts(
            string? rarityName,
            RarityData? rarityTableRow,
            ItemAffixByRaritySettings? tuning,
            out int prefixSlots,
            out int statSuffixes,
            out int actionBonuses,
            out int extraComboSlotsMin,
            ItemType? itemType = null)
        {
            var rule = GetResolvedAffixRule(rarityName, rarityTableRow, tuning, itemType);
            prefixSlots = rule.PrefixMin;
            statSuffixes = rule.StatMin;
            actionBonuses = rule.ActionMin;
            extraComboSlotsMin = rule.ExtraComboSlotsMin;
        }

        /// <summary>Returns true when <paramref name="rarityName"/> has an explicit tuning entry.</summary>
        public bool TryGetForRarity(string? rarityName, out ItemAffixPerRarityEntry entry)
        {
            entry = null!;
            if (PerRarity == null || PerRarity.Count == 0 || string.IsNullOrWhiteSpace(rarityName))
                return false;

            string trimmed = rarityName.Trim();
            if (PerRarity.TryGetValue(trimmed, out var direct) && direct != null)
            {
                entry = direct;
                return true;
            }

            foreach (var kv in PerRarity)
            {
                if (kv.Key.Equals(trimmed, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                {
                    entry = kv.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when <see cref="PerItemType"/> has an entry for this item category and rarity.</summary>
        public bool TryGetForItemTypeAndRarity(ItemType itemType, string? rarityName, out ItemAffixPerRarityEntry entry)
        {
            entry = null!;
            if (PerItemType == null || PerItemType.Count == 0 || string.IsNullOrWhiteSpace(rarityName))
                return false;

            string cat = ItemTypeToAffixCategoryKey(itemType);
            Dictionary<string, ItemAffixPerRarityEntry>? inner = null;
            if (PerItemType.TryGetValue(cat, out var direct) && direct != null)
                inner = direct;
            else
            {
                foreach (var kv in PerItemType)
                {
                    if (kv.Key.Equals(cat, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                    {
                        inner = kv.Value;
                        break;
                    }
                }
            }

            if (inner == null || inner.Count == 0)
                return false;

            string trimmed = rarityName.Trim();
            if (inner.TryGetValue(trimmed, out var row) && row != null)
            {
                entry = row;
                return true;
            }

            foreach (var kv in inner)
            {
                if (kv.Key.Equals(trimmed, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                {
                    entry = kv.Value;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Loot system configuration
    /// </summary>
    public class LootSystemConfig
    {
        public double BaseDropChance { get; set; }
        public double DropChancePerLevel { get; set; }
        public double MaxDropChance { get; set; }
        public double GuaranteedLootChance { get; set; }
        public double MagicFindEffectiveness { get; set; }
        public double GoldDropMultiplier { get; set; }
        public double ItemValueMultiplier { get; set; }

        /// <summary>At MF 100, affix-line tier weights for prefix/suffix pool rolls scale up to this multiplier on the highest tier index present in the pool (Common stays 1×).</summary>
        public double AffixMagicFindMaxWeightBoost { get; set; } = 2.0;

        /// <summary>At MF 100, optional affix *extra* chances (prefix/stat/action) multiply by this value before capping at 1.0 per step.</summary>
        public double AffixMagicFindMaxExtraChanceBoost { get; set; } = 2.0;

        /// <summary>Default maximum number of actions in the player combo sequence before feet bonuses.</summary>
        public int ComboSequenceBaseMax { get; set; } = 2;

        /// <summary>Hard cap on combo sequence length after equipment <see cref="Item.ExtraActionSlots"/> and <c>ExtraActionSlots</c> affixes.</summary>
        public int ComboSequenceAbsoluteMax { get; set; } = 8;

        public RarityUpgradeConfig RarityUpgrade { get; set; } = new();
        public string Description { get; set; } = "";

        public void EnsureSensibleLootDefaults()
        {
            if (BaseDropChance <= 0)
                BaseDropChance = 0.15;
            if (DropChancePerLevel < 0)
                DropChancePerLevel = 0;
            if (MaxDropChance <= 0)
                MaxDropChance = 0.95;
            if (GuaranteedLootChance <= 0)
                GuaranteedLootChance = 1.0;
            if (MagicFindEffectiveness < 0)
                MagicFindEffectiveness = 0;
            if (GoldDropMultiplier <= 0)
                GoldDropMultiplier = 1.0;
            if (ItemValueMultiplier <= 0)
                ItemValueMultiplier = 1.0;
            if (AffixMagicFindMaxWeightBoost < 1.0)
                AffixMagicFindMaxWeightBoost = 1.0;
            if (AffixMagicFindMaxExtraChanceBoost < 1.0)
                AffixMagicFindMaxExtraChanceBoost = 1.0;
            if (ComboSequenceBaseMax < 1)
                ComboSequenceBaseMax = 2;
            if (ComboSequenceAbsoluteMax < ComboSequenceBaseMax)
                ComboSequenceAbsoluteMax = ComboSequenceBaseMax;
        }
    }

    /// <summary>
    /// Rarity upgrade configuration - cascading rarity progression
    /// </summary>
    public class RarityUpgradeConfig
    {
        public bool Enabled { get; set; } = false;
        public double BaseUpgradeChance { get; set; } = 0.05;
        public double UpgradeChanceDecayPerTier { get; set; } = 0.5;
        public int MaxUpgradeTiers { get; set; } = 6;
        public double MagicFindBonus { get; set; } = 0.0001;
        public string Description { get; set; } = "Cascading rarity upgrade system - items can upgrade to next tier with exponentially decreasing probability";
    }

    /// <summary>
    /// Modification rarity configuration
    /// </summary>
    public class ModificationRarityConfig
    {
        public double Common { get; set; } = 35.0;
        public double Uncommon { get; set; } = 25.0;
        public double Rare { get; set; } = 20.0;
        public double Epic { get; set; } = 12.0;
        public double Legendary { get; set; } = 6.0;
        public double Mythic { get; set; } = 1.8;
        public double TierBonusPerLevel { get; set; } = 1.5;
        public double BonusPointEffectiveness { get; set; } = 1.0;
        public string Description { get; set; } = "Modification rarity distribution percentages. Total should equal 100.0";
    }

    /// <summary>
    /// Tier range configuration
    /// </summary>
    public class TierRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    /// <summary>
    /// Weapon type configuration
    /// </summary>
    public class WeaponTypeConfig
    {
        public string DamageFormula { get; set; } = "";
        public string SpeedFormula { get; set; } = "";
        public ScalingFactorsConfig ScalingFactors { get; set; } = new();
    }

    /// <summary>
    /// Armor type configuration
    /// </summary>
    public class ArmorTypeConfig
    {
        public string ArmorFormula { get; set; } = "";
        public string ActionChanceFormula { get; set; } = "";
    }

    /// <summary>
    /// Scaling factors configuration
    /// </summary>
    public class ScalingFactorsConfig
    {
        public double StrengthWeight { get; set; }
        public double AgilityWeight { get; set; }
        public double TechniqueWeight { get; set; }
        public double IntelligenceWeight { get; set; }
    }

    /// <summary>
    /// Rarity modifier configuration
    /// </summary>
    public class RarityModifierConfig
    {
        public double DamageMultiplier { get; set; }
        public double ArmorMultiplier { get; set; }
        public double BonusChanceMultiplier { get; set; }
    }

    /// <summary>
    /// Level scaling caps configuration
    /// </summary>
    public class LevelScalingCapsConfig
    {
        public double MaxDamageScaling { get; set; }
        public double MaxArmorScaling { get; set; }
        public double MaxSpeedScaling { get; set; }
        public MinimumValuesConfig MinimumValues { get; set; } = new();
    }

    /// <summary>
    /// Minimum values configuration
    /// </summary>
    public class MinimumValuesConfig
    {
        public int Damage { get; set; }
        public int Armor { get; set; }
        public double Speed { get; set; }
    }

    /// <summary>
    /// Formula configuration
    /// </summary>
    public class FormulaConfig
    {
        public double BaseMultiplier { get; set; }
        public double TierScaling { get; set; }
        public double LevelScaling { get; set; }
        public string Formula { get; set; } = "";
    }

    /// <summary>
    /// Rarity multipliers configuration
    /// </summary>
    public class RarityMultipliers
    {
        public double Common { get; set; }
        public double Uncommon { get; set; }
        public double Rare { get; set; }
        public double Epic { get; set; }
        public double Legendary { get; set; }
    }

    /// <summary>
    /// Roll chance formulas configuration
    /// </summary>
    public class RollChanceFormulas
    {
        public string ActionBonusChance { get; set; } = "";
        public string StatBonusChance { get; set; } = "";
    }

    /// <summary>
    /// Starting weapon damage configuration
    /// </summary>
    public class StartingWeaponDamageConfig
    {
        public int Mace { get; set; }
        public int Sword { get; set; }
        public int Dagger { get; set; }
        public int Wand { get; set; }
    }

    /// <summary>
    /// Starting gear configuration (weapons and armor)
    /// </summary>
    public class StartingGearConfig
    {
        public List<StartingWeaponConfig> Weapons { get; set; } = new();
        public List<StartingArmorConfig> Armor { get; set; } = new();
        public string Description { get; set; } = "Initial equipment for new characters";
    }

    /// <summary>
    /// Starting weapon configuration
    /// </summary>
    public class StartingWeaponConfig
    {
        public string Name { get; set; } = "";
        public double Damage { get; set; }
        public double AttackSpeed { get; set; }
        public double Weight { get; set; } = 0.0;
    }

    /// <summary>
    /// Starting armor configuration
    /// </summary>
    public class StartingArmorConfig
    {
        public string Slot { get; set; } = "";
        public string Name { get; set; } = "";
        public int Armor { get; set; }
        public double Weight { get; set; } = 0.0;
    }

    /// <summary>
    /// Tier damage ranges configuration
    /// </summary>
    public class TierDamageRangesConfig
    {
        public MinMaxConfig Tier1 { get; set; } = new();
        public MinMaxConfig Tier2 { get; set; } = new();
        public MinMaxConfig Tier3 { get; set; } = new();
        public MinMaxConfig Tier4 { get; set; } = new();
        public MinMaxConfig Tier5 { get; set; } = new();
    }

    /// <summary>
    /// Magic find scaling configuration
    /// </summary>
    public class MagicFindScalingConfig
    {
        public RarityMagicFindConfig Common { get; set; } = new();
        public RarityMagicFindConfig Uncommon { get; set; } = new();
        public RarityMagicFindConfig Rare { get; set; } = new();
        public RarityMagicFindConfig Epic { get; set; } = new();
        public RarityMagicFindConfig Legendary { get; set; } = new();
        public RarityMagicFindConfig Mythic { get; set; } = new();
    }

    /// <summary>
    /// Rarity magic find configuration
    /// </summary>
    public class RarityMagicFindConfig
    {
        public double PerPointMultiplier { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Level-based rarity scaling configuration
    /// </summary>
    public class LevelBasedRarityScalingConfig
    {
        public CommonRarityScalingConfig Common { get; set; } = new();
        public UncommonRarityScalingConfig Uncommon { get; set; } = new();
        public RareRarityScalingConfig Rare { get; set; } = new();
        public EpicRarityScalingConfig Epic { get; set; } = new();
        public LegendaryRarityScalingConfig Legendary { get; set; } = new();
    }

    /// <summary>
    /// Common rarity scaling configuration
    /// </summary>
    public class CommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelReduction { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Uncommon rarity scaling configuration
    /// </summary>
    public class UncommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Rare rarity scaling configuration
    /// </summary>
    public class RareRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Epic rarity scaling configuration
    /// </summary>
    public class EpicRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public double EarlyMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Legendary rarity scaling configuration
    /// </summary>
    public class LegendaryRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public int EarlyThreshold { get; set; }
        public double EarlyMultiplier { get; set; }
        public double MidMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }
}
