using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// Converts SpreadsheetActionData to ActionData format
    /// </summary>
    public static class SpreadsheetToActionDataConverter
    {
        /// <summary>
        /// Converts a SpreadsheetActionData to ActionData
        /// </summary>
        public static ActionData Convert(SpreadsheetActionData spreadsheet)
        {
            var actionData = new ActionData();
            
            // Basic properties
            actionData.Name = spreadsheet.Action;
            actionData.Description = spreadsheet.Description;
            actionData.Type = DetermineActionType(spreadsheet);
            actionData.TargetType = DetermineTargetType(spreadsheet);
            
            // Damage and speed
            actionData.DamageMultiplier = SpreadsheetActionData.ParseNumericValue(spreadsheet.Damage);
            if (actionData.DamageMultiplier == 0)
            {
                actionData.DamageMultiplier = 1.0; // Default
            }
            
            actionData.Length = SpreadsheetActionData.ParseNumericValue(spreadsheet.Speed);
            if (actionData.Length == 0)
            {
                actionData.Length = 1.0; // Default
            }
            
            // Multi-hit
            actionData.MultiHitCount = SpreadsheetActionData.ParseIntValue(spreadsheet.NumberOfHits);
            if (actionData.MultiHitCount == 0)
            {
                actionData.MultiHitCount = 1; // Default
            }
            
            // Status effects
            actionData.CausesStun = !string.IsNullOrWhiteSpace(spreadsheet.Stun) && spreadsheet.Stun != "0";
            actionData.CausesPoison = !string.IsNullOrWhiteSpace(spreadsheet.Poison) && spreadsheet.Poison != "0";
            actionData.CausesBurn = !string.IsNullOrWhiteSpace(spreadsheet.Burn) && spreadsheet.Burn != "0";
            actionData.CausesBleed = !string.IsNullOrWhiteSpace(spreadsheet.Bleed) && spreadsheet.Bleed != "0";
            actionData.CausesWeaken = !string.IsNullOrWhiteSpace(spreadsheet.Weaken) && spreadsheet.Weaken != "0";
            actionData.CausesSlow = !string.IsNullOrWhiteSpace(spreadsheet.Slow) && spreadsheet.Slow != "0";
            
            // Advanced status effects
            actionData.CausesVulnerability = !string.IsNullOrWhiteSpace(spreadsheet.Vulnerability) && spreadsheet.Vulnerability != "0";
            actionData.CausesHarden = !string.IsNullOrWhiteSpace(spreadsheet.Harden) && spreadsheet.Harden != "0";
            actionData.CausesExpose = !string.IsNullOrWhiteSpace(spreadsheet.Expose) && spreadsheet.Expose != "0";
            actionData.CausesSilence = !string.IsNullOrWhiteSpace(spreadsheet.Silence) && spreadsheet.Silence != "0";
            actionData.CausesPierce = !string.IsNullOrWhiteSpace(spreadsheet.Pierce) && spreadsheet.Pierce != "0";
            actionData.CausesStatDrain = !string.IsNullOrWhiteSpace(spreadsheet.StatDrain) && spreadsheet.StatDrain != "0";
            actionData.CausesFortify = !string.IsNullOrWhiteSpace(spreadsheet.Fortify) && spreadsheet.Fortify != "0";
            actionData.CausesFocus = !string.IsNullOrWhiteSpace(spreadsheet.Focus) && spreadsheet.Focus != "0";
            actionData.CausesCleanse = !string.IsNullOrWhiteSpace(spreadsheet.Cleanse) && spreadsheet.Cleanse != "0";
            actionData.CausesReflect = !string.IsNullOrWhiteSpace(spreadsheet.Reflect) && spreadsheet.Reflect != "0";
            
            // Self damage
            if (!string.IsNullOrWhiteSpace(spreadsheet.SelfDamage))
            {
                actionData.SelfDamagePercent = (int)Math.Round(SpreadsheetActionData.ParseNumericValue(spreadsheet.SelfDamage) * 100);
            }
            
            // Heal
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal))
            {
                // This would need to be handled differently - heal actions are a different type
                // For now, we'll note it in the description or handle via tags
            }
            
            // Combo properties
            actionData.IsComboAction = !string.IsNullOrWhiteSpace(spreadsheet.Cadence);
            actionData.ComboOrder = 0; // Default, can be set based on chain position
            
            // Tags
            actionData.Tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(spreadsheet.Category))
            {
                actionData.Tags.Add(spreadsheet.Category.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(spreadsheet.Rarity))
            {
                actionData.Tags.Add(spreadsheet.Rarity.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(spreadsheet.Tags))
            {
                var tagList = spreadsheet.Tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tagList)
                {
                    actionData.Tags.Add(tag.Trim().ToLower());
                }
            }
            
            // ACTION/ATTACK bonuses
            actionData.ActionAttackBonuses = ActionAttackKeywordProcessor.ProcessBonuses(spreadsheet);
            
            // Debug output for actions with CADENCE but no bonuses
            if (!string.IsNullOrWhiteSpace(spreadsheet.Cadence) && 
                (actionData.ActionAttackBonuses == null || actionData.ActionAttackBonuses.BonusGroups.Count == 0))
            {
                // This will help identify why bonuses aren't being captured
                if (actionData.Name == "AMPLIFY ACCURACY" || actionData.Name == "CONCENTRATE" || actionData.Name == "GRUNT")
                {
                    Console.WriteLine($"DEBUG {actionData.Name}: CADENCE='{spreadsheet.Cadence}', HeroACC='{spreadsheet.HeroAccuracy}', HeroHIT='{spreadsheet.HeroHit}', EnemyACC='{spreadsheet.EnemyAccuracy}'");
                }
            }
            
            // Add keyword description to action description if bonuses exist
            if (actionData.ActionAttackBonuses != null && actionData.ActionAttackBonuses.BonusGroups.Count > 0)
            {
                string keywordString = ActionAttackKeywordProcessor.GenerateKeywordString(actionData.ActionAttackBonuses);
                if (!string.IsNullOrEmpty(keywordString))
                {
                    if (!string.IsNullOrEmpty(actionData.Description))
                    {
                        actionData.Description += " " + keywordString;
                    }
                    else
                    {
                        actionData.Description = keywordString;
                    }
                }
            }
            
            // Modifiers
            // SpeedMod, DamageMod, MultiHitMod, AmpMod would need special handling
            // These might affect the action's properties directly rather than being bonuses
            
            return actionData;
        }
        
        /// <summary>
        /// Determines the action type from spreadsheet data
        /// </summary>
        private static string DetermineActionType(SpreadsheetActionData spreadsheet)
        {
            // Check if it's a heal action
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal))
            {
                return "Heal";
            }
            
            // Default to Attack
            return "Attack";
        }
        
        /// <summary>
        /// Determines the target type from spreadsheet data
        /// </summary>
        private static string DetermineTargetType(SpreadsheetActionData spreadsheet)
        {
            if (!string.IsNullOrWhiteSpace(spreadsheet.Target))
            {
                string target = spreadsheet.Target.ToUpper();
                if (target == "SELF")
                    return "Self";
                if (target == "ENEMY")
                    return "SingleTarget";
            }
            
            // Default based on action type
            if (!string.IsNullOrWhiteSpace(spreadsheet.HeroHeal))
            {
                return "Self"; // Heal actions typically target self
            }
            
            return "SingleTarget"; // Default
        }
    }
}
