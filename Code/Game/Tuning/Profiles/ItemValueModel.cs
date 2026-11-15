using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Item value in "power points": 1 point ≈ +1 weapon damage at 1.0× swing time. Weapons score damage per swing time;
    /// armor scores its armor; every stat suffix adds <see cref="StatRates"/>[stat] × value. Rates are the exchange rates
    /// between stats — calibrate them with STATVALUE, then use ITEMVALUE to compare items against a per-level budget.
    /// </summary>
    public static class ItemValueModel
    {
        /// <summary>
        /// Power points per 1 unit of each stat suffix (StatType keys, case-insensitive). Measured with STATVALUE at L10,
        /// typical gear (2026-10-11): one d20 face of HIT/COMBO/CRIT is worth ~7 flat weapon damage; +1 STR only ~0.36.
        /// </summary>
        public static readonly Dictionary<string, double> StatRates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["STR"] = 0.36, ["AGI"] = 0.62, ["TEC"] = 1.15, ["INT"] = 1.15, ["ALL"] = 3.28,
            ["HIT"] = 6.8, ["COMBO"] = 7.4, ["CRIT"] = 7.5,
            ["Damage"] = 1.00, ["Armor"] = 3.0, ["Health"] = 0.5, ["HealthRegen"] = 8.6,
            // AttackSpeed is weapon speed points now (1 point = 10% speed); 2.7 was measured per 1% of base time, so ×10 until re-measured.
            ["AttackSpeed"] = 27.0, ["MagicFind"] = 0.0,
        };

        /// <summary>Power points per armor point on armor pieces (same scale as the Armor suffix).</summary>
        public static double ArmorRate { get; set; } = 3.0;

        /// <summary>Power points per action bonus line on the item.</summary>
        public static double ActionBonusRate { get; set; } = 1.0;

        /// <summary>
        /// Power points per adjective prefix (2026-11-09: prefix values solved so each common prefix on every piece is worth
        /// ~+8 clear points, about a 2.5-point Damage suffix). HIT / COMBO thresholds are saturated by mid game and MAGIC FIND
        /// has no combat effect, so they score 0.
        /// </summary>
        public static double AdjectivePrefixRate { get; set; } = 2.5;

        private static double AdjectiveRate(string? effect)
        {
            string e = (effect ?? "").Replace(" ", "").Replace("-", "").ToUpperInvariant();
            return e is "HIT" or "COMBO" or "MAGICFIND" or "" ? 0 : AdjectivePrefixRate;
        }

        /// <summary>
        /// Attribute lines (STR / AGI / TEC / INT) are a percent of the wearer's own attribute, so their worth follows how
        /// much the hero runs on it. Each line is scaled by the hero's attribute / the hero's highest attribute, times
        /// this (the rates above are 4-class averages, where a line hit its class's main attribute about 1 time in 1.6).
        /// </summary>
        public static double AttributeFitScale { get; set; } = 1.6;

        /// <summary>Floor on the attribute fit, so an off-attribute line is still worth a little.</summary>
        public static double AttributeFitFloor { get; set; } = 0.1;

        /// <summary>Granted action outside the hero's class pool (off-class action on armor): fraction of <see cref="ActionBonusRate"/>.</summary>
        public static double OffClassActionShare { get; set; } = 0.3;

        /// <summary>Per extra strip slot, as a fraction of the hero's current weapon value (one more action per combo cycle).</summary>
        public static double ExtraSlotWeaponShare { get; set; } = 0.06;

        /// <summary>
        /// Base modifier (<see cref="Item.ImplicitRule"/>): a DAMAGE n% rule is worth n% of the weapon value × the share of
        /// the strip it applies to; SPEED counts this much of that; POTENCY this much while a strip action applies the status.
        /// </summary>
        public static double ImplicitSpeedShare { get; set; } = 0.8;
        public static double ImplicitPotencyShare { get; set; } = 0.3;

        /// <summary>Hero-blind score (old behavior; the gear contest log and tools without a hero).</summary>
        public static double Score(Item item) => Score(item, null);

        /// <summary>
        /// Item value for <paramref name="hero"/> (2026-11-15): attribute lines scaled by the hero's attribute fit, tiered
        /// adjective prefixes by their rolled value against their family's tier-1 range, extra strip slots, granted actions by
        /// class pool, and the base modifier by the share of the hero's strip it applies to. Null hero = the hero-blind score.
        /// </summary>
        public static double Score(Item item, Character? hero)
        {
            double weaponValue = WeaponValue(hero?.Weapon as WeaponItem);
            double score = item switch
            {
                WeaponItem w => WeaponValue(w),
                HeadItem h => h.GetTotalArmor() * ArmorRate,
                ChestItem c => c.GetTotalArmor() * ArmorRate,
                LegsItem l => l.GetTotalArmor() * ArmorRate,
                FeetItem f => f.GetTotalArmor() * ArmorRate,
                _ => 0
            };

            foreach (var s in item.StatBonuses)
                foreach (var (type, value) in s.EnumerateContributions())
                {
                    string key = NormalizeStat(type);
                    if (key == "ExtraActionSlots")
                        continue; // priced below from item.ExtraActionSlots
                    if (StatRates.TryGetValue(key, out double rate))
                        score += rate * value * (hero == null ? 1 : AttributeFit(hero, key));
                }

            if (hero == null)
                score += item.ActionBonuses.Count * ActionBonusRate;
            else
            {
                var classTags = LootContext.ResolveHeroClassTags(hero);
                foreach (var a in item.ActionBonuses)
                {
                    var tags = ActionLoader.GetActionData(a.Name)?.Tags;
                    score += ActionBonusRate * (GearActionPools.IsInClassPool(tags, classTags) ? 1 : OffClassActionShare);
                }
            }

            foreach (var m in item.Modifications)
                if (m != null && m.GetPrefixCategory() == ModificationPrefixCategory.Adjective)
                    score += AdjectiveRate(m.Effect) * (hero == null ? 1 : TierScale(m));

            if (hero != null)
            {
                score += item.ExtraActionSlots * ExtraSlotWeaponShare * weaponValue;
                score += ImplicitValue(item, hero, weaponValue);
            }
            return score;
        }

        private static double WeaponValue(WeaponItem? w) =>
            w == null ? 0 : w.GetTotalDamage() / Math.Max(0.1, w.GetTotalAttackSpeed());

        private static string NormalizeStat(string? raw)
        {
            string u = (raw ?? "").Replace(" ", "").Replace("_", "").ToUpperInvariant();
            return u switch
            {
                "TECH" or "TEC" => "TEC",
                "HP" or "HPBONUS" or "HEALTHBONUS" or "HEALTH" => "Health",
                "BASEDAMAGE" or "WEAPONDAMAGE" or "DAMAGE" => "Damage",
                "ARMOR" => "Armor",
                "HEALTHREGEN" => "HealthRegen",
                "ATTACKSPEED" => "AttackSpeed",
                "MAGICFIND" => "MagicFind",
                _ when u.Contains("ACTION") && u.Contains("SLOT") => "ExtraActionSlots",
                _ => raw ?? ""
            };
        }

        /// <summary>Attribute lines: the hero's attribute / its highest attribute × <see cref="AttributeFitScale"/>; 1 for other stats.</summary>
        private static double AttributeFit(Character hero, string stat)
        {
            int str = hero.GetEffectiveStrength(), agi = hero.GetEffectiveAgility(), tec = hero.GetEffectiveTechnique(), intel = hero.GetEffectiveIntelligence();
            int own = stat.ToUpperInvariant() switch { "STR" => str, "AGI" => agi, "TEC" => tec, "INT" => intel, _ => -1 };
            if (own < 0)
                return 1;
            int top = Math.Max(1, Math.Max(Math.Max(str, agi), Math.Max(tec, intel)));
            return Math.Max(AttributeFitFloor, (double)own / top) * AttributeFitScale;
        }

        private static Dictionary<string, double>? _tierOneMid;

        /// <summary>Rolled value / the family's tier-1 midpoint (ItemMods.json), clamped to [0.25, 6]; 1 for legacy lines.</summary>
        private static double TierScale(Modification m)
        {
            string family = m.ModLine?.Family ?? "";
            if (family.Length == 0 || m.RolledValue <= 0)
                return 1;
            _tierOneMid ??= LoadTierOneMid();
            return _tierOneMid.TryGetValue(family, out double mid) && mid > 0 ? Math.Clamp(m.RolledValue / mid, 0.25, 6) : 1;
        }

        private static Dictionary<string, double> LoadTierOneMid()
        {
            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = GameConstants.ResolveGameDataPath("ItemMods.json");
                if (!System.IO.File.Exists(path))
                    return map;
                using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("families", out var families))
                    return map;
                foreach (var f in families.EnumerateArray())
                {
                    if (!f.TryGetProperty("family", out var name) || !f.TryGetProperty("tiers", out var tiers) || tiers.GetArrayLength() == 0)
                        continue;
                    var t0 = tiers[0];
                    if (t0.TryGetProperty("min", out var lo) && t0.TryGetProperty("max", out var hi)
                        && lo.TryGetDouble(out double a) && hi.TryGetDouble(out double b))
                        map[name.GetString() ?? ""] = (a + b) / 2;
                }
            }
            catch (Exception)
            {
                // Malformed data: tier scaling falls back to 1.
            }
            return map;
        }

        /// <summary>
        /// Base modifier value, summed over its rules (high tiers hold two): each rule's amount (as a share of the weapon
        /// value) × the share of the hero's strip whose swings satisfy its IF ACTION / TAG / CLASS condition. A rule that also
        /// needs fight state (HP, combo step, slot, a status on the target) counts at <see cref="ImplicitStateShare"/>.
        /// POTENCY counts when a strip action applies that status.
        /// </summary>
        private static double ImplicitValue(Item item, Character hero, double weaponValue)
        {
            if (string.IsNullOrWhiteSpace(item.ImplicitRule) || weaponValue <= 0)
                return 0;
            var strip = hero.GetComboActions();
            if (strip.Count == 0)
                return 0;
            double total = 0;
            foreach (string part in item.ImplicitRule.Split(';'))
            {
                var rule = string.IsNullOrWhiteSpace(part) ? null : CrossWireRule.Parse(part.Trim());
                if (rule == null || !rule.IsPassive)
                    continue;
                var subject = rule.Conditions.Where(c => c.Subject is "action" or "tag" or "class").ToList();
                double stateShare = subject.Count < rule.Conditions.Count ? ImplicitStateShare : 1.0;
                var probe = new CrossWireRule { When = "WHILE", Conditions = subject, Verb = rule.Verb, Store = rule.Store, Amount = rule.Amount, Percent = rule.Percent };
                double share = strip.Count(a => CrossWires.ConditionsHold(hero, null, probe, action: a)) / (double)strip.Count;
                double amount = rule.Amount / 100.0 * stateShare;
                total += rule.Verb.ToUpperInvariant() switch
                {
                    "DAMAGE" => amount * share * weaponValue,
                    "SPEED" => amount * share * weaponValue * ImplicitSpeedShare,
                    "POTENCY" => strip.Any(a => RulesSynergy.Features(a.Name).ContainsKey("fx:" + rule.Store.ToLowerInvariant()))
                        ? amount * weaponValue * ImplicitPotencyShare : 0,
                    _ => 0
                };
            }
            return total;
        }

        /// <summary>Share of a base modifier rule's value kept when it also needs fight state (HP, step, slot, target status).</summary>
        private const double ImplicitStateShare = 0.5;
    }
}
