using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Rebuild 2.1 (D0.7, D5.6): a Common is stats and a material only. It has no base modifier (starters included,
    /// D0.7) and no action; starter gear is the one exception for actions (it carries the class starter, D5.6).
    /// Every weapon still grants its type's required basic (<see cref="GearActionNames"/>), so a Common weapon never
    /// leaves the strip empty.
    /// <para>Save migration (Phase 10): a loaded Common keeps its stats and loses its modifier and its rolled actions
    /// (starters keep their action). Switch: <c>lootSystem.itemGeneration.commonsArePlain</c>.</para>
    /// </summary>
    public static class CommonItemRules
    {
        public static bool Enabled =>
            GameConfiguration.Instance?.LootSystem?.ItemGeneration?.CommonsArePlain ?? true;

        public static bool IsCommon(Item? item) =>
            item != null && (string.IsNullOrWhiteSpace(item.Rarity) || item.Rarity.Trim().Equals("Common", StringComparison.OrdinalIgnoreCase));

        public static bool IsStarter(Item? item) =>
            item?.Tags != null && item.Tags.Any(t => string.Equals(t, StarterCatalogItems.StarterTag, StringComparison.OrdinalIgnoreCase));

        /// <summary>True when <paramref name="item"/> must stay plain: a Common while the rule is on.</summary>
        public static bool MustBePlain(Item? item) => Enabled && IsCommon(item);

        /// <summary>
        /// Strips what a Common may not carry: the base modifier always, the gear action and rolled actions unless it is
        /// starter gear. Returns true when anything changed.
        /// </summary>
        public static bool MakePlain(Item? item)
        {
            if (item == null || !MustBePlain(item))
                return false;
            bool changed = false;
            if (!string.IsNullOrWhiteSpace(item.ImplicitRule) || !string.IsNullOrWhiteSpace(item.Implicit))
            {
                item.ImplicitRule = "";
                item.Implicit = "";
                changed = true;
            }
            if (!IsStarter(item))
            {
                if (!string.IsNullOrEmpty(item.GearAction))
                {
                    item.GearAction = null;
                    changed = true;
                }
                if (item.ActionBonuses.Count > 0)
                {
                    item.ActionBonuses.Clear();
                    changed = true;
                }
                item.MinGeneratedActionBonuses = 0;
            }
            return changed;
        }
    }
}
