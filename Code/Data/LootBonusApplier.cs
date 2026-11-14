using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Material helpers for items built outside the item roller (starter gear, Action Lab, build-ladder tools):
    /// stamp a material prefix, its tags and the display name. Rolled loot goes through
    /// <see cref="Data.ItemGeneration.ItemRoller"/>.
    /// </summary>
    public class LootBonusApplier
    {
        private readonly LootDataCache _dataCache;
        private readonly Random _random;

        public LootBonusApplier(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
        }

        /// <summary>Market skill rules: a non-weapon drop may take one of the hero's three class materials.</summary>
        private string ApplyClassMaterialBias(string materialName, LootContext? context)
        {
            if (context?.ClassMaterialChance == null || context.ClassMaterialChance.Count == 0)
                return materialName;
            foreach (var kv in context.ClassMaterialChance)
            {
                if (kv.Value <= 0 || _random.NextDouble() >= kv.Value)
                    continue;
                if (ClassTags.WeaponOf(kv.Key) is WeaponType wt
                    && ItemMaterialRules.TryPickFamilyMaterial(wt, _random, out string family))
                    return family;
            }
            return materialName;
        }

        /// <summary>
        /// Ensures exactly one Material prefix. Weapons pick one of their family's three materials 90% of the time
        /// (<see cref="LootSystemConfig.WeaponFamilyMaterialShare"/>), else any set material; non-weapons pick any material.
        /// </summary>
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
                materialName = ItemMaterialRules.PickClassMaterial(item.WeaponType, _random);
            }
            else
            {
                var materialMods = (_dataCache.Modifications ?? new List<Modification>())
                    .Where(m => m.GetPrefixCategory() == ModificationPrefixCategory.Material);
                materialName = ItemMaterialRules.PickNonWeaponMaterial(materialMods, rarityName, _random);
                materialName = ApplyClassMaterialBias(materialName, context);
            }
            // Rolled first so the random stream matches an unforced run of the same seed.
            if (!string.IsNullOrWhiteSpace(Data.ItemGeneration.ItemRoller.ToolForcedMaterial))
                materialName = Data.ItemGeneration.ItemRoller.ToolForcedMaterial!;

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

        internal static void SyncMaterialPrefixTags(Item item)
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
                TriggerWhen = template.TriggerWhen,
                CrossWire = template.CrossWire,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                RolledValue = RollValueBetween(template.MinValue, template.MaxValue),
                StatusEffects = template.StatusEffects != null ? new List<string>(template.StatusEffects) : new List<string>(),
                Tags = template.Tags != null ? new List<string>(template.Tags) : null
            };
            if (template.AttributeRequirements != null && template.AttributeRequirements.Count > 0)
                clone.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int>(template.AttributeRequirements));
            // Rule prefixes: the card shows the rules with the rolled value.
            if (AffixTemplates.Is(clone, CrossWires.RuleEffect) && CrossWires.DescribeMod(clone) is { } ruleText)
                clone.Description = ruleText;
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
