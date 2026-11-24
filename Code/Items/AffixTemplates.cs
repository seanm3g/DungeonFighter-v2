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

        internal static IEnumerable<(Item Item, Modification Mod)> Equipped(Character hero, string effect)
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

        /// <summary>
        /// The template as item rules (2026-11-18, one rule engine): they resolve with the piece's other rules in owner
        /// order. TUNE = <c>WHILE -> MINTBONUS material n</c>; STORECOND = <c>WHILE IF KEYWORD&gt;=threshold -> DAMAGE n%</c>;
        /// COUNTSDOUBLE = <c>WHILE -> SETCOUNT n</c>; OPENER = <c>WHILE IF SLOT=1 -> DAMAGE n%</c>; FINISHER =
        /// <c>WHILE IF SLOTSLEFT=0 -> DAMAGE n%</c>; BLOODPRICE = <c>WHILE -> DAMAGE n%</c> (its HP cost is
        /// <see cref="PayActionCosts"/>); LISTEN = a mint of the piece's currency on its rolled event.
        /// </summary>
        public static IEnumerable<CrossWireRule> Rules(Item item, Modification mod)
        {
            string effect = mod.Effect?.Trim().ToUpperInvariant() ?? "";
            if (effect.Length == 0)
                yield break;
            string n = Value(mod).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string material = MaterialOf(item);
            string source = item.Name ?? "";
            if (effect == ListenerAffix.EffectName)
            {
                var build = material.Length > 0 ? MaterialBuildsLoader.FindByMaterial(material) : null;
                if (build == null || string.IsNullOrWhiteSpace(build.Keyword) || string.IsNullOrWhiteSpace(mod.TriggerWhen))
                    yield break;
                yield return new CrossWireRule
                {
                    When = mod.TriggerWhen.Trim(),
                    Verb = "ADD",
                    Store = build.Keyword,
                    Amount = Math.Max(1.0, mod.RolledValue),
                    Source = source,
                    Text = $"{mod.TriggerWhen} -> ADD {build.Keyword} (Listening)",
                    SetBuild = build,
                    Listener = true
                };
                yield break;
            }
            string? text = effect switch
            {
                Tune when material.Length > 0 => $"WHILE -> MINTBONUS {material} {n}",
                StoreCondition => MaterialBuildsLoader.FindByMaterial(material)?.Keyword is { Length: > 0 } keyword
                    ? $"WHILE IF {keyword}>={Math.Max(1, GameConfiguration.Instance.Combat.AffixConditionStoreThreshold)} -> DAMAGE {n}%"
                    : null,
                CountsDouble => $"WHILE -> SETCOUNT {n}",
                Opener => $"WHILE IF SLOT=1 -> DAMAGE {n}%",
                Finisher => $"WHILE IF SLOTSLEFT=0 -> DAMAGE {n}%",
                BloodPrice => $"WHILE -> DAMAGE {n}%",
                _ => null
            };
            if (text != null && CrossWires.RuleFromText(text, source) is { } rule)
                yield return rule;
        }

        /// <summary>Outgoing damage percent from passive DAMAGE rules (affix templates included) for this action.</summary>
        /// <param name="target">The defender, for passive rule conditions (TARGET / PREFIX); default the current target.</param>
        public static double OutgoingDamagePercent(Character hero, Action? action, Actor? target = null) =>
            CrossWires.Passive(hero, "DAMAGE", target, action);

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

        /// <summary>
        /// Stat-list row (label + signed value) for TUNE / STORECOND / COUNTSDOUBLE / OPENER / FINISHER / BLOODPRICE, so
        /// tooltips read "Charged — Damage while Celestial 5+: +9%" instead of the raw template id. False for other mods.
        /// </summary>
        public static bool TryStatLine(Modification mod, Item? item, out string label, out string value)
        {
            label = "";
            value = "";
            string effect = mod.Effect?.Trim().ToUpperInvariant() ?? "";
            string n = Value(mod).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string material = item != null && !string.IsNullOrWhiteSpace(item.Material) ? MaterialOf(item) : "material";
            switch (effect)
            {
                case Tune:
                    label = $"{material} mint bonus";
                    value = $"+{n}";
                    return true;
                case StoreCondition:
                    label = $"Damage while {material} currency {GameConfiguration.Instance.Combat.AffixConditionStoreThreshold}+";
                    value = $"+{n}%";
                    return true;
                case CountsDouble:
                    label = $"{material} set count";
                    value = $"+{n}";
                    return true;
                case Opener:
                    label = "Opener damage";
                    value = $"+{n}%";
                    return true;
                case Finisher:
                    label = "Finisher damage";
                    value = $"+{n}%";
                    return true;
                case BloodPrice:
                    label = $"Damage (each action costs {GameConfiguration.Instance.Combat.AffixBloodPriceHpPercent}% max HP)";
                    value = $"+{n}%";
                    return true;
                default:
                    return false;
            }
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
                CountsDouble => Value(mod) <= 1
                    ? $"Counts twice toward the {material} set"
                    : $"Counts as {Value(mod) + 1} pieces toward the {material} set",
                Opener => $"+{n}% damage on your strip's opener",
                Finisher => $"+{n}% damage on your strip's finisher",
                BloodPrice => $"Each action costs {GameConfiguration.Instance.Combat.AffixBloodPriceHpPercent}% max HP, +{n}% damage",
                CrossWires.RuleEffect or CrossWires.AffixEffect => CrossWires.DescribeMod(mod),
                _ => null
            };
        }
    }
}
