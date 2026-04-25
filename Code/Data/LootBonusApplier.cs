using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Applies bonuses, modifications, and stat adjustments to items
    /// Handles stat bonuses, action bonuses, and prefix slots (Adjective / Material / Quality) based on rarity
    /// </summary>
    public class LootBonusApplier
    {
        private readonly LootDataCache _dataCache;
        private readonly Random _random;
        private readonly LootModificationSelector? _modificationSelector;
        private readonly LootActionSelector? _actionSelector;

        public LootBonusApplier(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
            _modificationSelector = new LootModificationSelector(random);
            _actionSelector = new LootActionSelector(random);
        }

        /// <summary>
        /// Applies all bonuses to an item based on its rarity
        /// </summary>
        public void ApplyBonuses(Item item, RarityData rarity, LootContext? context = null)
        {
            string rarityName = rarity.Name?.Trim() ?? "Common";

            item.StatBonuses.Clear();
            item.ActionBonuses.Clear();

            var rule = ItemAffixByRaritySettings.GetResolvedAffixRule(
                rarityName,
                rarity,
                GameConfiguration.Instance?.ItemAffixByRarity);
            ItemAffixByRaritySettings.RollAffixCounts(_random, rule, out int prefixSlots, out int statSuffixes, out int actionBonuses);

            ApplyStatBonuses(item, statSuffixes);
            ApplyActionBonuses(item, actionBonuses, context);
            ApplyPrefixSlots(item, prefixSlots, context);

            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
            AdjustRarityBasedOnBonuses(item, rarity);
            ResolveRerollAdjectiveIfPresent(item, context);
            AdjustRarityBasedOnBonuses(item, rarity);
            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
        }

        /// <summary>
        /// Fills prefix categories (Quality / Adjective / Material) with at most one modification each.
        /// </summary>
        public void ApplyPrefixSlots(Item item, string rarityName, LootContext? context = null) =>
            ApplyPrefixSlots(item, ItemAffixByRaritySettings.DefaultPrefixSlotsForRarity(rarityName), context);

        /// <summary>
        /// Fills <paramref name="prefixSlotCount"/> prefix categories (0–3) with at most one modification each.
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
        }

        private static List<ModificationPrefixCategory> SelectCategoriesForPrefixSlotCount(int count, Random rnd)
        {
            var all = new[]
            {
                ModificationPrefixCategory.Quality,
                ModificationPrefixCategory.Adjective,
                ModificationPrefixCategory.Material
            };

            count = Math.Clamp(count, 0, 3);
            if (count <= 0)
                return new List<ModificationPrefixCategory>();

            if (count >= 3)
            {
                return new List<ModificationPrefixCategory>
                {
                    ModificationPrefixCategory.Quality,
                    ModificationPrefixCategory.Adjective,
                    ModificationPrefixCategory.Material
                };
            }

            if (count == 1)
                return new List<ModificationPrefixCategory> { all[rnd.Next(all.Length)] };

            return all.OrderBy(_ => rnd.Next()).Take(2).ToList();
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
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            int index1 = Array.IndexOf(rarityOrder, rarity1);
            int index2 = Array.IndexOf(rarityOrder, rarity2);
            if (index1 < 0) index1 = 0;
            if (index2 < 0) index2 = 0;
            return index1 > index2;
        }

        public void ApplyStatBonuses(Item item, int count)
        {
            if (_dataCache.StatBonuses != null && _dataCache.StatBonuses.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var statBonus = _dataCache.StatBonuses[_random.Next(_dataCache.StatBonuses.Count)];
                    item.StatBonuses.Add(statBonus);
                }
            }
        }

        public void ApplyActionBonuses(Item item, int count, LootContext? context = null)
        {
            if (_dataCache.ActionBonuses == null || _dataCache.ActionBonuses.Count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                if (_actionSelector != null && context != null)
                {
                    var contextualAction = _actionSelector.SelectAction(context, item);
                    if (!string.IsNullOrEmpty(contextualAction))
                    {
                        var actionBonus = _dataCache.ActionBonuses.FirstOrDefault(
                            a => a.Name.Equals(contextualAction, StringComparison.OrdinalIgnoreCase));
                        if (actionBonus != null)
                        {
                            item.ActionBonuses.Add(actionBonus);
                            continue;
                        }
                    }
                }

                var actionBonuses = _dataCache.ActionBonuses;
                if (actionBonuses.Count == 0) continue;
                int totalWeight = actionBonuses.Sum(a => Math.Max(1, a.Weight));
                int roll = _random.Next(totalWeight);
                int accumulated = 0;
                ActionBonus? chosen = null;
                foreach (var ab in actionBonuses)
                {
                    accumulated += Math.Max(1, ab.Weight);
                    if (roll < accumulated) { chosen = ab; break; }
                }
                if (chosen != null)
                    item.ActionBonuses.Add(chosen);
                else
                    item.ActionBonuses.Add(actionBonuses[actionBonuses.Count - 1]);
            }
        }

        private Modification? RollOneCategory(ModificationPrefixCategory category, Item item, LootContext? context, int diceBonus)
        {
            if (_dataCache.Modifications == null || _dataCache.Modifications.Count == 0)
                return null;

            var pool = _dataCache.Modifications
                .Where(m => m.GetPrefixCategory() == category)
                .Where(m => ItemPrefixHelper.MeetsMinimumItemRank(item.Rarity, m.ItemRank))
                .ToList();

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

            Modification template = pool[_random.Next(pool.Count)];
            return CloneRolledModification(template, diceBonus);
        }

        private Modification? CloneRolledModification(Modification template, int _)
        {
            return new Modification
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
                StatusEffects = template.StatusEffects != null ? new List<string>(template.StatusEffects) : new List<string>()
            };
        }

        private double RollValueBetween(double minValue, double maxValue)
        {
            if (Math.Abs(minValue - maxValue) < 0.001)
                return minValue;
            return minValue + (_random.NextDouble() * (maxValue - minValue));
        }
    }
}
