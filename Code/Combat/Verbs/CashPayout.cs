using System;
using System.Collections.Generic;
using System.Globalization;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// What a <c>CASH ... INTO x</c> verb turns the spent store into (2026-11-18: one currency, many ways to cash it).
    /// <list type="bullet">
    /// <item><c>DAMAGE</c>: the default convert (% of the hit, <see cref="VerbPresets"/>).</item>
    /// <item><c>HEAL</c>: % of max HP per unit.</item>
    /// <item><c>STR</c> / <c>AGI</c> / <c>TEC</c> / <c>INT</c> / <c>PRIMARY</c>: % of that attribute per unit, for the rest of
    /// the fight (PRIMARY = the cashed material's class stat, else the hero's top attribute).</item>
    /// <item>A store id: a hero resource (<c>temphp</c> in % of max HP, <c>harden</c>, <c>fortify</c>, <c>focus</c>,
    /// <c>haste</c>) or a target status (<c>bleed</c>, <c>burn</c>, <c>expose</c>, ...) in stacks per unit.</item>
    /// </list>
    /// Per-unit rates come from <see cref="CombatConfig.CashIntoPerUnit"/> (patchable), else <see cref="DefaultRates"/>.
    /// Fractions round down over the whole cash, with at least 1 when anything was spent.
    /// </summary>
    public static class CashPayout
    {
        public const string Damage = "DAMAGE";
        public const string Heal = "HEAL";
        public const string Primary = "PRIMARY";
        /// <summary>Rate key for the most % of max HP one HEAL cash can restore (0 = uncapped).</summary>
        public const string HealCapKey = "healCapPercent";

        private static readonly string[] Attributes = { "STR", "AGI", "TEC", "INT", Primary };

        /// <summary>Default per-unit rates (at 100% cash). 10 units ≈ a full store.</summary>
        public static readonly IReadOnlyDictionary<string, double> DefaultRates =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [Heal] = 0.75,         // % max HP per unit (2.0 out-healed L20 enemies: ACTIONVALUE 2026-11-18)
                [HealCapKey] = 4.0,    // most % max HP one cash can heal
                ["temphp"] = 5.0,      // % max HP per unit
                ["attribute"] = 2.0,   // % of the attribute per unit
                ["harden"] = 0.5, ["fortify"] = 0.5, ["focus"] = 0.34, ["haste"] = 0.5,
                ["bleed"] = 1.0, ["burn"] = 1.0, ["poison"] = 1.0,
                ["expose"] = 0.34, ["stagger"] = 0.34, ["brittle"] = 0.34, ["rattle"] = 0.34, ["slow"] = 0.34,
                ["weaken"] = 0.2, ["acid"] = 0.2, ["mark"] = 0.1, ["stun"] = 0.1,
            };

        public static bool IsValid(string? payout)
        {
            if (string.IsNullOrWhiteSpace(payout))
                return false;
            string p = payout.Trim();
            return p.Equals(Damage, StringComparison.OrdinalIgnoreCase)
                || p.Equals(Heal, StringComparison.OrdinalIgnoreCase)
                || IsAttribute(p)
                || (Stores.TryGet(p, out var def) && def.CanAdd);
        }

        /// <summary>Upper-case keywords (DAMAGE, HEAL, attributes); store ids keep their registry spelling.</summary>
        public static string Normalize(string payout)
        {
            string p = payout.Trim();
            if (p.Equals(Damage, StringComparison.OrdinalIgnoreCase) || p.Equals(Heal, StringComparison.OrdinalIgnoreCase) || IsAttribute(p))
                return p.ToUpperInvariant() == "TECH" ? "TEC" : p.ToUpperInvariant();
            return Stores.TryGet(p, out var def) ? def.Id : p;
        }

        public static bool IsAttribute(string payout) =>
            Array.Exists(Attributes, a => a.Equals(payout, StringComparison.OrdinalIgnoreCase))
            || payout.Equals("TECH", StringComparison.OrdinalIgnoreCase);

        /// <summary>Per-unit rate for a payout (patch overrides first).</summary>
        public static double RatePerUnit(string payout)
        {
            string key = IsAttribute(payout) ? "attribute" : payout;
            var patch = GameConfiguration.Instance.Combat?.CashIntoPerUnit;
            if (patch != null && patch.TryGetValue(key, out double fromPatch))
                return Math.Max(0, fromPatch);
            return DefaultRates.TryGetValue(key, out double rate) ? rate : 0.34;
        }

        /// <summary>Pays <paramref name="spent"/> units of <paramref name="storeId"/> into <paramref name="payout"/>. Returns the amount granted.</summary>
        public static int Pay(Character hero, Actor? target, string storeId, int spent, double share, string payout)
        {
            if (hero == null || spent <= 0 || share <= 0)
                return 0;
            double raw = spent * share * RatePerUnit(payout);
            int amount = Math.Max(1, (int)Math.Floor(raw + 1e-9));

            if (payout == Heal)
            {
                double cap = RatePerUnit(HealCapKey);
                double pct = cap > 0 ? Math.Min(cap, raw) : raw;
                int hp = Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * pct / 100.0));
                if (hero.CurrentHealth > 0)
                    hero.Heal(hp);
                return hp;
            }
            if (IsAttribute(payout))
            {
                string code = payout == Primary ? MaterialSetController.GetConvertStatCode(hero, storeId) : payout;
                int current = code switch
                {
                    "AGI" => hero.GetEffectiveAgility(),
                    "TEC" => hero.GetEffectiveTechnique(),
                    "INT" => hero.GetEffectiveIntelligence(),
                    _ => hero.GetEffectiveStrength()
                };
                int points = Math.Max(1, (int)Math.Round(Math.Max(1, current) * raw / 100.0));
                // Cadence buffs spell Technique "TECH" (a "TEC" buff is never read).
                string buffType = code == "TEC" ? DynamicAttributeCategoryResolver.CodeTechnique : code;
                hero.FightCadenceBuffs.MergeAdditively(new[] { new ActionAttackBonusItem { Type = buffType, Value = points } });
                return points;
            }
            if (string.Equals(payout, "temphp", StringComparison.OrdinalIgnoreCase))
            {
                int hp = Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * raw / 100.0));
                return Stores.Add(hero, "temphp", hp);
            }
            return Stores.Add(VerbPresets.Holder(hero, target, payout), payout, amount);
        }

        /// <summary>Card text for a payout, e.g. <c>Harden (1 per 3 units)</c>.</summary>
        public static string Describe(Character? hero, string store, string payout, double share)
        {
            double rate = RatePerUnit(payout) * share;
            string n = rate.ToString("0.##", CultureInfo.InvariantCulture);
            if (payout == Heal)
            {
                double cap = RatePerUnit(HealCapKey);
                string capText = cap > 0 ? $", up to {cap.ToString("0.##", CultureInfo.InvariantCulture)}% per cash" : "";
                return $"healing ({n}% max HP per {store}{capText})";
            }
            if (string.Equals(payout, "temphp", StringComparison.OrdinalIgnoreCase))
                return $"temporary HP ({n}% max HP per {store})";
            if (IsAttribute(payout))
            {
                string stat = payout == Primary
                    ? (hero != null ? MaterialSetController.GetConvertStatCode(hero, store) : "your class stat")
                    : payout;
                return $"+{n}% {stat} per {store} for the fight";
            }
            string name = Stores.TryGet(payout, out var def) ? def.Label : payout;
            if (rate >= 1)
                return $"{name} ({n} per {store})";
            int per = Math.Max(1, (int)Math.Round(1 / Math.Max(1e-6, rate)));
            return $"{name} (1 per {per} {store})";
        }
    }
}
