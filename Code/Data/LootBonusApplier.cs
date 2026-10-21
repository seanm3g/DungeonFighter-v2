using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Applies bonuses, modifications, and stat adjustments to items.
    /// Handles Quality/Adjective prefix lottery (0–2), always-on Material,
    /// stat bonuses, and action bonuses. Combat procs come from MATERIAL BUILDS set counts, not per-item rolls.
    /// </summary>
    public class LootBonusApplier
    {
        private static readonly string[] AffixTierOrder =
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
        };

        private readonly LootDataCache _dataCache;
        private readonly Random _random;
        private readonly LootModificationSelector? _modificationSelector;

        /// <summary>Clamped 0–100 for the current <see cref="ApplyBonuses"/> call (prefix/suffix tier + optional affix extras).</summary>
        private int _affixMagicFind;

        public LootBonusApplier(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
            _modificationSelector = new LootModificationSelector(random);
        }

        /// <summary>
        /// Applies all bonuses to an item based on its rarity.
        /// <paramref name="magicFind"/> (0–100) tilts affix-line tier rolls and optional affix extra chances; it does not change base item rarity.
        /// Material is always assigned (weapons: class ladder; armor: any).
        /// Item combat procs are not rolled — MATERIAL BUILDS gates synthesis by equipped count.
        /// </summary>
        public void ApplyBonuses(Item item, RarityData rarity, LootContext? context = null, int magicFind = 0)
        {
            _affixMagicFind = Math.Clamp(magicFind, 0, 100);
            try
            {
                string rarityName = rarity.Name?.Trim() ?? "Common";
                bool legacyRarity = GameConfiguration.Instance?.LootSystem?.RarityFromAffixes ?? true;
                _itemRarityCapIndex = legacyRarity ? -1 : Math.Max(0, AffixTierOrderIndex(rarityName));

                item.StatBonuses.Clear();
                item.ActionBonuses.Clear();

                var rule = ItemAffixByRaritySettings.GetResolvedAffixRule(
                    rarityName,
                    rarity,
                    GameConfiguration.Instance?.ItemAffixByRarity,
                    item.Type);
                ItemAffixByRaritySettings.RollAffixCounts(
                    _random,
                    rule,
                    _affixMagicFind,
                    GameConfiguration.Instance?.LootSystem,
                    out int prefixSlots,
                    out int statSuffixes,
                    out int actionBonuses,
                    out int extraComboSlotsLoot);

                if (item.MinGeneratedActionBonuses > 0)
                    actionBonuses = Math.Max(actionBonuses, item.MinGeneratedActionBonuses);

                if (extraComboSlotsLoot > 0)
                    item.ExtraActionSlots = Math.Max(0, item.ExtraActionSlots + extraComboSlotsLoot);

                ApplyStatBonuses(item, statSuffixes);
                ApplyActionBonuses(item, actionBonuses, context);
                ApplyPrefixSlots(item, prefixSlots, context);
                EnsureMaterial(item, rarityName, context);
                MaterialTriggerMerge.ClearCatalogTriggerStamp(item);
                MaterialTriggerMerge.RemapLegacyMaterialOnItem(item);
                if (GameConfiguration.Instance?.LootSystem?.MaterialPieceTriggers == true)
                    MaterialTriggerMerge.ApplyMaterialTrigger(item, _random);
                SyncMaterialPrefixTags(item);

                item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
                if (legacyRarity)
                    AdjustRarityBasedOnBonuses(item, rarity);
                ResolveRerollAdjectiveIfPresent(item, context);
                SyncMaterialPrefixTags(item);
                if (legacyRarity)
                    AdjustRarityBasedOnBonuses(item, rarity);
                item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
            }
            finally
            {
                _affixMagicFind = 0;
                _itemRarityCapIndex = -1;
            }
        }

        /// <summary>
        /// Fills Quality / Adjective prefix categories (0–2). Material is applied separately via <see cref="EnsureMaterial"/>.
        /// </summary>
        public void ApplyPrefixSlots(Item item, string rarityName, LootContext? context = null) =>
            ApplyPrefixSlots(item, ItemAffixByRaritySettings.DefaultPrefixSlotsForRarity(rarityName), context);

        /// <summary>
        /// Fills <paramref name="prefixSlotCount"/> Quality/Adjective slots (0–2). Material is not part of this lottery.
        /// </summary>
        public void ApplyPrefixSlots(Item item, int prefixSlotCount, LootContext? context = null)
        {
            item.Modifications.Clear();

            var categories = SelectCategoriesForPrefixSlotCount(prefixSlotCount, _random);
            foreach (var cat in categories)
            {
                var mod = RollOneCategory(cat, item, context, 0);
                if (mod != null)
                    item.Modifications.Add(mod);
            }

            item.RecomputeAttributeRequirementsIncludingModifications();
        }

        /// <summary>
        /// Ensures exactly one Material prefix. Weapons use the class ladder; non-weapons pick any material.
        /// </summary>
        /// <summary>Market skill rules: a non-weapon drop may take the hero's class material for its rarity.</summary>
        private string ApplyClassMaterialBias(string materialName, string rarityName, LootContext? context)
        {
            if (context?.ClassMaterialChance == null || context.ClassMaterialChance.Count == 0)
                return materialName;
            foreach (var kv in context.ClassMaterialChance)
            {
                if (kv.Value <= 0 || _random.NextDouble() >= kv.Value)
                    continue;
                if (ClassTags.WeaponOf(kv.Key) is WeaponType wt)
                    return ItemMaterialRules.ResolveWeaponMaterial(wt, rarityName);
            }
            return materialName;
        }

        public void EnsureMaterial(Item item, string? itemRarity = null, LootContext? context = null)
        {
            if (item == null)
                return;

            string rarityName = itemRarity?.Trim()
                ?? item.Rarity?.Trim()
                ?? "Common";

            item.Modifications.RemoveAll(m =>
                m != null && m.GetPrefixCategory() == ModificationPrefixCategory.Material);

            string materialName;
            if (item.Type == ItemType.Weapon)
            {
                materialName = ItemMaterialRules.ResolveWeaponMaterial(item.WeaponType, rarityName);
            }
            else
            {
                var materialMods = (_dataCache.Modifications ?? new List<Modification>())
                    .Where(m => m.GetPrefixCategory() == ModificationPrefixCategory.Material);
                materialName = ItemMaterialRules.PickNonWeaponMaterial(materialMods, rarityName, _random);
                materialName = ApplyClassMaterialBias(materialName, rarityName, context);
            }

            var template = (_dataCache.Modifications ?? new List<Modification>())
                .FirstOrDefault(m =>
                    m.GetPrefixCategory() == ModificationPrefixCategory.Material
                    && string.Equals(m.Name?.Trim(), materialName, StringComparison.OrdinalIgnoreCase));

            Modification mod;
            if (template != null)
            {
                mod = CloneRolledModification(template, 0)!;
            }
            else
            {
                mod = new Modification
                {
                    Name = materialName,
                    PrefixCategory = "MATERIAL",
                    ItemRank = rarityName,
                    Description = "Material",
                    MinValue = 0,
                    MaxValue = 0,
                    RolledValue = 0,
                    Tags = new List<string> { materialName.ToLowerInvariant() }
                };
            }

            item.Modifications.Add(mod);
            item.Material = materialName;
            item.RecomputeAttributeRequirementsIncludingModifications();
        }

        /// <summary>
        /// Ensures Material without rolling Quality/Adjective/suffixes (starter weapons, lab helpers).
        /// Combat procs come from MATERIAL BUILDS set counts, not per-item trigger stamps.
        /// </summary>
        public void ApplyAlwaysMaterialAndTrigger(Item item, string? itemRarity = null)
        {
            EnsureMaterial(item, itemRarity);
            MaterialTriggerMerge.ClearCatalogTriggerStamp(item);
            MaterialTriggerMerge.RemapLegacyMaterialOnItem(item);
            SyncMaterialPrefixTags(item);
            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
        }

        /// <summary>
        /// Forces a specific always-on Material (e.g. starter leather kit) without rolling the loot pool.
        /// </summary>
        public void ApplySpecificMaterial(Item item, string materialName, string? itemRarity = null)
        {
            if (item == null)
                return;

            string forced = ItemMaterialRules.RemapLegacyMaterial(materialName);
            if (string.IsNullOrWhiteSpace(forced))
                return;

            string rarityName = itemRarity?.Trim()
                ?? item.Rarity?.Trim()
                ?? "Common";

            item.Modifications.RemoveAll(m =>
                m != null && m.GetPrefixCategory() == ModificationPrefixCategory.Material);

            var template = (_dataCache.Modifications ?? new List<Modification>())
                .FirstOrDefault(m =>
                    m.GetPrefixCategory() == ModificationPrefixCategory.Material
                    && string.Equals(m.Name?.Trim(), forced, StringComparison.OrdinalIgnoreCase));

            Modification mod;
            if (template != null)
            {
                mod = CloneRolledModification(template, 0)!;
            }
            else
            {
                mod = new Modification
                {
                    Name = forced,
                    PrefixCategory = "MATERIAL",
                    ItemRank = rarityName,
                    Description = "Material",
                    MinValue = 0,
                    MaxValue = 0,
                    RolledValue = 0,
                    Tags = new List<string> { forced.ToLowerInvariant() }
                };
            }

            item.Modifications.Add(mod);
            item.Material = forced;
            if (string.IsNullOrWhiteSpace(item.Rarity))
                item.Rarity = rarityName;
            MaterialTriggerMerge.ClearCatalogTriggerStamp(item);
            MaterialTriggerMerge.RemapLegacyMaterialOnItem(item);
            SyncMaterialPrefixTags(item);
            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
            item.RecomputeAttributeRequirementsIncludingModifications();
        }

        private static List<ModificationPrefixCategory> SelectCategoriesForPrefixSlotCount(int count, Random rnd)
        {
            var all = new[]
            {
                ModificationPrefixCategory.Quality,
                ModificationPrefixCategory.Adjective
            };

            count = Math.Clamp(count, 0, 2);
            if (count <= 0)
                return new List<ModificationPrefixCategory>();

            if (count >= 2)
            {
                return new List<ModificationPrefixCategory>
                {
                    ModificationPrefixCategory.Quality,
                    ModificationPrefixCategory.Adjective
                };
            }

            return new List<ModificationPrefixCategory> { all[rnd.Next(all.Length)] };
        }

        /// <summary>
        /// Legacy API: roll N adjective-only modifications (tests). Only the first is kept if N&gt;1 to respect one adjective per item.
        /// </summary>
        public void ApplyModifications(Item item, int count, LootContext? context = null)
        {
            item.Modifications.RemoveAll(m => m.GetPrefixCategory() == ModificationPrefixCategory.Adjective);
            int rolls = Math.Max(0, Math.Min(count, 1));
            for (int i = 0; i < rolls; i++)
            {
                var mod = RollOneCategory(ModificationPrefixCategory.Adjective, item, context, 0);
                if (mod != null)
                    item.Modifications.Add(mod);
            }

            item.RecomputeAttributeRequirementsIncludingModifications();
        }

        private void ResolveRerollAdjectiveIfPresent(Item item, LootContext? context)
        {
            var reroll = item.Modifications.FirstOrDefault(m => m.Effect == "reroll");
            if (reroll == null)
                return;

            int bonus = (int)Math.Round(reroll.RolledValue);
            if (bonus <= 0)
                bonus = 3;

            item.Modifications.Remove(reroll);
            var replacement = RollOneCategory(ModificationPrefixCategory.Adjective, item, context, bonus);
            if (replacement != null)
                item.Modifications.Add(replacement);

            item.RecomputeAttributeRequirementsIncludingModifications();
        }

        private static void SyncMaterialPrefixTags(Item item)
        {
            var tags = GameDataTagHelper.NormalizeDistinct(item.Tags);
            foreach (var mod in item.Modifications)
            {
                if (mod == null)
                    continue;

                if (mod.Tags != null)
                {
                    foreach (var raw in mod.Tags)
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                            continue;
                        var tag = raw.Trim();
                        if (!GameDataTagHelper.HasTag(tags, tag))
                            tags.Add(tag);
                    }
                }

                if (mod.GetPrefixCategory() != ModificationPrefixCategory.Material || string.IsNullOrWhiteSpace(mod.Name))
                    continue;

                var materialTag = mod.Name.Trim().ToLowerInvariant();
                if (!GameDataTagHelper.HasTag(tags, materialTag))
                    tags.Add(materialTag);
            }
            item.Tags = tags;
        }

        private void AdjustRarityBasedOnBonuses(Item item, RarityData currentRarity)
        {
            string? requiredRarity = null;

            if (item.Modifications != null && item.Modifications.Count > 0)
            {
                foreach (var mod in item.Modifications)
                {
                    if (!string.IsNullOrEmpty(mod.ItemRank))
                    {
                        if (requiredRarity == null || IsRarityHigher(mod.ItemRank, requiredRarity))
                            requiredRarity = mod.ItemRank;
                    }
                }
            }

            if (item.StatBonuses != null && item.StatBonuses.Count > 0)
            {
                foreach (var statBonus in item.StatBonuses)
                {
                    if (!string.IsNullOrWhiteSpace(statBonus.Rarity))
                    {
                        string r = statBonus.Rarity.Trim();
                        if (requiredRarity == null || IsRarityHigher(r, requiredRarity))
                            requiredRarity = r;
                    }

                    if (statBonus.Name.Equals("of the Sage", StringComparison.OrdinalIgnoreCase))
                    {
                        if (requiredRarity == null || IsRarityHigher("Rare", requiredRarity))
                            requiredRarity = "Rare";
                    }
                }
            }

            if (requiredRarity != null && IsRarityHigher(requiredRarity, currentRarity.Name))
                item.Rarity = requiredRarity;
        }

        private bool IsRarityHigher(string rarity1, string rarity2)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            int index1 = IndexOfRarityName(rarityOrder, rarity1);
            int index2 = IndexOfRarityName(rarityOrder, rarity2);
            if (index1 < 0) index1 = 0;
            if (index2 < 0) index2 = 0;
            return index1 > index2;
        }

        private static int IndexOfRarityName(string[] order, string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return -1;
            string trimmed = name.Trim();
            for (int i = 0; i < order.Length; i++)
            {
                if (string.Equals(order[i], trimmed, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static int AffixTierOrderIndex(string? rarityName)
        {
            if (string.IsNullOrWhiteSpace(rarityName))
                return 0;
            string t = rarityName.Trim();
            for (int i = 0; i < AffixTierOrder.Length; i++)
            {
                if (AffixTierOrder[i].Equals(t, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        public void ApplyStatBonuses(Item item, int count)
        {
            if (_dataCache.StatBonuses == null || _dataCache.StatBonuses.Count == 0 || count <= 0)
                return;

            var tierSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in _dataCache.StatBonuses)
                tierSet.Add(string.IsNullOrWhiteSpace(s.Rarity) ? "Common" : s.Rarity.Trim());

            var usedAffixRarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < count; i++)
            {
                if (_itemRarityCapIndex >= 0)
                {
                    // Rarity-capped model: first line carries the item's rarity, later lines lean lower; no repeated stat.
                    var capped = PickStatBonusCapped(signature: i == 0, usedStats);
                    if (capped == null)
                        continue;
                    // Two Common suffixes fuse into one random Uncommon suffix ("of Agility of Vitality" → one Uncommon line).
                    if (TryFuseCommonSuffixes(item, capped, usedStats))
                        continue;
                    usedStats.Add(StatKey(capped));
                    item.StatBonuses.Add(capped.CloneForItemInstance());
                    continue;
                }

                var pick = PickStatBonusWithUniqueAffixRarity(tierSet, usedAffixRarities);
                if (pick == null)
                    continue;

                usedAffixRarities.Add(NormalizeStatBonusAffixRarity(pick.Rarity));
                var instance = pick.CloneForItemInstance();
                item.StatBonuses.Add(instance);
            }

            // Suffix Requirements (when authored on rolled rows) merge into the equip gate.
            item.RecomputeAttributeRequirementsIncludingModifications();
            AnimalTagHelper.SyncSuffixTags(item);
        }

        /// <summary>Item rarity index when the rarity-capped affix model is active; -1 = legacy model.</summary>
        private int _itemRarityCapIndex = -1;

        private static string StatKey(StatBonus s) => string.IsNullOrWhiteSpace(s.StatType) ? s.Name : s.StatType;

        /// <summary>
        /// Tier for a non-signature affix line under the rarity-capped model: 0..cap, each lower tier
        /// <see cref="LootSystemConfig.AffixTierFalloff"/>× more likely.
        /// </summary>
        private int RollCappedTierIndex(int cap)
        {
            double falloff = Math.Max(1.0, GameConfiguration.Instance?.LootSystem?.AffixTierFalloff ?? 3.0);
            double total = 0;
            var weights = new double[cap + 1];
            for (int t = 0; t <= cap; t++)
            {
                weights[t] = Math.Pow(falloff, cap - t);
                total += weights[t];
            }
            double roll = _random.NextDouble() * total;
            for (int t = 0; t <= cap; t++)
            {
                roll -= weights[t];
                if (roll < 0)
                    return t;
            }
            return 0;
        }

        private StatBonus? PickStatBonusCapped(bool signature, HashSet<string> usedStats)
        {
            int cap = Math.Min(_itemRarityCapIndex, AffixTierOrder.Length - 1);
            int start = signature ? cap : RollCappedTierIndex(cap);
            // Walk down from the chosen tier until a tier has an unused stat; never above the item's rarity.
            for (int t = start; t >= 0; t--)
            {
                var pool = FilterStatBonusesByAffixTier(_dataCache.StatBonuses!, AffixTierOrder[t])
                    .Where(s => !usedStats.Contains(StatKey(s)))
                    .ToList();
                if (pool.Count > 0)
                    return pool[_random.Next(pool.Count)];
            }
            return null;
        }

        /// <summary>
        /// When <paramref name="pick"/> is Common and the item already carries a Common suffix, removes that line and adds
        /// one random Uncommon suffix (unused stat) instead. Returns false when no fusion happened.
        /// </summary>
        internal bool TryFuseCommonSuffixes(Item item, StatBonus pick, HashSet<string> usedStats)
        {
            const string common = "Common";
            if (!NormalizeStatBonusAffixRarity(pick.Rarity).Equals(common, StringComparison.OrdinalIgnoreCase))
                return false;
            var existing = item.StatBonuses.FirstOrDefault(s =>
                NormalizeStatBonusAffixRarity(s.Rarity).Equals(common, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
                return false;

            usedStats.Remove(StatKey(existing));
            var pool = FilterStatBonusesByAffixTier(_dataCache.StatBonuses!, "Uncommon")
                .Where(s => !usedStats.Contains(StatKey(s)))
                .ToList();
            if (pool.Count == 0)
            {
                usedStats.Add(StatKey(existing));
                return false;
            }

            item.StatBonuses.Remove(existing);
            var fused = pool[_random.Next(pool.Count)];
            usedStats.Add(StatKey(fused));
            item.StatBonuses.Add(fused.CloneForItemInstance());
            return true;
        }

        /// <summary>At most one stat suffix per affix-line rarity; duplicate rolls promote up <see cref="AffixTierOrder"/>.</summary>
        private StatBonus? PickStatBonusWithUniqueAffixRarity(HashSet<string> tierSet, HashSet<string> usedAffixRarities)
        {
            string tierRoll = RollAffixLineRarityForPools(tierSet);
            int startIdx = AffixTierOrderIndex(tierRoll);
            if (startIdx < 0)
                startIdx = 0;

            for (int bump = 0; bump < AffixTierOrder.Length; bump++)
            {
                int idx = Math.Min(AffixTierOrder.Length - 1, startIdx + bump);
                string tryTier = AffixTierOrder[idx];
                var pool = FilterStatBonusesByAffixTier(_dataCache.StatBonuses, tryTier)
                    .Where(s => !usedAffixRarities.Contains(NormalizeStatBonusAffixRarity(s.Rarity)))
                    .ToList();

                // When the rolled tier has no data rows, mirror legacy behavior: try random tiers from the pool set.
                if (pool.Count == 0 && bump == 0)
                {
                    for (int attempt = 0; attempt < 8 && pool.Count == 0; attempt++)
                    {
                        string pickTier = tierSet.ElementAt(_random.Next(tierSet.Count));
                        pool = FilterStatBonusesByAffixTier(_dataCache.StatBonuses, pickTier)
                            .Where(s => !usedAffixRarities.Contains(NormalizeStatBonusAffixRarity(s.Rarity)))
                            .ToList();
                    }
                }

                if (pool.Count > 0)
                    return pool[_random.Next(pool.Count)];
            }

            // No free tier in walk order: any remaining rarity not yet used on this item.
            var fallback = _dataCache.StatBonuses
                .Where(s => !usedAffixRarities.Contains(NormalizeStatBonusAffixRarity(s.Rarity)))
                .ToList();
            return fallback.Count > 0 ? fallback[_random.Next(fallback.Count)] : null;
        }

        private static string NormalizeStatBonusAffixRarity(string? rarity) =>
            string.IsNullOrWhiteSpace(rarity) ? "Common" : rarity.Trim();

        public void ApplyActionBonuses(Item item, int count, LootContext? context = null)
        {
            var heroGearPool = GetHeroGearActionBonusPool();
            if (heroGearPool.Count == 0)
                return;

            WeaponType? heroWeaponType = GearActionPools.WeaponTypeForClass(context?.PlayerClass);
            for (int i = 0; i < count; i++)
            {
                var chosen = GearActionPools.Pick(item, heroGearPool, _random, heroWeaponType);
                if (chosen != null)
                    item.ActionBonuses.Add(chosen);
            }
        }

        private List<ActionBonus> GetHeroGearActionBonusPool()
        {
            if (_dataCache.ActionBonuses == null || _dataCache.ActionBonuses.Count == 0)
                return new List<ActionBonus>();
            // LootDataCache.LoadActionBonuses already excludes environment/enemy-tagged actions.
            return _dataCache.ActionBonuses
                .Where(ab => !string.IsNullOrEmpty(ab.Name))
                .ToList();
        }

        private Modification? RollOneCategory(ModificationPrefixCategory category, Item item, LootContext? context, int diceBonus)
        {
            if (_dataCache.Modifications == null || _dataCache.Modifications.Count == 0)
                return null;

            var basePool = _dataCache.Modifications
                .Where(m => m.GetPrefixCategory() == category)
                .ToList();

            if (basePool.Count == 0)
                return null;

            var tierSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in basePool)
                tierSet.Add(string.IsNullOrWhiteSpace(m.ItemRank) ? "Common" : m.ItemRank.Trim());

            string tier = _itemRarityCapIndex >= 0
                ? AffixTierOrder[RollCappedTierIndex(Math.Min(_itemRarityCapIndex, AffixTierOrder.Length - 1))]
                : RollAffixLineRarityForPools(tierSet);
            var pool = FilterModificationsByAffixTier(basePool, tier);
            // Capped model: if the rolled tier has no rows, step down (never up past the item's rarity).
            for (int t = AffixTierOrderIndex(tier) - 1; _itemRarityCapIndex >= 0 && pool.Count == 0 && t >= 0; t--)
                pool = FilterModificationsByAffixTier(basePool, AffixTierOrder[t]);
            for (int attempt = 0; attempt < 8 && pool.Count == 0; attempt++)
            {
                string pick = tierSet.ElementAt(_random.Next(tierSet.Count));
                pool = FilterModificationsByAffixTier(basePool, pick);
            }

            if (pool.Count == 0)
                return null;

            if (diceBonus > 0)
            {
                var highTier = pool.Where(m => m.DiceResult >= 12).ToList();
                if (highTier.Count > 0)
                    pool = highTier;
            }

            if (category == ModificationPrefixCategory.Adjective &&
                _modificationSelector != null &&
                context != null &&
                _random.NextDouble() < 0.70)
            {
                var favored = _modificationSelector.GetFavoredDiceResults(context);
                if (favored.Count > 0)
                {
                    var biased = pool.Where(m => favored.Contains(m.DiceResult)).ToList();
                    if (biased.Count > 0)
                        pool = biased;
                }
            }

            if (pool.Count == 0)
                return null;

            Modification template = pool[_random.Next(pool.Count)];
            return CloneRolledModification(template, diceBonus);
        }

        /// <summary>
        /// Weighted affix-line tier using <see cref="RarityData"/> rows present in <paramref name="tierNames"/>.
        /// Magic find ramps weight on higher tier indices up to <see cref="LootSystemConfig.AffixMagicFindMaxWeightBoost"/> at MF 100.
        /// </summary>
        private string RollAffixLineRarityForPools(HashSet<string> tierNames)
        {
            if (_dataCache.RarityData == null || _dataCache.RarityData.Count == 0)
                return tierNames.ElementAt(_random.Next(tierNames.Count));

            var rows = _dataCache.RarityData
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && tierNames.Contains(r.Name.Trim()))
                .Where(r => r.Weight > 0)
                .ToList();

            if (rows.Count == 0)
                return tierNames.ElementAt(_random.Next(tierNames.Count));

            double t = _affixMagicFind / 100.0;
            double maxBoost = GameConfiguration.Instance?.LootSystem?.AffixMagicFindMaxWeightBoost ?? 2.0;
            if (maxBoost < 1.0)
                maxBoost = 1.0;

            int maxIdx = rows.Max(r => AffixTierOrderIndex(r.Name));
            double total = 0;
            var weights = new double[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                int idx = AffixTierOrderIndex(rows[i].Name);
                double frac = maxIdx > 0 ? (double)idx / maxIdx : 0.0;
                double factor = 1.0 + t * (maxBoost - 1.0) * frac;
                weights[i] = Math.Max(0.0, rows[i].Weight) * factor;
                total += weights[i];
            }

            if (total <= 0)
            {
                total = rows.Sum(r => Math.Max(0.0, r.Weight));
                for (int i = 0; i < rows.Count; i++)
                    weights[i] = Math.Max(0.0, rows[i].Weight);
            }

            double roll = _random.NextDouble() * total;
            double acc = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                acc += weights[i];
                if (roll < acc)
                    return rows[i].Name.Trim();
            }

            return rows[^1].Name.Trim();
        }

        private static List<StatBonus> FilterStatBonusesByAffixTier(IReadOnlyList<StatBonus> all, string tier)
        {
            string t = tier.Trim();
            var list = new List<StatBonus>();
            foreach (var s in all)
            {
                string rowTier = string.IsNullOrWhiteSpace(s.Rarity) ? "Common" : s.Rarity.Trim();
                if (string.Equals(rowTier, t, StringComparison.OrdinalIgnoreCase))
                    list.Add(s);
            }

            return list;
        }

        private static List<Modification> FilterModificationsByAffixTier(IReadOnlyList<Modification> all, string tier)
        {
            string t = tier.Trim();
            var list = new List<Modification>();
            foreach (var m in all)
            {
                string rowTier = string.IsNullOrWhiteSpace(m.ItemRank) ? "Common" : m.ItemRank.Trim();
                if (string.Equals(rowTier, t, StringComparison.OrdinalIgnoreCase))
                    list.Add(m);
            }

            return list;
        }

        private Modification? CloneRolledModification(Modification template, int _)
        {
            var clone = new Modification
            {
                DiceResult = template.DiceResult,
                ItemRank = template.ItemRank,
                PrefixCategory = template.PrefixCategory,
                Name = template.Name,
                Description = template.Description,
                Effect = template.Effect,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                RolledValue = RollValueBetween(template.MinValue, template.MaxValue),
                StatusEffects = template.StatusEffects != null ? new List<string>(template.StatusEffects) : new List<string>(),
                Tags = template.Tags != null ? new List<string>(template.Tags) : null
            };
            if (template.AttributeRequirements != null && template.AttributeRequirements.Count > 0)
                clone.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int>(template.AttributeRequirements));
            return clone;
        }

        private double RollValueBetween(double minValue, double maxValue)
        {
            if (Math.Abs(minValue - maxValue) < 0.001)
                return minValue;
            return minValue + (_random.NextDouble() * (maxValue - minValue));
        }
    }
}
