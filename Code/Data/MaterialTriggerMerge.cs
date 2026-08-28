using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Applies a randomly chosen material trigger identity onto a generated item.
    /// Replaces main's <c>StatBonusTriggerMerge</c> / animal-suffix trigger ownership.
    /// Bundles are built from <see cref="MaterialTriggerCatalog"/> (not the Wave-2 seed stamp).
    /// </summary>
    public static class MaterialTriggerMerge
    {
        /// <summary>
        /// Appends one random identity from the item's material pool.
        /// Call <see cref="ClearCatalogTriggerStamp"/> first so material owns loot procs.
        /// </summary>
        public static void ApplyMaterialTrigger(Item item, Random? random = null)
        {
            if (item == null)
                return;

            random ??= Random.Shared;

            string material = ResolveMaterialName(item);
            if (!MaterialTriggerCatalog.TryGetPool(material, out var pool) || pool.Count == 0)
                return;

            var pick = pool[random.Next(pool.Count)];
            AppendDef(item, pick);
        }

        /// <summary>
        /// Remaps legacy Damascus→Iron on the item and its Material prefix. Does not re-roll procs.
        /// </summary>
        public static void RemapLegacyMaterialOnItem(Item item)
        {
            if (item == null)
                return;

            string remapped = ItemMaterialRules.RemapLegacyMaterial(item.Material);
            if (!string.IsNullOrWhiteSpace(remapped))
                item.Material = remapped;

            if (item.Modifications == null)
                return;
            foreach (var mod in item.Modifications)
            {
                if (mod == null)
                    continue;
                if (mod.GetPrefixCategory() == ModificationPrefixCategory.Material
                    && !string.IsNullOrWhiteSpace(mod.Name))
                {
                    string next = ItemMaterialRules.RemapLegacyMaterial(mod.Name);
                    if (!string.Equals(next, mod.Name, StringComparison.Ordinal))
                        mod.Name = next;
                }
            }
        }

        /// <summary>
        /// Remaps leftover Damascus material names. Does not re-roll a random pool proc.
        /// </summary>
        public static void RepairMissingMaterialTrigger(Item item, Random? random = null)
        {
            if (item == null)
                return;

            RemapLegacyMaterialOnItem(item);

            string material = ResolveMaterialName(item);
            if (string.IsNullOrWhiteSpace(material))
                return;

            if (string.IsNullOrWhiteSpace(item.Material))
                item.Material = ItemMaterialRules.RemapLegacyMaterial(material);

            // MATERIAL BUILDS owns combat triggers; drop catalog/pool stamps so they cannot fire.
            ClearCatalogTriggerStamp(item);
        }

        /// <summary>Prefer <see cref="Item.Material"/>; fall back to the Material prefix modification name.</summary>
        public static string ResolveMaterialName(Item item)
        {
            if (item == null)
                return "";

            if (!string.IsNullOrWhiteSpace(item.Material))
                return item.Material.Trim();

            var matMod = item.Modifications?.FirstOrDefault(m =>
                m != null && m.GetPrefixCategory() == ModificationPrefixCategory.Material);
            return matMod?.Name?.Trim() ?? "";
        }

        /// <summary>Clear catalog stamp procs so material owns gear procs for this item.</summary>
        public static void ClearCatalogTriggerStamp(Item item)
        {
            if (item == null)
                return;
            item.TriggerBundles = new List<ActionTriggerBundle>();
            item.EquipEffects = new List<ActionTriggerBundle>();
        }

        public static void AppendDef(Item item, MaterialTriggerDef def)
        {
            if (item == null)
                return;

            var bundle = ToBundle(def);
            bool equip = string.Equals(def.Channel?.Trim(), "equip", StringComparison.OrdinalIgnoreCase);
            if (equip)
            {
                item.EquipEffects ??= new List<ActionTriggerBundle>();
                if (!ContainsEquivalent(item.EquipEffects, bundle))
                    item.EquipEffects.Add(bundle);
            }
            else
            {
                item.TriggerBundles ??= new List<ActionTriggerBundle>();
                if (!ContainsEquivalent(item.TriggerBundles, bundle))
                    item.TriggerBundles.Add(bundle);
            }
        }

        public static ActionTriggerBundle ToBundle(MaterialTriggerDef def)
        {
            List<string>? filters = null;
            if (!string.IsNullOrWhiteSpace(def.Filters))
            {
                filters = new List<string>();
                foreach (var part in def.Filters.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string t = part.Trim();
                    if (t.Length > 0)
                        filters.Add(t);
                }

                if (filters.Count == 0)
                    filters = null;
            }

            return new ActionTriggerBundle
            {
                When = def.When ?? "",
                Count = "1",
                Scope = def.Scope ?? "",
                Mechanics = def.Mechanics ?? "",
                Value = def.Value,
                Filters = filters,
                IdentityName = def.TriggerName,
                Description = def.Description
            };
        }

        private static bool ContainsEquivalent(List<ActionTriggerBundle> list, ActionTriggerBundle bundle)
        {
            foreach (var existing in list)
            {
                if (existing == null)
                    continue;
                if (string.Equals(existing.When, bundle.When, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.Mechanics, bundle.Mechanics, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.Scope ?? "", bundle.Scope ?? "", StringComparison.OrdinalIgnoreCase)
                    && Nullable.Equals(existing.Value, bundle.Value))
                    return true;
            }

            return false;
        }
    }
}
