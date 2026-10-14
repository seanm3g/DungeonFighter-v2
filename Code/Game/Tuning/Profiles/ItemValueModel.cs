using System;
using System.Collections.Generic;
using System.Linq;

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

        public static double Score(Item item)
        {
            double score = item switch
            {
                WeaponItem w => w.GetTotalDamage() / Math.Max(0.1, w.GetTotalAttackSpeed()),
                HeadItem h => h.GetTotalArmor() * ArmorRate,
                ChestItem c => c.GetTotalArmor() * ArmorRate,
                LegsItem l => l.GetTotalArmor() * ArmorRate,
                FeetItem f => f.GetTotalArmor() * ArmorRate,
                _ => 0
            };

            foreach (var s in item.StatBonuses)
            {
                string key = string.IsNullOrWhiteSpace(s.StatType) ? "" : s.StatType;
                if (StatRates.TryGetValue(key, out double rate))
                    score += rate * s.Value;
                if (s.Mechanics != null)
                    foreach (var m in s.Mechanics.Where(m => !string.Equals(m.StatType, key, StringComparison.OrdinalIgnoreCase)))
                        if (StatRates.TryGetValue(m.StatType ?? "", out double mrate))
                            score += mrate * m.Value;
            }

            score += item.ActionBonuses.Count * ActionBonusRate;
            return score;
        }
    }
}
