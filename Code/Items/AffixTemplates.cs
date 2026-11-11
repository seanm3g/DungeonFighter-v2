using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Item affix templates from Documentation/BUILD_SYSTEM_CONDENSED.md §5, as prefix <see cref="Modification"/>
    /// effects (rolled value in <see cref="Modification.RolledValue"/>):
    /// <list type="bullet">
    /// <item><c>LISTEN</c> (template 1): see <see cref="ListenerAffix"/>.</item>
    /// <item><c>TUNE</c> (2): +n to every mint of this piece's material.</item>
    /// <item><c>STORECOND</c> (4): +n% damage while this piece's material bank is at least
    /// <see cref="CombatConfig.AffixConditionStoreThreshold"/>.</item>
    /// <item><c>COUNTSDOUBLE</c> (5): this piece counts twice toward its material's set.</item>
    /// <item><c>OPENER</c> / <c>FINISHER</c> (8): +n% damage on the first / last action of your strip.</item>
    /// <item><c>BLOODPRICE</c> (10): each of your actions costs <see cref="CombatConfig.AffixBloodPriceHpPercent"/>% max HP
    /// (raises ONSELFCOST), +n% damage.</item>
    /// </list>
    /// On-hit (3), Tilt (6), Grant (7) and Carry (9) already exist as status prefixes, stat prefixes, rolled gear
    /// actions and dungeon-scoped banks. Condition-band (11) waits on an item-condition design.
    /// </summary>
    public static class AffixTemplates
    {
        public const string Tune = "TUNE";
        public const string StoreCondition = "STORECOND";
        public const string CountsDouble = "COUNTSDOUBLE";
        public const string Opener = "OPENER";
        public const string Finisher = "FINISHER";
        public const string BloodPrice = "BLOODPRICE";

        public static bool Is(Modification? mod, string effect) =>
            mod != null && string.Equals(mod.Effect?.Trim(), effect, StringComparison.OrdinalIgnoreCase);

        private static string MaterialOf(Item item) =>
            MaterialBuildData.CanonicalMaterialName(ItemMaterialRules.RemapLegacyMaterial(item.Material));

        private static IEnumerable<(Item Item, Modification Mod)> Equipped(Character hero, string effect)
        {
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item?.Modifications == null)
                    continue;
                foreach (var mod in item.Modifications)
                    if (Is(mod, effect))
                        yield return (item, mod);
            }
        }

        private static int Value(Modification mod) => Math.Max(1, (int)Math.Round(mod.RolledValue > 0 ? mod.RolledValue : mod.MinValue));

        /// <summary>TUNE: extra currency per mint of <paramref name="keyword"/> from pieces of that material.</summary>
        public static int MintBonus(Character hero, string keyword)
        {
            var build = MaterialBuildsLoader.FindByKeyword(keyword);
            if (build == null)
                return 0;
            return Equipped(hero, Tune)
                .Where(e => string.Equals(MaterialOf(e.Item), build.Material, StringComparison.OrdinalIgnoreCase))
                .Sum(e => Value(e.Mod));
        }

        /// <summary>COUNTSDOUBLE: extra set count for <paramref name="material"/> (each such piece counts once more).</summary>
        public static int ExtraSetCount(Character hero, string material) =>
            Equipped(hero, CountsDouble).Count(e => string.Equals(MaterialOf(e.Item), material, StringComparison.OrdinalIgnoreCase));

        /// <summary>STORECOND + OPENER + FINISHER + BLOODPRICE: outgoing damage percent for this action.</summary>
        public static double OutgoingDamagePercent(Character hero, Action? action)
        {
            double pct = 0;
            int threshold = Math.Max(1, GameConfiguration.Instance.Combat.AffixConditionStoreThreshold);
            foreach (var (item, mod) in Equipped(hero, StoreCondition))
            {
                string? keyword = MaterialBuildsLoader.FindByMaterial(MaterialOf(item))?.Keyword;
                if (!string.IsNullOrWhiteSpace(keyword) && hero.Effects.GetMaterialKeyword(keyword) >= threshold)
                    pct += Value(mod);
            }

            if (action != null)
            {
                var strip = hero.ComboSequence;
                if (strip != null && strip.Count > 0)
                {
                    bool isOpener = string.Equals(strip[0]?.Name, action.Name, StringComparison.OrdinalIgnoreCase);
                    bool isFinisher = string.Equals(strip[^1]?.Name, action.Name, StringComparison.OrdinalIgnoreCase);
                    if (isOpener)
                        pct += Equipped(hero, Opener).Sum(e => Value(e.Mod));
                    if (isFinisher)
                        pct += Equipped(hero, Finisher).Sum(e => Value(e.Mod));
                }
            }

            pct += Equipped(hero, BloodPrice).Sum(e => Value(e.Mod));
            return pct;
        }

        /// <summary>BLOODPRICE: pays the per-action HP cost of each such piece (never the last HP). Returns HP paid.</summary>
        public static int PayActionCosts(Character hero)
        {
            int pieces = Equipped(hero, BloodPrice).Count();
            if (pieces == 0)
                return 0;
            double pct = GameConfiguration.Instance.Combat.AffixBloodPriceHpPercent * pieces;
            int hp = (int)Math.Round(hero.GetEffectiveMaxHealth() * pct / 100.0);
            hp = Math.Min(hp, Math.Max(0, hero.CurrentHealth - 1));
            if (hp <= 0)
                return 0;
            hero.TakeDamage(hp);
            HeroEventHub.OnSelfCost(hero, "HP");
            return hp;
        }

        /// <summary>Player-facing line for a template prefix; null for other prefixes.</summary>
        public static string? Describe(Modification mod, Item? item = null)
        {
            if (ListenerAffix.IsListener(mod))
                return ListenerAffix.Describe(mod, item);
            string n = Value(mod).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string material = item != null ? MaterialOf(item) : "its material";
            return mod.Effect?.Trim().ToUpperInvariant() switch
            {
                Tune => $"+{n} to each {material} mint",
                StoreCondition => $"+{n}% damage while {material}'s currency is {GameConfiguration.Instance.Combat.AffixConditionStoreThreshold}+",
                CountsDouble => $"Counts twice toward the {material} set",
                Opener => $"+{n}% damage on your strip's opener",
                Finisher => $"+{n}% damage on your strip's finisher",
                BloodPrice => $"Each action costs {GameConfiguration.Instance.Combat.AffixBloodPriceHpPercent}% max HP, +{n}% damage",
                _ => null
            };
        }
    }
}
