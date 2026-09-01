using System;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Stamps always-on Material onto lab-built gear (same contract as loot / starter weapons).
    /// </summary>
    internal static class ActionLabGearMaterial
    {
        /// <summary>
        /// If the item already has a Material-category prefix, that name wins; otherwise
        /// <see cref="LootBonusApplier.ApplyAlwaysMaterialAndTrigger"/> assigns the class ladder / pool material.
        /// </summary>
        public static void Stamp(Item item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var selected = item.Modifications?
                .LastOrDefault(m => m != null && m.GetPrefixCategory() == ModificationPrefixCategory.Material);
            if (selected != null && !string.IsNullOrWhiteSpace(selected.Name))
            {
                item.Material = selected.Name.Trim();
                MaterialTriggerMerge.ClearCatalogTriggerStamp(item);
                MaterialTriggerMerge.RemapLegacyMaterialOnItem(item);
                item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
                return;
            }

            var applier = new LootBonusApplier(LootDataCache.Load(), Random.Shared);
            applier.ApplyAlwaysMaterialAndTrigger(item, item.Rarity);
        }
    }
}
