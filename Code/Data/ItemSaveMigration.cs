using System;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Brings a saved or freshly rolled item up to the current rules: legacy material names, legacy implicit triggers
    /// (now base modifiers, 2026-11-15) and base modifiers whose catalog rule changed shape (2026-11-17). Items carry no
    /// trigger bundles since 2026-11-18 (the item-trigger path was removed; old saves' bundles are ignored on load).
    /// </summary>
    public static class ItemSaveMigration
    {
        /// <summary>Everything a loaded item needs: material remap, legacy implicit, catalog modifier refresh.</summary>
        public static void Migrate(Item item)
        {
            if (item == null)
                return;
            RemapLegacyMaterialOnItem(item);
            MigrateLegacyImplicit(item);
            RefreshCatalogModifier(item);
            string material = ResolveMaterialName(item);
            if (!string.IsNullOrWhiteSpace(material) && string.IsNullOrWhiteSpace(item.Material))
                item.Material = ItemMaterialRules.RemapLegacyMaterial(material);
        }

        /// <summary>
        /// Remaps legacy material names (Damascus→Iron, MITHIRL→Mithril, IRON→Iron title case) on the item and its Material
        /// prefix, then refreshes the composed display name when anything changed.
        /// </summary>
        public static void RemapLegacyMaterialOnItem(Item item)
        {
            if (item == null)
                return;

            bool changed = false;
            string remapped = ItemMaterialRules.RemapLegacyMaterial(item.Material);
            if (!string.IsNullOrWhiteSpace(remapped))
            {
                changed = !string.Equals(remapped, item.Material, StringComparison.Ordinal);
                item.Material = remapped;
            }

            if (item.Modifications != null)
            {
                foreach (var mod in item.Modifications)
                {
                    if (mod == null || mod.GetPrefixCategory() != ModificationPrefixCategory.Material || string.IsNullOrWhiteSpace(mod.Name))
                        continue;
                    string next = ItemMaterialRules.RemapLegacyMaterial(mod.Name);
                    if (!string.Equals(next, mod.Name, StringComparison.Ordinal))
                    {
                        mod.Name = next;
                        changed = true;
                    }
                }
            }

            if (changed)
                item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
        }

        /// <summary>
        /// Base implicits are modifiers now (<see cref="Item.ImplicitRule"/>, 2026-11-15). A saved item that still names an
        /// implicit trigger gets its base's catalog modifier instead (values stable per item name and base); a base no longer
        /// in the catalog rolls one from BaseModifiers.json.
        /// </summary>
        public static void MigrateLegacyImplicit(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Implicit))
                return;
            if (string.IsNullOrWhiteSpace(item.ImplicitRule))
            {
                int seed = Seed(item);
                var template = RPGGame.Data.ItemGeneration.BaseModifierRoller.CatalogModifierFor(item.BaseName);
                if (RPGGame.Data.ItemGeneration.BaseModifierRoller.FromCatalog(template, new Random(seed)) is { } fromCatalog)
                    item.ImplicitRule = fromCatalog.Rule;
                else
                {
                    double perLevel = GameConfiguration.Instance?.LootSystem?.ItemGeneration?.ImplicitPercentPerItemLevel ?? 0.01;
                    if (RPGGame.Data.ItemGeneration.BaseModifierRoller.Roll(item.ItemClass, item.Level, new Random(seed), perLevel) is { } rolled)
                        item.ImplicitRule = rolled.Rule;
                }
            }
            item.Implicit = "";
        }

        /// <summary>
        /// A saved item whose base modifier no longer matches its base's catalog rule (the 2026-11-17 pass replaced the flat
        /// tier 1-2 "+n% with class actions" lines with build rules) re-rolls it from the catalog. Only the rule's shape is
        /// compared (numbers ignored), so an item already on the current rule keeps its rolled values.
        /// </summary>
        public static void RefreshCatalogModifier(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ImplicitRule))
                return;
            var template = RPGGame.Data.ItemGeneration.BaseModifierRoller.CatalogModifierFor(item.BaseName);
            if (string.IsNullOrWhiteSpace(template) || RuleShape(template) == RuleShape(item.ImplicitRule))
                return;
            if (RPGGame.Data.ItemGeneration.BaseModifierRoller.FromCatalog(template, new Random(Seed(item))) is { } fromCatalog)
                item.ImplicitRule = fromCatalog.Rule;
        }

        private static int Seed(Item item)
        {
            int seed = 17;
            foreach (char ch in (item.Name ?? "") + "|" + item.BaseName)
                seed = unchecked(seed * 31 + ch);
            return seed;
        }

        private static readonly System.Text.RegularExpressions.Regex RangeOrNumber =
            new(@"\{[^}]*\}|-?[0-9]+(?:\.[0-9]+)?", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>A modifier rule with every value range / number replaced by <c>N</c> and whitespace collapsed.</summary>
        internal static string RuleShape(string rule) =>
            string.Join(" ", RangeOrNumber.Replace(rule ?? "", "N").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();

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
    }
}
