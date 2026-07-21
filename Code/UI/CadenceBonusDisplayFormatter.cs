using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame.UI
{
    /// <summary>Shared short formatting for cadence bonus items in HUD and action strip.</summary>
    public static class CadenceBonusDisplayFormatter
    {
        public static string FormatBonusItemsShort(IEnumerable<ActionAttackBonusItem>? bonuses)
        {
            if (bonuses == null) return "";
            var parts = new List<string>();
            foreach (var b in bonuses)
            {
                string part = FormatBonusItemShort(b);
                if (!string.IsNullOrEmpty(part)) parts.Add(part);
            }
            return string.Join(", ", parts);
        }

        public static string FormatBonusItemShort(ActionAttackBonusItem? b)
        {
            if (b == null) return "";
            string sign = b.Value >= 0 ? "+" : "";
            return b.Type switch
            {
                "ACCURACY" => $"{sign}{b.Value:0} ACC",
                "HIT" => $"{sign}{b.Value:0} HIT",
                "COMBO" => $"{sign}{b.Value:0} COMBO",
                "CRIT" => $"{sign}{b.Value:0} CRIT",
                "CRIT_MISS" => $"{sign}{b.Value:0} CRIT MISS",
                "DAMAGE_MOD" when b.Value >= 0 => b.Value >= 100 ? $"{(b.Value / 100.0):0.#}x DMG" : $"{sign}{b.Value:0}% DMG",
                "DAMAGE_MOD" => $"{b.Value:0}% DMG",
                "SPEED_MOD" => $"{sign}{b.Value:0}% SPD",
                "MULTIHIT_MOD" => $"{sign}{b.Value:0} MH",
                "AMP_MOD" => $"{sign}{b.Value:0}% AMP",
                "WEAPON_SPEED" => $"{sign}{b.Value:0} WPN SPD",
                "WEAPON_DAMAGE" => $"{sign}{b.Value:0} WPN DMG",
                "STR" or "STRENGTH" => $"{sign}{b.Value:0} STR",
                "AGI" or "AGILITY" => $"{sign}{b.Value:0} AGI",
                "TECH" or "TECHNIQUE" => $"{sign}{b.Value:0} TECH",
                "INT" or "INTELLIGENCE" => $"{sign}{b.Value:0} INT",
                MultiDiceRollMapper.AdvantageBonusType => "Advantage",
                MultiDiceRollMapper.DisadvantageBonusType => "Disadvantage",
                _ => $"{sign}{b.Value:0} {b.Type}"
            };
        }

        public static string FormatTurnDurationSuffix(int turns) =>
            $"({turns} turn{(turns != 1 ? "s" : "")})";
    }
}
