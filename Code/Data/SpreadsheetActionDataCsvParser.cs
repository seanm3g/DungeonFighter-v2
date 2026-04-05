using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Parses CSV rows into SpreadsheetActionData. Extracted from SpreadsheetActionData for separation of data shape vs parsing.
    /// </summary>
    public static class SpreadsheetActionDataCsvParser
    {
        /// <summary>
        /// Parses a CSV row into SpreadsheetActionData. When header is provided, uses row 1 (context) and row 2 (labels)
        /// to resolve columns by section and label so mechanics are determined correctly.
        /// </summary>
        public static SpreadsheetActionData FromCsvRow(string[]? columns, SpreadsheetHeader? header = null)
        {
            var data = new SpreadsheetActionData();
            if (columns == null || columns.Length == 0)
                return data;

            if (header != null)
            {
                ParseWithHeader(data, columns, header);
                return data;
            }

            ParseWithIndexFallback(data, columns);
            return data;
        }

        private static void ParseWithHeader(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            data.Action = header.GetValue(columns, null, "ACTION");
            data.Description = header.GetValue(columns, null, "DESCRIPTION");
            data.Rarity = header.GetValue(columns, null, "RARITY");
            data.Category = header.GetValue(columns, null, "CATEGORY");
            data.DPS = header.GetValue(columns, null, "DPS(%)");
            data.NumberOfHits = header.GetValue(columns, null, "# OF HITS");
            data.Damage = header.GetValue(columns, null, "DAMAGE(%)", rawLabelMustContain: "%");
            if (string.IsNullOrEmpty(data.Damage) && columns.Length > 6)
                data.Damage = columns[6].Trim();
            data.Speed = header.GetValue(columns, null, "SPEED(x)");
            data.Duration = header.GetValue(columns, null, "DURATION");
            data.Cadence = header.GetValue(columns, null, "CADENCE");
            data.Opener = header.GetValue(columns, null, "OPENER");
            data.Finisher = header.GetValue(columns, null, "FINISHER");

            data.HeroAccuracy = header.GetValue(columns, "HERO DICE ROLL MODIFICATIONS", "ACCUARCY");
            if (string.IsNullOrEmpty(data.HeroAccuracy)) data.HeroAccuracy = header.GetValue(columns, "HERO DICE ROLL MODIFICATIONS", "ACCURACY");
            data.HeroHit = header.GetValue(columns, "HERO DICE ROLL MODIFICATIONS", "HIT");
            data.HeroCombo = header.GetValue(columns, "HERO DICE ROLL MODIFICATIONS", "COMBO");
            data.HeroCrit = header.GetValue(columns, "HERO DICE ROLL MODIFICATIONS", "CRIT");
            data.HeroCritMiss = header.GetValue(columns, "HERO DICE ROLL MODIFICATIONS", "CRIT MISS");

            data.EnemyAccuracy = header.GetValue(columns, "ENEMY DICE MODIFICATIONS", "ACCUARCY");
            if (string.IsNullOrEmpty(data.EnemyAccuracy)) data.EnemyAccuracy = header.GetValue(columns, "ENEMY DICE MODIFICATIONS", "ACCURACY");
            data.EnemyHit = header.GetValue(columns, "ENEMY DICE MODIFICATIONS", "HIT");
            data.EnemyCombo = header.GetValue(columns, "ENEMY DICE MODIFICATIONS", "COMBO");
            data.EnemyCrit = header.GetValue(columns, "ENEMY DICE MODIFICATIONS", "CRIT");

            data.HeroSTR = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "STR");
            data.HeroAGI = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "AGI");
            data.HeroTECH = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "TECH");
            data.HeroINT = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "INT");

            data.EnemySTR = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "STR");
            data.EnemyAGI = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "AGI");
            data.EnemyTECH = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "TECH");
            data.EnemyINT = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "INT");

            data.SpeedMod = header.GetValue(columns, "ENEMY BASE STATS", "SPEED MOD");
            data.DamageMod = header.GetValue(columns, "ENEMY BASE STATS", "DAMAGE MOD");
            data.MultiHitMod = header.GetValue(columns, "ENEMY BASE STATS", "MULTIHIT MOD");
            data.AmpMod = header.GetValue(columns, "ENEMY BASE STATS", "AMP_MOD");

            data.HeroHeal = header.GetValue(columns, "HERO HEAL", "HEAL");
            data.HeroHealMaxHealth = header.GetValue(columns, "HERO HEAL", "MAX HEALTH");

            data.Stun = header.GetValue(columns, "ENEMY TARGET", "STUN");
            data.Poison = header.GetValue(columns, "ENEMY TARGET", "POISON");
            data.Burn = header.GetValue(columns, "ENEMY TARGET", "BURN");
            data.Bleed = header.GetValue(columns, "ENEMY TARGET", "BLEED");
            data.Weaken = header.GetValue(columns, "ENEMY TARGET", "WEAKEN");
            data.Expose = header.GetValue(columns, "ENEMY TARGET", "EXPOSE");
            data.Slow = header.GetValue(columns, "ENEMY TARGET", "SLOW");
            data.Vulnerability = header.GetValue(columns, "ENEMY TARGET", "VULNERABILITY");
            data.Harden = header.GetValue(columns, "ENEMY TARGET", "HARDEN");
            data.Silence = header.GetValue(columns, "ENEMY TARGET", "SILENCE");
            data.Pierce = header.GetValue(columns, "ENEMY TARGET", "PIERCE");
            data.StatDrain = header.GetValue(columns, "ENEMY TARGET", "STAT DRAIN");
            data.Fortify = header.GetValue(columns, "ENEMY TARGET", "FORTIFY");
            data.Consume = header.GetValue(columns, "ENEMY TARGET", "CONSUME");
            data.Focus = header.GetValue(columns, "ENEMY TARGET", "FOCUS");
            data.Cleanse = header.GetValue(columns, "ENEMY TARGET", "CLENSE");
            data.Lifesteal = header.GetValue(columns, "ENEMY TARGET", "LIFESTEAL");
            data.Reflect = header.GetValue(columns, "ENEMY TARGET", "REFLECT");
            data.SelfDamage = header.GetValue(columns, "ENEMY TARGET", "SELF DAMAGE");
            if (string.IsNullOrEmpty(data.SelfDamage)) data.SelfDamage = header.GetValue(columns, "SELF TARGET", "SELF DAMAGE");

            if (string.IsNullOrEmpty(data.SpeedMod)) data.SpeedMod = header.GetValue(columns, null, "SPEED MOD");
            if (string.IsNullOrEmpty(data.DamageMod)) data.DamageMod = header.GetValue(columns, null, "DAMAGE MOD");
            if (string.IsNullOrEmpty(data.MultiHitMod)) data.MultiHitMod = header.GetValue(columns, null, "MULTIHIT MOD");
            if (string.IsNullOrEmpty(data.AmpMod)) data.AmpMod = header.GetValue(columns, null, "AMP_MOD");
            if (string.IsNullOrEmpty(data.Stun)) data.Stun = header.GetValue(columns, null, "STUN");
            if (string.IsNullOrEmpty(data.Poison)) data.Poison = header.GetValue(columns, null, "POISON");
            if (string.IsNullOrEmpty(data.Burn)) data.Burn = header.GetValue(columns, null, "BURN");
            if (string.IsNullOrEmpty(data.Bleed)) data.Bleed = header.GetValue(columns, null, "BLEED");
            if (string.IsNullOrEmpty(data.Weaken)) data.Weaken = header.GetValue(columns, null, "WEAKEN");
            if (string.IsNullOrEmpty(data.Expose)) data.Expose = header.GetValue(columns, null, "EXPOSE");
            if (string.IsNullOrEmpty(data.Slow)) data.Slow = header.GetValue(columns, null, "SLOW");
            if (string.IsNullOrEmpty(data.Vulnerability)) data.Vulnerability = header.GetValue(columns, null, "VULNERABILITY");
            if (string.IsNullOrEmpty(data.Harden)) data.Harden = header.GetValue(columns, null, "HARDEN");
            if (string.IsNullOrEmpty(data.Silence)) data.Silence = header.GetValue(columns, null, "SILENCE");
            if (string.IsNullOrEmpty(data.Pierce)) data.Pierce = header.GetValue(columns, null, "PIERCE");
            if (string.IsNullOrEmpty(data.StatDrain)) data.StatDrain = header.GetValue(columns, null, "STAT DRAIN");
            if (string.IsNullOrEmpty(data.Fortify)) data.Fortify = header.GetValue(columns, null, "FORTIFY");
            if (string.IsNullOrEmpty(data.Consume)) data.Consume = header.GetValue(columns, null, "CONSUME");
            if (string.IsNullOrEmpty(data.Focus)) data.Focus = header.GetValue(columns, null, "FOCUS");
            if (string.IsNullOrEmpty(data.Cleanse)) data.Cleanse = header.GetValue(columns, null, "CLENSE");
            if (string.IsNullOrEmpty(data.Lifesteal)) data.Lifesteal = header.GetValue(columns, null, "LIFESTEAL");
            if (string.IsNullOrEmpty(data.Reflect)) data.Reflect = header.GetValue(columns, null, "REFLECT");
            if (string.IsNullOrEmpty(data.SelfDamage)) data.SelfDamage = header.GetValue(columns, null, "SELF DAMAGE");
            if (string.IsNullOrEmpty(data.HeroHeal)) data.HeroHeal = header.GetValue(columns, null, "HEAL");
            if (string.IsNullOrEmpty(data.HeroHealMaxHealth)) data.HeroHealMaxHealth = header.GetValue(columns, null, "MAX HEALTH");
        }

        private static void ParseWithIndexFallback(SpreadsheetActionData data, string[] columns)
        {
            if (columns.Length > 0) data.Action = columns[0].Trim();
            if (columns.Length > 1) data.Description = columns[1].Trim();
            if (columns.Length > 3) data.Rarity = columns[3].Trim();
            if (columns.Length > 4) { data.Category = columns[4].Trim(); data.DPS = columns[4].Trim(); }
            if (columns.Length > 5) data.NumberOfHits = columns[5].Trim();
            if (columns.Length > 6) data.Damage = columns[6].Trim();
            if (columns.Length > 7) data.Speed = columns[7].Trim();
            if (columns.Length > 8) data.Duration = columns[8].Trim();
            if (columns.Length > 9) data.Cadence = columns[9].Trim();
            if (columns.Length > 10) data.Opener = columns[10].Trim();
            if (columns.Length > 11) data.Finisher = columns[11].Trim();
            if (columns.Length > 12) data.HeroAccuracy = columns[12].Trim();
            if (columns.Length > 13) data.HeroHit = columns[13].Trim();
            if (columns.Length > 14) data.HeroCombo = columns[14].Trim();
            if (columns.Length > 15) data.HeroCrit = columns[15].Trim();
            if (columns.Length > 16) data.EnemyAccuracy = columns[16].Trim();
            if (columns.Length > 17) data.EnemyHit = columns[17].Trim();
            if (columns.Length > 18) data.EnemyCombo = columns[18].Trim();
            if (columns.Length > 19) data.EnemyCrit = columns[19].Trim();
            if (columns.Length > 21) data.HeroSTR = columns[21].Trim();
            if (columns.Length > 22) data.HeroAGI = columns[22].Trim();
            if (columns.Length > 23) data.HeroTECH = columns[23].Trim();
            if (columns.Length > 24) data.HeroINT = columns[24].Trim();
            if (columns.Length > 29) data.SpeedMod = columns[29].Trim();
            if (columns.Length > 30) data.DamageMod = columns[30].Trim();
            if (columns.Length > 31) data.MultiHitMod = columns[31].Trim();
            if (columns.Length > 32) data.AmpMod = columns[32].Trim();
            if (columns.Length > 33) data.HeroHeal = columns[33].Trim();
            if (columns.Length > 34) data.HeroHealMaxHealth = columns[34].Trim();
            if (columns.Length > 35) data.Stun = columns[35].Trim();
            if (columns.Length > 36) data.Poison = columns[36].Trim();
            if (columns.Length > 37) data.Burn = columns[37].Trim();
            if (columns.Length > 38) data.Bleed = columns[38].Trim();
            if (columns.Length > 39) data.Weaken = columns[39].Trim();
            if (columns.Length > 40) data.Expose = columns[40].Trim();
            if (columns.Length > 41) data.Slow = columns[41].Trim();
            if (columns.Length > 42) data.Vulnerability = columns[42].Trim();
            if (columns.Length > 43) data.Harden = columns[43].Trim();
            if (columns.Length > 44) data.Silence = columns[44].Trim();
            if (columns.Length > 45) data.Pierce = columns[45].Trim();
            if (columns.Length > 46) data.StatDrain = columns[46].Trim();
            if (columns.Length > 47) data.Fortify = columns[47].Trim();
            if (columns.Length > 48) data.Consume = columns[48].Trim();
            if (columns.Length > 49) data.Focus = columns[49].Trim();
            if (columns.Length > 50) data.Cleanse = columns[50].Trim();
            if (columns.Length > 51) data.Lifesteal = columns[51].Trim();
            if (columns.Length > 52) data.Reflect = columns[52].Trim();
            if (columns.Length > 53) data.SelfDamage = columns[53].Trim();
        }
    }
}
