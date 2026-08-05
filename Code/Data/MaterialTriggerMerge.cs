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

            string material = item.Material;
            if (string.IsNullOrWhiteSpace(material))
            {
                var matMod = item.Modifications?.FirstOrDefault(m =>
                    m != null && m.GetPrefixCategory() == ModificationPrefixCategory.Material);
                material = matMod?.Name?.Trim() ?? "";
            }

            if (!MaterialTriggerCatalog.TryGetPool(material, out var pool) || pool.Count == 0)
                return;

            var pick = pool[random.Next(pool.Count)];
            AppendDef(item, pick);
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
                Filters = filters
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
