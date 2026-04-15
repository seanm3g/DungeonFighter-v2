namespace RPGGame.Data
{
    /// <summary>
    /// Writes <see cref="SpreadsheetActionData"/> into a sheet row using the same (context, label) rules as
    /// <see cref="SpreadsheetActionDataCsvParser"/> ingestion.
    /// </summary>
    public static class SpreadsheetActionDataSheetRowSerializer
    {
        public static string[] ToRow(SpreadsheetActionData data, SpreadsheetHeader header)
        {
            int w = header.LabelByIndex.Count;
            var row = new string[w];
            for (int i = 0; i < w; i++)
                row[i] = "";

            var h = header;

            h.SetCell(row, null, "ACTION", data.Action);
            h.SetCell(row, null, "DESCRIPTION", data.Description);
            h.SetCell(row, null, "RARITY", data.Rarity);
            h.SetCell(row, null, "CATEGORY", data.Category);
            h.SetCell(row, null, "DPS(%)", data.DPS);
            h.SetCell(row, null, "# OF HITS", data.NumberOfHits);
            h.SetDamagePercentCell(row, data.Damage);
            h.SetCell(row, null, "SPEED(x)", data.Speed);
            h.SetCell(row, null, "DURATION", data.Duration);
            h.SetCell(row, null, "CADENCE", data.Cadence);
            h.SetCell(row, null, "OPENER", data.Opener);
            h.SetCell(row, null, "FINISHER", data.Finisher);

            WriteHeroEnemyAccuracy(h, row, data.HeroAccuracy, isHero: true);
            h.SetCell(row, "HERO DICE ROLL MODIFICATIONS", "HIT", data.HeroHit);
            h.SetCell(row, "HERO DICE ROLL MODIFICATIONS", "COMBO", data.HeroCombo);
            h.SetCell(row, "HERO DICE ROLL MODIFICATIONS", "CRIT", data.HeroCrit);
            h.SetCell(row, "HERO DICE ROLL MODIFICATIONS", "CRIT MISS", data.HeroCritMiss);

            WriteHeroEnemyAccuracy(h, row, data.EnemyAccuracy, isHero: false);
            h.SetCell(row, "ENEMY DICE MODIFICATIONS", "HIT", data.EnemyHit);
            h.SetCell(row, "ENEMY DICE MODIFICATIONS", "COMBO", data.EnemyCombo);
            h.SetCell(row, "ENEMY DICE MODIFICATIONS", "CRIT", data.EnemyCrit);
            h.SetCell(row, "ENEMY DICE MODIFICATIONS", "CRIT MISS", data.EnemyCritMiss);

            h.SetCell(row, "HERO ATTRIBUTE MODIFICATION", "STR", data.HeroSTR);
            h.SetCell(row, "HERO ATTRIBUTE MODIFICATION", "AGI", data.HeroAGI);
            h.SetCell(row, "HERO ATTRIBUTE MODIFICATION", "TECH", data.HeroTECH);
            h.SetCell(row, "HERO ATTRIBUTE MODIFICATION", "INT", data.HeroINT);

            h.SetCell(row, "ENEMY ATTRIBUTE MODIFICATIONS", "STR", data.EnemySTR);
            h.SetCell(row, "ENEMY ATTRIBUTE MODIFICATIONS", "AGI", data.EnemyAGI);
            h.SetCell(row, "ENEMY ATTRIBUTE MODIFICATIONS", "TECH", data.EnemyTECH);
            h.SetCell(row, "ENEMY ATTRIBUTE MODIFICATIONS", "INT", data.EnemyINT);

            h.SetCell(row, "ENEMY BASE STATS", "SPEED MOD", data.SpeedMod);
            h.SetCell(row, "ENEMY BASE STATS", "DAMAGE MOD", data.DamageMod);
            h.SetCell(row, "ENEMY BASE STATS", "MULTIHIT MOD", data.MultiHitMod);
            h.SetCell(row, "ENEMY BASE STATS", "AMP_MOD", data.AmpMod);

            h.SetCell(row, "HERO HEAL", "HEAL", data.HeroHeal);
            h.SetCell(row, "HERO HEAL", "MAX HEALTH", data.HeroHealMaxHealth);

            h.SetCell(row, "ENEMY TARGET", "STUN", data.Stun);
            h.SetCell(row, "ENEMY TARGET", "POISON", data.Poison);
            h.SetCell(row, "ENEMY TARGET", "BURN", data.Burn);
            h.SetCell(row, "ENEMY TARGET", "BLEED", data.Bleed);
            h.SetCell(row, "ENEMY TARGET", "WEAKEN", data.Weaken);
            h.SetCell(row, "ENEMY TARGET", "EXPOSE", data.Expose);
            h.SetCell(row, "ENEMY TARGET", "SLOW", data.Slow);
            h.SetCell(row, "ENEMY TARGET", "VULNERABILITY", data.Vulnerability);
            h.SetCell(row, "ENEMY TARGET", "HARDEN", data.Harden);
            h.SetCell(row, "ENEMY TARGET", "SILENCE", data.Silence);
            h.SetCell(row, "ENEMY TARGET", "PIERCE", data.Pierce);
            h.SetCell(row, "ENEMY TARGET", "STAT DRAIN", data.StatDrain);
            h.SetCell(row, "ENEMY TARGET", "FORTIFY", data.Fortify);
            h.SetCell(row, "ENEMY TARGET", "CONSUME", data.Consume);
            h.SetCell(row, "ENEMY TARGET", "FOCUS", data.Focus);
            h.SetCell(row, "ENEMY TARGET", "CLENSE", data.Cleanse);
            h.SetCell(row, "ENEMY TARGET", "LIFESTEAL", data.Lifesteal);
            h.SetCell(row, "ENEMY TARGET", "REFLECT", data.Reflect);
            h.SetCell(row, "ENEMY TARGET", "SELF DAMAGE", data.SelfDamage);
            h.SetCell(row, "SELF TARGET", "SELF DAMAGE", data.SelfDamage);

            FallbackUnscoped(h, row, data);

            WriteOptionalMechanics(h, row, data);

            if (w > 2)
            {
                string c = SheetsPushUtilities.NormalizeSheetString(data.ColumnC);
                if (c.Length > 0)
                    row[2] = c;
            }

            return row;
        }

        private static void WriteHeroEnemyAccuracy(SpreadsheetHeader h, string[] row, string value, bool isHero)
        {
            string ctx = isHero ? "HERO DICE ROLL MODIFICATIONS" : "ENEMY DICE MODIFICATIONS";
            int iTypo = h.GetColumnIndex(ctx, "ACCUARCY");
            int iOk = h.GetColumnIndex(ctx, "ACCURACY");
            if (iTypo >= 0 && iTypo < row.Length)
                row[iTypo] = SheetsPushUtilities.NormalizeSheetString(value);
            else if (iOk >= 0 && iOk < row.Length)
                row[iOk] = SheetsPushUtilities.NormalizeSheetString(value);
        }

        private static string rowValueAtLabel(SpreadsheetHeader h, string[] row, string? ctx, string label)
        {
            int idx = h.GetColumnIndex(ctx, label);
            if (idx < 0 || idx >= row.Length)
                return "";
            return row[idx];
        }

        /// <summary>
        /// Same fallbacks as <see cref="SpreadsheetActionDataCsvParser"/> when section-specific cells are empty on ingest.
        /// </summary>
        private static void FallbackUnscoped(SpreadsheetHeader h, string[] row, SpreadsheetActionData data)
        {
            void SetIf(string lbl, string v, string? raw = null)
            {
                int idx = h.GetColumnIndex(null, lbl, raw);
                if (idx >= 0 && idx < row.Length && string.IsNullOrEmpty(row[idx]))
                    row[idx] = SheetsPushUtilities.NormalizeSheetString(v);
            }

            SetIf("SPEED MOD", data.SpeedMod);
            SetIf("DAMAGE MOD", data.DamageMod);
            SetIf("MULTIHIT MOD", data.MultiHitMod);
            SetIf("AMP_MOD", data.AmpMod);
            SetIf("STUN", data.Stun);
            SetIf("POISON", data.Poison);
            SetIf("BURN", data.Burn);
            SetIf("BLEED", data.Bleed);
            SetIf("WEAKEN", data.Weaken);
            SetIf("EXPOSE", data.Expose);
            SetIf("SLOW", data.Slow);
            SetIf("VULNERABILITY", data.Vulnerability);
            SetIf("HARDEN", data.Harden);
            SetIf("SILENCE", data.Silence);
            SetIf("PIERCE", data.Pierce);
            SetIf("STAT DRAIN", data.StatDrain);
            SetIf("FORTIFY", data.Fortify);
            SetIf("CONSUME", data.Consume);
            SetIf("FOCUS", data.Focus);
            SetIf("CLENSE", data.Cleanse);
            SetIf("LIFESTEAL", data.Lifesteal);
            SetIf("REFLECT", data.Reflect);
            SetIf("SELF DAMAGE", data.SelfDamage);
            SetIf("HEAL", data.HeroHeal);
            SetIf("MAX HEALTH", data.HeroHealMaxHealth);
        }

        private static void WriteOptionalMechanics(SpreadsheetHeader h, string[] row, SpreadsheetActionData data)
        {
            void L(string label, string v) => h.SetCell(row, null, label, v);

            L("REPLACE NEXT ROLL", data.ReplaceNextRoll);
            L("HIGHEST/LOWEST ROLL", data.HighestLowestRoll);
            if (string.IsNullOrEmpty(rowValueAtLabel(h, row, null, "HIGHEST/LOWEST ROLL")))
                L("HIGHEST LOWEST ROLL", data.HighestLowestRoll);
            L("DICE ROLLS", data.DiceRolls);
            L("EXPLODING DICE THRESHOLD", data.ExplodingDiceThreshold);
            L("CURSE", data.Curse);
            L("SKIP", data.Skip);
            L("JUMP", data.Jump);
            L("DISRUPT", data.Disrupt);
            L("GRACE", data.Grace);
            L("LOOP CHAIN", data.LoopChain);
            L("SHUFFLE", data.Shuffle);
            L("REPLACE ACTION", data.ReplaceAction);
            L("CHAIN LENGTH", data.ChainLength);
            L("CHAIN POSITION", data.ChainPosition);
            L("MODIFY BASED ON CHAIN POISITION", data.ModifyBasedOnChainPosition);
            if (string.IsNullOrEmpty(rowValueAtLabel(h, row, null, "MODIFY BASED ON CHAIN POISITION")))
                L("MODIFY BASED ON CHAIN POSITION", data.ModifyBasedOnChainPosition);
            L("DISTANCE FROM X SLOT", data.DistanceFromXSlot);

            L("ON HIT", data.OnHit);
            L("ON MISS", data.OnMiss);
            L("ON CRIT", data.OnCrit);
            L("ON KILL", data.OnKill);
            L("ON ROOMS CLEARED", data.OnRoomsCleared);
            L("ON ROLL VALUE", data.OnRollValue);

            L("TRIGGER CONDITIONS", data.TriggerConditions);
            L("STAT BONUSES JSON", data.StatBonusesJson);
            L("THRESHOLDS JSON", data.ThresholdsJson);
            L("ACCUMULATIONS JSON", data.AccumulationsJson);
            L("CHAIN POSITION BONUSES JSON", data.ChainPositionBonusesJson);

            L("TARGET", data.Target);
            L("THRESHOLD CATEGORY", data.ThresholdCategory);
            L("THRESHOLD AMOUNT", data.ThresholdAmount);
            L("BONUS", data.Bonus);
            L("BONUS ATTRIBUTE", data.BonusAttribute);
            L("VALUE", data.Value);
            L("ATTRIBUTE", data.Attribute);
            L("RESET", data.Reset);
            L("RESET BLOCKER BUFFER", data.ResetBlockerBuffer);
            L("MODIFY ROOM", data.ModifyRoom);
            L("TAGS", data.Tags);
            L("IS DEFAULT ACTION", data.IsDefaultAction);
            L("WEAPON TYPES", data.WeaponTypes);
        }
    }
}
