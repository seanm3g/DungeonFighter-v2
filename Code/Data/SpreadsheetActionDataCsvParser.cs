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
            // Explicit TAGS cell (comma/semicolon list). Row-2 label-driven; usually column E (column F may be a sheet formula on push).
            data.Tags = FirstNonEmpty(
                header.GetValue(columns, null, "TAGS"),
                header.GetValue(columns, null, "TAG"));
            data.NumberOfHits = header.GetValue(columns, null, "# OF HITS");
            data.Damage = header.GetDamagePercentValue(columns);
            data.Speed = header.GetValue(columns, null, "SPEED(x)");
            data.Duration = FirstNonEmpty(
                header.GetValue(columns, SpreadsheetDurationSemantics.StatusEffectContext, "DURATION", allowUnscopedLabelFallback: false),
                header.GetValue(columns, null, "DURATION"));
            data.Cadence = header.GetValue(columns, null, "CADENCE");
            data.Mechanics = FirstNonEmpty(
                header.GetValue(columns, "MECHANICS", "MECHANICS", allowUnscopedLabelFallback: false),
                header.GetValue(columns, null, "MECHANICS"));
            SpreadsheetDurationSemantics.NormalizeDurationAndCadence(data);
            data.Opener = header.GetValue(columns, null, "OPENER");
            data.Finisher = header.GetValue(columns, null, "FINISHER");
            data.Target = header.GetValue(columns, null, "TARGET");

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
            data.EnemyCritMiss = header.GetValue(columns, "ENEMY DICE MODIFICATIONS", "CRIT MISS");

            data.HeroSTR = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "STR");
            data.HeroAGI = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "AGI");
            data.HeroTECH = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "TECH");
            data.HeroINT = header.GetValue(columns, "HERO ATTRIBUTE MODIFICATION", "INT");

            data.EnemySTR = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "STR");
            data.EnemyAGI = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "AGI");
            data.EnemyTECH = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "TECH");
            data.EnemyINT = header.GetValue(columns, "ENEMY ATTRIBUTE MODIFICATIONS", "INT");

            // Next-action modifiers: many ACTION tabs label this block "HERO BASE STATS"; legacy sheets use "ENEMY BASE STATS".
            ParseNextActionModsFromHeader(data, columns, header);
            // Row-1 context missing or non-standard: some sheets use globally unique row-2 labels only.
            if (string.IsNullOrWhiteSpace(data.SpeedMod))
                data.SpeedMod = FirstNonEmpty(
                    header.GetValue(columns, null, "HERO ACTION SPEED MOD"),
                    header.GetValue(columns, null, "HERO SPEED MOD"));
            if (string.IsNullOrWhiteSpace(data.MultiHitMod))
                data.MultiHitMod = header.GetValue(columns, null, "HERO MULTIHIT MOD");
            if (string.IsNullOrWhiteSpace(data.EnemySpeedMod))
                data.EnemySpeedMod = FirstNonEmpty(
                    header.GetValue(columns, null, "ENEMY ACTION SPEED MOD"),
                    header.GetValue(columns, null, "ENEMY SPEED MOD"));
            if (string.IsNullOrWhiteSpace(data.EnemyMultiHitMod))
                data.EnemyMultiHitMod = header.GetValue(columns, null, "ENEMY MULTIHIT MOD");

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
            data.Confuse = header.GetValue(columns, "ENEMY TARGET", "CONFUSE");

            ParseSelfTargetStatusEffects(data, columns, header);

            // Unscoped fallbacks only when neither hero nor enemy section provided a value (avoids stealing AD–AG into AJ–AM).
            if (string.IsNullOrEmpty(data.SpeedMod) && string.IsNullOrEmpty(data.EnemySpeedMod))
                data.SpeedMod = FirstNonEmpty(
                    header.GetValue(columns, null, "HERO ACTION SPEED MOD"),
                    header.GetValue(columns, null, "HERO SPEED MOD"),
                    header.GetValue(columns, null, "SPEED MOD"),
                    header.GetValue(columns, null, "ACTION SPEED"));
            if (string.IsNullOrEmpty(data.DamageMod) && string.IsNullOrEmpty(data.EnemyDamageMod))
                data.DamageMod = FirstNonEmpty(
                    header.GetValue(columns, null, "DAMAGE MOD"),
                    header.GetValue(columns, null, "ACTION DAMAGE"));
            if (string.IsNullOrEmpty(data.MultiHitMod) && string.IsNullOrEmpty(data.EnemyMultiHitMod))
                data.MultiHitMod = FirstNonEmpty(
                    header.GetValue(columns, null, "HERO MULTIHIT MOD"),
                    header.GetValue(columns, null, "MULTIHIT MOD"));
            if (string.IsNullOrEmpty(data.AmpMod) && string.IsNullOrEmpty(data.EnemyAmpMod))
            {
                data.AmpMod = header.GetValue(columns, null, "AMP_MOD");
                if (string.IsNullOrEmpty(data.AmpMod))
                    data.AmpMod = header.GetValue(columns, null, "AMP MOD");
            }
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
            if (string.IsNullOrEmpty(data.Confuse)) data.Confuse = header.GetValue(columns, null, "CONFUSE");
            if (string.IsNullOrEmpty(data.HeroHeal)) data.HeroHeal = header.GetValue(columns, null, "HEAL");
            if (string.IsNullOrEmpty(data.HeroHealMaxHealth)) data.HeroHealMaxHealth = header.GetValue(columns, null, "MAX HEALTH");

            ParseRollMechanics(data, columns, header);
            ParseJumpShiftDisruptMechanics(data, columns, header);
        }

        private static void ParseSelfTargetStatusEffects(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            data.SelfTargetEffects.Clear();
            AddSelfTargetEffectIfPresent(data, columns, header, "STUN", "stun");
            AddSelfTargetEffectIfPresent(data, columns, header, "POISON", "poison");
            AddSelfTargetEffectIfPresent(data, columns, header, "BURN", "burn");
            AddSelfTargetEffectIfPresent(data, columns, header, "BLEED", "bleed");
            AddSelfTargetEffectIfPresent(data, columns, header, "WEAKEN", "weaken");
            AddSelfTargetEffectIfPresent(data, columns, header, "EXPOSE", "expose");
            AddSelfTargetEffectIfPresent(data, columns, header, "SLOW", "slow");
            AddSelfTargetEffectIfPresent(data, columns, header, "VULNERABILITY", "vulnerability");
            AddSelfTargetEffectIfPresent(data, columns, header, "HARDEN", "harden");
            AddSelfTargetEffectIfPresent(data, columns, header, "SILENCE", "silence");
            AddSelfTargetEffectIfPresent(data, columns, header, "PIERCE", "pierce");
            AddSelfTargetEffectIfPresent(data, columns, header, "STAT DRAIN", "statdrain");
            AddSelfTargetEffectIfPresent(data, columns, header, "FOCUS", "focus");
        }

        private static void AddSelfTargetEffectIfPresent(
            SpreadsheetActionData data,
            string[] columns,
            SpreadsheetHeader header,
            string label,
            string effectKey)
        {
            string value = header.GetValue(columns, "SELF TARGET", label);
            if (!string.IsNullOrWhiteSpace(value) && value != "0")
                data.SelfTargetEffects.Add(effectKey);
        }

        private static void ParseRollMechanics(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            data.DiceRolls = FirstNonEmpty(
                header.GetValue(columns, null, "DICE ROLLS"),
                data.DiceRolls);
            data.HighestLowestRoll = FirstNonEmpty(
                header.GetValue(columns, null, "HIGHEST/LOWEST ROLL"),
                header.GetValue(columns, null, "HIGHEST LOWEST ROLL"),
                data.HighestLowestRoll);
        }

        private static void ParseNextActionModsFromHeader(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            // Require row-1 section + label match; do not use the first duplicate label in another block (enemy before hero on sheet).
            const bool allowUnscopedLabelFallback = false;
            data.SpeedMod = FirstNonEmpty(
                header.GetValue(columns, "HERO BASE STATS", "SPEED MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "HERO BASE STATS", "ACTION SPEED", null, allowUnscopedLabelFallback),
                // Templates that prefix the stat name in row 2 (e.g. "HERO ACTION SPEED MOD" vs "SPEED MOD").
                header.GetValue(columns, "HERO BASE STATS", "HERO ACTION SPEED MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "HERO BASE STATS", "HERO SPEED MOD", null, allowUnscopedLabelFallback));

            data.DamageMod = FirstNonEmpty(
                header.GetValue(columns, "HERO BASE STATS", "DAMAGE MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "HERO BASE STATS", "ACTION DAMAGE", null, allowUnscopedLabelFallback));

            data.MultiHitMod = FirstNonEmpty(
                header.GetValue(columns, "HERO BASE STATS", "MULTIHIT MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "HERO BASE STATS", "HERO MULTIHIT MOD", null, allowUnscopedLabelFallback));

            data.AmpMod = FirstNonEmpty(
                header.GetValue(columns, "HERO BASE STATS", "AMP_MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "HERO BASE STATS", "AMP MOD", null, allowUnscopedLabelFallback));

            data.EnemySpeedMod = FirstNonEmpty(
                header.GetValue(columns, "ENEMY BASE STATS", "SPEED MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "ENEMY BASE STATS", "ACTION SPEED", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "ENEMY BASE STATS", "ENEMY ACTION SPEED MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "ENEMY BASE STATS", "ENEMY SPEED MOD", null, allowUnscopedLabelFallback));

            data.EnemyDamageMod = FirstNonEmpty(
                header.GetValue(columns, "ENEMY BASE STATS", "DAMAGE MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "ENEMY BASE STATS", "ACTION DAMAGE", null, allowUnscopedLabelFallback));

            data.EnemyMultiHitMod = FirstNonEmpty(
                header.GetValue(columns, "ENEMY BASE STATS", "MULTIHIT MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "ENEMY BASE STATS", "ENEMY MULTIHIT MOD", null, allowUnscopedLabelFallback));

            data.EnemyAmpMod = FirstNonEmpty(
                header.GetValue(columns, "ENEMY BASE STATS", "AMP_MOD", null, allowUnscopedLabelFallback),
                header.GetValue(columns, "ENEMY BASE STATS", "AMP MOD", null, allowUnscopedLabelFallback));
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null) return "";
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return "";
        }

        private static void ParseJumpShiftDisruptMechanics(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            data.Jump = header.GetValue(columns, null, "JUMP");
            data.JumpRelative = header.GetValue(columns, null, "SHIFT");
            if (string.IsNullOrEmpty(data.JumpRelative))
                data.JumpRelative = header.GetValue(columns, null, "JUMP RELATIVE");
            data.Disrupt = header.GetValue(columns, null, "DISRUPT");
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
            // Fixed layout aligned to sheet columns AD–AG (enemy next-action mods) and AJ–AM (hero); AH–AI = heal; status block follows AM (+4 vs legacy indices).
            if (columns.Length > 29) data.EnemySpeedMod = columns[29].Trim();
            if (columns.Length > 30) data.EnemyDamageMod = columns[30].Trim();
            if (columns.Length > 31) data.EnemyMultiHitMod = columns[31].Trim();
            if (columns.Length > 32) data.EnemyAmpMod = columns[32].Trim();
            if (columns.Length > 33) data.HeroHeal = columns[33].Trim();
            if (columns.Length > 34) data.HeroHealMaxHealth = columns[34].Trim();
            if (columns.Length > 35) data.SpeedMod = columns[35].Trim();
            if (columns.Length > 36) data.DamageMod = columns[36].Trim();
            if (columns.Length > 37) data.MultiHitMod = columns[37].Trim();
            if (columns.Length > 38) data.AmpMod = columns[38].Trim();
            if (columns.Length > 39) data.Stun = columns[39].Trim();
            if (columns.Length > 40) data.Poison = columns[40].Trim();
            if (columns.Length > 41) data.Burn = columns[41].Trim();
            if (columns.Length > 42) data.Bleed = columns[42].Trim();
            if (columns.Length > 43) data.Weaken = columns[43].Trim();
            if (columns.Length > 44) data.Expose = columns[44].Trim();
            if (columns.Length > 45) data.Slow = columns[45].Trim();
            if (columns.Length > 46) data.Vulnerability = columns[46].Trim();
            if (columns.Length > 47) data.Harden = columns[47].Trim();
            if (columns.Length > 48) data.Silence = columns[48].Trim();
            if (columns.Length > 49) data.Pierce = columns[49].Trim();
            if (columns.Length > 50) data.StatDrain = columns[50].Trim();
            if (columns.Length > 51) data.Fortify = columns[51].Trim();
            if (columns.Length > 52) data.Consume = columns[52].Trim();
            if (columns.Length > 53) data.Focus = columns[53].Trim();
            if (columns.Length > 54) data.Cleanse = columns[54].Trim();
            if (columns.Length > 55) data.Lifesteal = columns[55].Trim();
            if (columns.Length > 56) data.Reflect = columns[56].Trim();
        }
    }
}
