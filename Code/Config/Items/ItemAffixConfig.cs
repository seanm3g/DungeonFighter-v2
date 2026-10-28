using System;
using System.Collections.Generic;

namespace RPGGame
{
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

        /// <summary>
        /// Affix rolls: when set, the item gets this many chances at an affix line; each succeeds at
        /// <see cref="AffixRollChance"/> (so a roll can yield nothing) and lands in a pool from
        /// <see cref="ItemAffixByRaritySettings.AffixPoolWeights"/>. Replaces the prefix/stat mins and extras (and the action
        /// axis too when its pool weight is above 0). Null = legacy per-axis counts.
        /// </summary>
        public int? AffixRolls { get; set; }

        /// <summary>Per-roll success chance (0–1) for this row; null = <see cref="ItemAffixByRaritySettings.AffixRollChance"/>.</summary>
        public double? AffixRollChance { get; set; }
    }

    /// <summary>
    /// Where a successful affix roll lands (relative weights; 0 keeps that axis out of the rolls). Prefix lines are capped at 2
    /// (Quality + Adjective). With <see cref="Action"/> at 0, gear actions keep their own per-axis min/max/chance rules.
    /// </summary>
    public sealed class AffixPoolWeights
    {
        public double Prefix { get; set; } = 1.0;
        public double Stat { get; set; } = 1.0;
        public double Action { get; set; }
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
            double extraComboSlotsExtraChance = 0,
            int affixRolls = -1,
            double affixRollChance = 1.0,
            AffixPoolWeights? poolWeights = null)
        {
            AffixRolls = affixRolls;
            AffixRollChance = Math.Clamp(affixRollChance, 0, 1);
            PoolPrefixWeight = Math.Max(0, poolWeights?.Prefix ?? 1.0);
            PoolStatWeight = Math.Max(0, poolWeights?.Stat ?? 1.0);
            PoolActionWeight = Math.Max(0, poolWeights?.Action ?? 0.0);
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

        /// <summary>Affix chances (see <see cref="ItemAffixPerRarityEntry.AffixRolls"/>); -1 = legacy per-axis counts.</summary>
        public int AffixRolls { get; }
        /// <summary>Success chance per affix roll, before magic find.</summary>
        public double AffixRollChance { get; }
        public double PoolPrefixWeight { get; }
        public double PoolStatWeight { get; }
        public double PoolActionWeight { get; }
        public bool UsesAffixRolls => AffixRolls >= 0;
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

        /// <summary>Pool split for rarities that use <see cref="ItemAffixPerRarityEntry.AffixRolls"/>.</summary>
        public AffixPoolWeights AffixPoolWeights { get; set; } = new();

        /// <summary>Default per-roll success chance (0–1) for <see cref="ItemAffixPerRarityEntry.AffixRolls"/>.</summary>
        public double AffixRollChance { get; set; } = 0.5;

        /// <summary>
        /// Optional base rarity roll weights (rarity name → weight) that replace <c>RarityTable.json</c> weights, so a balance
        /// patch can carry its own rarity curve. Rarities missing here keep their table weight. Empty = table weights.
        /// </summary>
        public Dictionary<string, double> RarityWeights { get; set; } = new();

        /// <summary>Weight for <paramref name="row"/>: <see cref="RarityWeights"/> override when present, else the table weight.</summary>
        public double ResolveRarityWeight(RarityData row)
        {
            if (RarityWeights != null && row?.Name != null)
            {
                foreach (var kv in RarityWeights)
                {
                    if (kv.Key.Equals(row.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                        return Math.Max(0, kv.Value);
                }
            }

            return row?.Weight ?? 0;
        }

        /// <summary>
        /// Optional per–item-category overrides (keys: Head, Chest, Legs, Feet, Weapon — case-insensitive).
        /// When present for <paramref name="itemType"/> and rarity, overrides <see cref="PerRarity"/>.
        /// </summary>
        public Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>> PerItemType { get; set; } = new();

        /// <summary>Maps <see cref="ItemType"/> to <see cref="PerItemType"/> outer dictionary key.</summary>
        public static string ItemTypeToAffixCategoryKey(ItemType type) =>
            type switch
            {
                ItemType.Head => "Head",
                ItemType.Chest => "Chest",
                ItemType.Legs => "Legs",
                ItemType.Feet => "Feet",
                ItemType.Weapon => "Weapon",
                ItemType.Consumable => "Chest",
                _ => "Chest"
            };

        /// <summary>Built-in Quality/Adjective prefix-slot count when tuning does not override (Material is always separate).</summary>
        public static int DefaultPrefixSlotsForRarity(string rarityName)
        {
            if (rarityName.Equals("Common", StringComparison.OrdinalIgnoreCase))
                return 1;
            // Uncommon+ fills both Quality and Adjective (Material is unconditional outside this budget).
            return 2;
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
                return BuildRuleFromTuningEntry(typedEntry, rarityTableRow, tuning);

            if (tuning != null && tuning.TryGetForRarity(name, out var entry))
                return BuildRuleFromTuningEntry(entry, rarityTableRow, tuning);

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
            RarityData? rarityTableRow,
            ItemAffixByRaritySettings? tuning = null)
        {
            int pMin = Math.Clamp(entry.PrefixSlots, 0, 2);
            double pCh = Clamp01(entry.PrefixExtraChance);
            int pMax = entry.PrefixSlotsMax.HasValue
                ? Math.Clamp(entry.PrefixSlotsMax.Value, pMin, 2)
                : (pCh <= 0 ? pMin : 2);

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

            int rolls = entry.AffixRolls.HasValue ? Math.Max(0, entry.AffixRolls.Value) : -1;
            double rollChance = entry.AffixRollChance ?? tuning?.AffixRollChance ?? 0.5;
            return new ItemAffixRollRule(pMin, pMax, pCh, sMin, sMax, sCh, aMin, aMax, aCh, eMin, eMax, eCh,
                rolls, rollChance, tuning?.AffixPoolWeights);
        }

        /// <summary>Most Quality/Adjective prefix lines one item can hold.</summary>
        public const int MaxPrefixLines = 2;

        /// <summary>
        /// Takes <see cref="ItemAffixRollRule.AffixRolls"/> chances at <paramref name="rollChance"/> each; every hit lands in a
        /// weighted pool (prefix capped at <see cref="MaxPrefixLines"/>). Misses, and hits that fit no pool, add nothing.
        /// Actions are only counted here when their pool weight is above 0.
        /// </summary>
        public static void RollAffixLines(Random random, in ItemAffixRollRule rule, double rollChance, out int prefixSlots, out int statSuffixes, out int actionBonuses)
        {
            prefixSlots = 0;
            statSuffixes = 0;
            actionBonuses = 0;
            for (int i = 0; i < rule.AffixRolls; i++)
            {
                if (random.NextDouble() >= rollChance)
                    continue;

                double wp = prefixSlots < MaxPrefixLines ? rule.PoolPrefixWeight : 0;
                double ws = rule.PoolStatWeight;
                double wa = rule.PoolActionWeight;
                double total = wp + ws + wa;
                if (total <= 0)
                    return;

                double roll = random.NextDouble() * total;
                if (roll < wp)
                    prefixSlots++;
                else if (roll < wp + ws)
                    statSuffixes++;
                else
                    actionBonuses++;
            }
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

            if (rule.UsesAffixRolls)
            {
                // Rarity sets how many chances; magic find raises the odds of each one landing.
                RollAffixLines(random, rule, Clamp01(rule.AffixRollChance * chanceMult), out prefixSlots, out statSuffixes, out int rolledActions);
                actionBonuses = rule.PoolActionWeight > 0
                    ? rolledActions
                    : RollAxis(random, rule.ActionMin, rule.ActionMax, aCh);
            }
            else
            {
                prefixSlots = RollAxis(random, rule.PrefixMin, rule.PrefixMax, pCh);
                statSuffixes = RollAxis(random, rule.StatMin, rule.StatMax, sCh);
                actionBonuses = RollAxis(random, rule.ActionMin, rule.ActionMax, aCh);
            }

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
}
