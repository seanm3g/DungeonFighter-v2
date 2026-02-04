using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Processes ABILITY/ACTION keywords from spreadsheet data and generates bonus structures.
    /// ABILITY = bonuses consumed on successful ability use; ACTION = bonuses consumed per attack roll.
    /// </summary>
    public static class ActionAttackKeywordProcessor
    {
        /// <summary>
        /// Processes a SpreadsheetActionData and extracts ABILITY/ACTION bonuses
        /// </summary>
        public static ActionAttackBonuses ProcessBonuses(SpreadsheetActionData spreadsheetData)
        {
            var bonuses = new ActionAttackBonuses();
            
            // Get action name once
            string actionName = spreadsheetData.Action?.Trim() ?? "";
            
            if (string.IsNullOrWhiteSpace(spreadsheetData.Cadence))
            {
                return bonuses; // No keyword bonuses
            }
            
            string cadence = spreadsheetData.Cadence.Trim().ToUpper();
            int duration = SpreadsheetActionData.ParseIntValue(spreadsheetData.Duration);
            // Default duration for ABILITY or ACTION keyword
            if (duration == 0 && (cadence == "ABILITY" || cadence == "ABILITIES" || cadence == "ACTION" || cadence == "ACTIONS" || cadence == "ATTACK" || cadence == "ATTACKS"))
            {
                duration = 1; // Default to 1 if not specified
            }
            
            // Determine keyword type: legacy "ACTION"/"ACTIONS" -> ABILITY; "ATTACK"/"ATTACKS" -> ACTION
            string keyword = "";
            if (cadence == "ABILITY" || cadence == "ABILITIES" || cadence == "ACTION" || cadence == "ACTIONS")
            {
                keyword = "ABILITY";
            }
            else if (cadence == "ATTACK" || cadence == "ATTACKS")
            {
                keyword = "ACTION";
            }
            else
            {
                // Other duration types (FIGHT, DUNGEON, CHAIN, COMBO) - handle separately
                return ProcessSpecialDurationBonuses(spreadsheetData, cadence);
            }
            
            // Collect all bonuses
            var bonusItems = new List<ActionAttackBonusItem>();
            
            // Roll-based bonuses
            // Check Hero columns first
            AddBonusIfPresent(bonusItems, "ACCURACY", spreadsheetData.HeroAccuracy);
            AddBonusIfPresent(bonusItems, "HIT", spreadsheetData.HeroHit);
            AddBonusIfPresent(bonusItems, "COMBO", spreadsheetData.HeroCombo);
            AddBonusIfPresent(bonusItems, "CRIT", spreadsheetData.HeroCrit);
            
            // Fallback: If Hero columns are empty but Enemy columns have values, map them to Hero bonuses
            // Some CSV exports may have bonus values in Enemy columns that should be Hero bonuses
            // Common pattern: single value in Enemy ACCUARCY column (index 16) should be Hero HIT bonus
            if (bonusItems.Count == 0)
            {
                // Check if any Enemy bonus columns have values
                // Map Enemy ACCUARCY -> Hero HIT (most common case based on CSV structure)
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyAccuracy))
                {
                    AddBonusIfPresent(bonusItems, "HIT", spreadsheetData.EnemyAccuracy);
                }
                // Also check other Enemy columns
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyHit))
                {
                    AddBonusIfPresent(bonusItems, "COMBO", spreadsheetData.EnemyHit);
                }
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyCombo))
                {
                    AddBonusIfPresent(bonusItems, "CRIT", spreadsheetData.EnemyCombo);
                }
                if (!string.IsNullOrWhiteSpace(spreadsheetData.EnemyCrit))
                {
                    AddBonusIfPresent(bonusItems, "ACCURACY", spreadsheetData.EnemyCrit);
                }
            }
            
            // Stat bonuses
            AddBonusIfPresent(bonusItems, "STR", spreadsheetData.HeroSTR);
            AddBonusIfPresent(bonusItems, "AGI", spreadsheetData.HeroAGI);
            AddBonusIfPresent(bonusItems, "TECH", spreadsheetData.HeroTECH);
            AddBonusIfPresent(bonusItems, "INT", spreadsheetData.HeroINT);
            
            if (bonusItems.Count > 0)
            {
                var group = new ActionAttackBonusGroup
                {
                    Keyword = keyword,
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
            
            // For special durations, we might need to apply bonuses differently
            // For now, treat them as ACTION bonuses with special duration type
            var bonusItems = new List<ActionAttackBonusItem>();
            
            AddBonusIfPresent(bonusItems, "ACCURACY", spreadsheetData.HeroAccuracy);
            AddBonusIfPresent(bonusItems, "HIT", spreadsheetData.HeroHit);
            AddBonusIfPresent(bonusItems, "COMBO", spreadsheetData.HeroCombo);
            AddBonusIfPresent(bonusItems, "CRIT", spreadsheetData.HeroCrit);
            AddBonusIfPresent(bonusItems, "STR", spreadsheetData.HeroSTR);
            AddBonusIfPresent(bonusItems, "AGI", spreadsheetData.HeroAGI);
            AddBonusIfPresent(bonusItems, "TECH", spreadsheetData.HeroTECH);
            AddBonusIfPresent(bonusItems, "INT", spreadsheetData.HeroINT);
            
            if (bonusItems.Count > 0)
            {
                int duration = SpreadsheetActionData.ParseIntValue(spreadsheetData.Duration);
                if (duration == 0) duration = 1;
                
                var group = new ActionAttackBonusGroup
                {
                    Keyword = "ABILITY", // Default to ABILITY for special durations
                    Count = duration,
                    Bonuses = bonusItems,
                    DurationType = durationType
                };
                
                bonuses.BonusGroups.Add(group);
            }
            
            return bonuses;
        }
        
        /// <summary>
        /// Adds a bonus item if the value is present and non-zero
        /// </summary>
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
        
        /// <summary>
        /// Generates a human-readable keyword string for display
        /// </summary>
        public static string GenerateKeywordString(ActionAttackBonuses bonuses)
        {
            if (bonuses.BonusGroups.Count == 0)
                return "";
            
            var parts = new List<string>();
            
            foreach (var group in bonuses.BonusGroups)
            {
                var keywordText = group.Keyword;
                if (group.Count > 1)
                {
                    keywordText = group.Count + " " + (group.Keyword == "ABILITY" ? "ABILITIES" : "ACTIONS");
                }
                else
                {
                    keywordText = "the Next " + group.Keyword;
                }
                
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
