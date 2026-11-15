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

            random ??= RandomUtility.Ambient;

            string material = ResolveMaterialName(item);
            if (!MaterialTriggerCatalog.TryGetPool(material, out var pool) || pool.Count == 0)
                return;

            var pick = pool[random.Next(pool.Count)];
            AppendDef(item, pick);
        }

        /// <summary>
        /// Remaps legacy material names (Damascus→Iron, MITHIRL→Mithril, IRON→Iron title case)
        /// on the item and its Material prefix, then refreshes the composed display name when anything changed.
        /// Does not re-roll procs.
        /// </summary>
        public static void RemapLegacyMaterialOnItem(Item item)
        {
            if (item == null)
                return;

            bool changed = false;
            string remapped = ItemMaterialRules.RemapLegacyMaterial(item.Material);
            if (!string.IsNullOrWhiteSpace(remapped)
                && !string.Equals(remapped, item.Material, StringComparison.Ordinal))
            {
                item.Material = remapped;
                changed = true;
            }
            else if (!string.IsNullOrWhiteSpace(remapped))
            {
                item.Material = remapped;
            }

            if (item.Modifications != null)
            {
                foreach (var mod in item.Modifications)
                {
                    if (mod == null)
                        continue;
                    if (mod.GetPrefixCategory() == ModificationPrefixCategory.Material
                        && !string.IsNullOrWhiteSpace(mod.Name))
                    {
                        string next = ItemMaterialRules.RemapLegacyMaterial(mod.Name);
                        if (!string.Equals(next, mod.Name, StringComparison.Ordinal))
                        {
                            mod.Name = next;
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
                item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
        }

        /// <summary>
        /// Remaps leftover Damascus material names. Does not re-roll a random pool proc.
        /// </summary>
        public static void RepairMissingMaterialTrigger(Item item, Random? random = null)
        {
            if (item == null)
                return;

            RemapLegacyMaterialOnItem(item);
            MigrateLegacyImplicit(item);

            string material = ResolveMaterialName(item);
            if (string.IsNullOrWhiteSpace(material))
                return;

            if (string.IsNullOrWhiteSpace(item.Material))
                item.Material = ItemMaterialRules.RemapLegacyMaterial(material);

            // Materials no longer add a trigger to the piece (2026-11-15): drop every catalog / material-pool stamp.
            // Rolled affix lines live in Modifications.
            ClearCatalogTriggerStamp(item);
        }

        /// <summary>
        /// Base implicits are modifiers now (<see cref="Item.ImplicitRule"/>, 2026-11-15). A saved item that still names an
        /// implicit trigger loses that trigger and gets its base's catalog modifier instead (values stable per item name and
        /// base); a base no longer in the catalog rolls one from BaseModifiers.json.
        /// </summary>
        public static void MigrateLegacyImplicit(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Implicit))
                return;
            string legacy = item.Implicit;
            bool IsLegacy(ActionTriggerBundle? b) =>
                b != null && string.Equals(b.IdentityName, legacy, StringComparison.OrdinalIgnoreCase);
            item.TriggerBundles = (item.TriggerBundles ?? new List<ActionTriggerBundle>()).Where(b => !IsLegacy(b)).ToList();
            item.EquipEffects = (item.EquipEffects ?? new List<ActionTriggerBundle>()).Where(b => !IsLegacy(b)).ToList();
            if (string.IsNullOrWhiteSpace(item.ImplicitRule))
            {
                int seed = 17;
                foreach (char ch in (item.Name ?? "") + "|" + item.BaseName)
                    seed = unchecked(seed * 31 + ch);
                var (template, name) = RPGGame.Data.ItemGeneration.BaseModifierRoller.CatalogModifierFor(item.BaseName);
                if (RPGGame.Data.ItemGeneration.BaseModifierRoller.FromCatalog(template, new Random(seed)) is { } fromCatalog)
                {
                    item.ImplicitRule = fromCatalog.Rule;
                    item.ImplicitName = name?.Trim() ?? "";
                }
                else
                {
                    double perLevel = GameConfiguration.Instance?.LootSystem?.ItemGeneration?.ImplicitPercentPerItemLevel ?? 0.01;
                    if (RPGGame.Data.ItemGeneration.BaseModifierRoller.Roll(item.ItemClass, item.Level, new Random(seed), perLevel) is { } rolled)
                        item.ImplicitRule = rolled.Rule;
                }
            }
            item.Implicit = "";
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

        /// <summary>Clear catalog stamp procs so material owns gear procs for this item. The base item's implicit
        /// (<see cref="Item.Implicit"/>) is kept.</summary>
        public static void ClearCatalogTriggerStamp(Item item)
        {
            if (item == null)
                return;
            item.TriggerBundles = (item.TriggerBundles ?? new List<ActionTriggerBundle>()).Where(b => IsImplicit(item, b)).ToList();
            item.EquipEffects = (item.EquipEffects ?? new List<ActionTriggerBundle>()).Where(b => IsImplicit(item, b)).ToList();
        }

        /// <summary>True for the bundle that carries the base item's implicit.</summary>
        public static bool IsImplicit(Item item, ActionTriggerBundle? bundle) =>
            bundle != null && !string.IsNullOrWhiteSpace(item.Implicit)
            && string.Equals(bundle.IdentityName, item.Implicit, StringComparison.OrdinalIgnoreCase);

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
