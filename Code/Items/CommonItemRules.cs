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
            // Mechanic prefixes (cross-wires, rules, listeners, affix templates) are not stats (D0.7). Older Commons
            // rolled them from the material pool; a loaded one keeps its material and stat lines and gets its name back.
            var mechanics = item.Modifications.Where(IsMechanic).ToList();
            if (mechanics.Count > 0)
            {
                int slotsLost = mechanics
                    .Where(m => string.Equals(m.Effect, "EXTRASLOTS", StringComparison.OrdinalIgnoreCase))
                    .Sum(m => Math.Max(0, (int)Math.Round(m.RolledValue)));
                item.Modifications.RemoveAll(IsMechanic);
                item.ExtraActionSlots = Math.Max(0, item.ExtraActionSlots - slotsLost);
                foreach (var m in mechanics)
                    item.Name = DropWord(item.Name, m.Name);
                changed = true;
            }
            return changed;
        }

        /// <summary>The item name without a stripped line's keyword ("Dour Bone Club" → "Bone Club").</summary>
        private static string DropWord(string? name, string? word)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(word))
                return name ?? "";
            string pattern = @"(?<![\w'-])" + System.Text.RegularExpressions.Regex.Escape(word.Trim()) + @"(?![\w'-])";
            string result = System.Text.RegularExpressions.Regex.Replace(name, pattern, "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s{2,}", " ").Trim();
            return result.Length > 0 ? result : name;
        }

        private static readonly string[] MechanicEffects =
        {
            CrossWires.AffixEffect, CrossWires.RuleEffect, ListenerAffix.EffectName,
            AffixTemplates.Tune, AffixTemplates.StoreCondition, AffixTemplates.CountsDouble,
            AffixTemplates.Opener, AffixTemplates.Finisher, AffixTemplates.BloodPrice
        };

        /// <summary>
        /// True for a prefix that is a mechanic rather than a stat: a material- or mechanic-pool line, or a rule /
        /// listener / affix-template effect. The material prefix itself (Bone, Iron…) and Quality are never mechanics.
        /// </summary>
        public static bool IsMechanic(Modification? mod)
        {
            if (mod == null)
                return false;
            var category = mod.GetPrefixCategory();
            if (category == ModificationPrefixCategory.Material || category == ModificationPrefixCategory.Quality)
                return false;
            if (mod.ModLine is { } line
                && (line.Pool == Data.ItemGeneration.ModPools.Mechanic || line.Pool == Data.ItemGeneration.ModPools.Material))
                return true;
            string effect = mod.Effect?.Trim() ?? "";
            return MechanicEffects.Any(e => string.Equals(e, effect, StringComparison.OrdinalIgnoreCase));
        }
    }
}
