using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Enhances action descriptions with modifier information
    /// </summary>
    public static class ActionDescriptionEnhancer
    {
        public static string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();
            
            // Add roll bonus information
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
            
            // Add damage multiplier information
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
            
            // Add combo bonus information
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
            
            // Add status effect information
            if (data.CausesBleed)
            {
                modifiers.Add("Causes Bleed");
            }
            
            if (data.CausesWeaken)
            {
                modifiers.Add("Causes Weaken");
            }
            
            if (data.CausesSlow)
            {
                modifiers.Add("Causes Slow");
            }
            
            if (data.CausesPoison)
            {
                modifiers.Add("Causes Poison");
            }
            
            // Add multi-hit information
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
            
            // Add self-damage information
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
            
            // Add stat bonus information (list or legacy single)
            var statEntries = GetStatBonusEntries(data);
            if (statEntries.Count > 0)
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                foreach (var entry in statEntries)
                {
                    // Do not display stat bonus of 0 or with empty type
                    if (entry.Value == 0 || string.IsNullOrEmpty(entry.Type)) continue;
                    modifiers.Add($"+{entry.Value} {entry.Type} ({durationText})");
                }
            }
            
            // Add special effects
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skips next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeats last action");
            }
            
            // Combine base description with modifiers
            string result = data.Description;
            if (modifiers.Count > 0)
            {
                result += $" | {string.Join(", ", modifiers)}";
            }
            
            return result;
        }

        private static List<StatBonusEntry> GetStatBonusEntries(ActionData data)
        {
            if (data == null) return new List<StatBonusEntry>();
            if (data.StatBonuses != null && data.StatBonuses.Count > 0)
                return data.StatBonuses;
            // Only add legacy single stat bonus when both value and type are meaningful (no 0, no empty type)
            if (data.StatBonus != 0 && !string.IsNullOrEmpty(data.StatBonusType))
                return new List<StatBonusEntry> { new StatBonusEntry { Value = data.StatBonus, Type = data.StatBonusType ?? "" } };
            return new List<StatBonusEntry>();
        }
    }
}

