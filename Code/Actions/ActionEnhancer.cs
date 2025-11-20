using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Actions
{
    /// <summary>
    /// Enhances action descriptions with modifier information
    /// Consolidates description enhancement logic that was duplicated across the codebase
    /// Responsible for adding information about roll bonuses, damage multipliers, effects, etc.
    /// </summary>
    public class ActionEnhancer
    {
        /// <summary>
        /// Enhances an action description by appending modifier information
        /// </summary>
        /// <param name="data">The action data to enhance</param>
        /// <returns>Enhanced description with modifier information</returns>
        public static string EnhanceActionDescription(ActionData data)
        {
            if (data == null) return "";
            
            var modifiers = new List<string>();
            
            // Add roll bonus information
            AddRollBonusInfo(data, modifiers);
            
            // Add damage multiplier information
            AddDamageMultiplierInfo(data, modifiers);
            
            // Add combo bonus information
            AddComboBonusInfo(data, modifiers);
            
            // Add status effect information
            AddStatusEffectInfo(data, modifiers);
            
            // Add multi-hit information
            AddMultiHitInfo(data, modifiers);
            
            // Add self-damage information
            AddSelfDamageInfo(data, modifiers);
            
            // Add stat bonus information
            AddStatBonusInfo(data, modifiers);
            
            // Add special effects
            AddSpecialEffectInfo(data, modifiers);
            
            // Combine base description with modifiers
            if (modifiers.Count == 0)
            {
                return data.Description;
            }
            
            return $"{data.Description} ({string.Join(", ", modifiers)})";
        }
        
        /// <summary>
        /// Adds roll bonus information to the modifier list
        /// </summary>
        private static void AddRollBonusInfo(ActionData data, List<string> modifiers)
        {
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
        }
        
        /// <summary>
        /// Adds damage multiplier information to the modifier list
        /// </summary>
        private static void AddDamageMultiplierInfo(ActionData data, List<string> modifiers)
        {
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
        }
        
        /// <summary>
        /// Adds combo bonus information to the modifier list
        /// </summary>
        private static void AddComboBonusInfo(ActionData data, List<string> modifiers)
        {
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
        }
        
        /// <summary>
        /// Adds status effect information to the modifier list
        /// </summary>
        private static void AddStatusEffectInfo(ActionData data, List<string> modifiers)
        {
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
        }
        
        /// <summary>
        /// Adds multi-hit attack information to the modifier list
        /// </summary>
        private static void AddMultiHitInfo(ActionData data, List<string> modifiers)
        {
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
        }
        
        /// <summary>
        /// Adds self-damage information to the modifier list
        /// </summary>
        private static void AddSelfDamageInfo(ActionData data, List<string> modifiers)
        {
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
        }
        
        /// <summary>
        /// Adds stat bonus information to the modifier list
        /// </summary>
        private static void AddStatBonusInfo(ActionData data, List<string> modifiers)
        {
            if (data.StatBonus > 0 && !string.IsNullOrEmpty(data.StatBonusType))
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                modifiers.Add($"+{data.StatBonus} {data.StatBonusType} ({durationText})");
            }
        }
        
        /// <summary>
        /// Adds special effect information to the modifier list
        /// </summary>
        private static void AddSpecialEffectInfo(ActionData data, List<string> modifiers)
        {
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skip next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeat last action");
            }
            
            if (data.EnemyRollPenalty != 0)
            {
                string penaltyText = data.EnemyRollPenalty > 0 ? 
                    $"-{data.EnemyRollPenalty}" : 
                    $"+{Math.Abs(data.EnemyRollPenalty)}";
                modifiers.Add($"Enemy roll: {penaltyText}");
            }
        }
    }
}

