using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Processes TURN/ACTION cadence keywords from spreadsheet data and generates bonus structures.
    /// TURN = bonuses consumed per roll; ACTION = bonuses queued as FIFO combo layers.
    /// </summary>
    public static class ActionAttackKeywordProcessor
    {
        /// <summary>
        /// Processes a SpreadsheetActionData and extracts TURN/ACTION bonuses
        /// </summary>
        public static ActionAttackBonuses ProcessBonuses(SpreadsheetActionData spreadsheetData)
        {
            var bonuses = new ActionAttackBonuses();
            
            if (string.IsNullOrWhiteSpace(spreadsheetData.Cadence))
            {
                return bonuses;
            }
            
            string cadence = CadenceKeywords.NormalizeFromRow(spreadsheetData.Cadence, spreadsheetData);
            int duration = SpreadsheetDurationSemantics.ResolveCadenceDuration(spreadsheetData);
            if (duration == 0 && CadenceKeywords.IsKeywordCadence(cadence))
            {
                duration = 1;
            }
            
            string keyword = "";
            string cadenceType = "";
            if (CadenceKeywords.IsAction(cadence))
            {
                keyword = CadenceKeywords.Action;
                cadenceType = CadenceKeywords.Action;
            }
            else if (CadenceKeywords.IsTurn(cadence))
            {
                keyword = CadenceKeywords.Turn;
                cadenceType = CadenceKeywords.Turn;
            }
            else
            {
                return ProcessSpecialDurationBonuses(spreadsheetData, cadence);
            }
            
            var bonusItems = CollectBonusItems(spreadsheetData);
            
            if (bonusItems.Count > 0)
            {
                var group = new ActionAttackBonusGroup
                {
                    Keyword = keyword,
                    CadenceType = cadenceType,
                    Count = duration,
                    Bonuses = bonusItems,
                    DurationType = cadence
                };
                
                bonuses.BonusGroups.Add(group);
            }
            
            return bonuses;
        }
        
        /// <summary>
        /// Processes bonuses with special duration types (FIGHT, DUNGEON, CHAIN, COMBO)
        /// </summary>
        private static ActionAttackBonuses ProcessSpecialDurationBonuses(SpreadsheetActionData spreadsheetData, string durationType)
        {
            var bonuses = new ActionAttackBonuses();
            var bonusItems = CollectBonusItems(spreadsheetData);
            
            if (bonusItems.Count > 0)
            {
                int duration = SpreadsheetDurationSemantics.ResolveCadenceDuration(spreadsheetData);
                if (duration == 0) duration = 1;
                
                var group = new ActionAttackBonusGroup
                {
                    Keyword = CadenceKeywords.Turn,
                    CadenceType = CadenceKeywords.Turn,
                    Count = duration,
                    Bonuses = bonusItems,
                    DurationType = durationType
                };
                
                bonuses.BonusGroups.Add(group);
            }
            
            return bonuses;
        }

        private static List<ActionAttackBonusItem> CollectBonusItems(SpreadsheetActionData spreadsheetData)
        {
            var bonusItems = new List<ActionAttackBonusItem>();
            
            AddBonusIfPresent(bonusItems, "ACCURACY", spreadsheetData.HeroAccuracy);
            AddBonusIfPresent(bonusItems, "HIT", spreadsheetData.HeroHit);
            AddBonusIfPresent(bonusItems, "COMBO", spreadsheetData.HeroCombo);
            AddBonusIfPresent(bonusItems, "CRIT", spreadsheetData.HeroCrit);
            
            if (bonusItems.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyAccuracy))
                    AddBonusIfPresent(bonusItems, "HIT", spreadsheetData.EnemyAccuracy);
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyHit))
                    AddBonusIfPresent(bonusItems, "COMBO", spreadsheetData.EnemyHit);
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyCombo))
                    AddBonusIfPresent(bonusItems, "CRIT", spreadsheetData.EnemyCombo);
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyCrit))
                    AddBonusIfPresent(bonusItems, "ACCURACY", spreadsheetData.EnemyCrit);
            }
            
            AddBonusIfPresent(bonusItems, "STR", spreadsheetData.HeroSTR);
            AddBonusIfPresent(bonusItems, "AGI", spreadsheetData.HeroAGI);
            AddBonusIfPresent(bonusItems, "TECH", spreadsheetData.HeroTECH);
            AddBonusIfPresent(bonusItems, "INT", spreadsheetData.HeroINT);
            
            if (ModifierParser.ParsePercent(spreadsheetData.SpeedMod) is { } sv) bonusItems.Add(new ActionAttackBonusItem { Type = "SPEED_MOD", Value = sv * 100.0 });
            if (ModifierParser.ParsePercent(spreadsheetData.DamageMod) is { } dv) bonusItems.Add(new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = dv * 100.0 });
            if (ModifierParser.ParseValue(spreadsheetData.MultiHitMod) is { } mv) bonusItems.Add(new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = mv });
            if (ModifierParser.ParsePercent(spreadsheetData.AmpMod) is { } av) bonusItems.Add(new ActionAttackBonusItem { Type = "AMP_MOD", Value = av * 100.0 });

            return bonusItems;
        }
        
        private static void AddBonusIfPresent(List<ActionAttackBonusItem> bonuses, string type, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            
            double numericValue = SpreadsheetActionData.ParseNumericValue(value);
            if (numericValue != 0.0)
            {
                bonuses.Add(new ActionAttackBonusItem
                {
                    Type = type,
                    Value = numericValue
                });
            }
        }
        
        public static string GenerateKeywordString(ActionAttackBonuses bonuses)
        {
            if (bonuses.BonusGroups.Count == 0)
                return "";
            
            var parts = new List<string>();
            
            foreach (var group in bonuses.BonusGroups)
            {
                var cadence = CadenceKeywords.NormalizeCadenceType(
                    string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType);
                var keywordText = CadenceKeywords.GetPluralDurationPhrase(cadence, group.Count);
                
                var bonusStrings = group.Bonuses.Select(b => 
                {
                    string sign = b.Value >= 0 ? "+" : "";
                    return $"{sign}{b.Value} {b.Type}";
                });
                
                parts.Add($"For {keywordText}: {string.Join(", ", bonusStrings)}");
            }
            
            return string.Join("; ", parts);
        }
    }
}
