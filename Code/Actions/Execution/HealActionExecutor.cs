using RPGGame.Combat;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;
using Avalonia.Media;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Executes heal actions and returns ColoredText results
    /// Handles healing calculation, application, and formatting
    /// </summary>
    public static class HealActionExecutor
    {
        /// <summary>
        /// Executes a heal action and returns ColoredText results
        /// </summary>
        public static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteHealActionColored(
            Actor source, 
            Actor target, 
            Action selectedAction, 
            int baseRoll, 
            int rollBonus, 
            BattleNarrative? battleNarrative)
        {
            int healAmount = ActionUtilities.CalculateHealAmount(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            
            ActionUtilities.ApplyHealing(target, healAmount);
            
            if (!ActionExecutor.DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} healed {target.Name} for {healAmount} health");
            }
            
            // Track statistics
            if (target is Character targetCharacter)
            {
                ActionStatisticsTracker.RecordHealingReceived(targetCharacter, healAmount);
            }
            
            bool isCombo = selectedAction.Name != "BASIC ATTACK";
            ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, totalRoll, rollBonus, true, isCombo, healAmount, 0, false, battleNarrative);
            
            var healingText = CombatResults.FormatHealingMessageColored(source, target, healAmount);
            
            // Create a simple roll info for healing (no attack/defense, just roll and speed)
            var rollInfoBuilder = new ColoredTextBuilder();
            rollInfoBuilder.Add("    (", Colors.Gray);
            rollInfoBuilder.Add("roll:", ColorPalette.Info);
            rollInfoBuilder.AddSpace();
            rollInfoBuilder.Add(baseRoll.ToString(), Colors.White);
            
            if (rollBonus != 0)
            {
                if (rollBonus > 0)
                {
                    rollInfoBuilder.Add(" + ", Colors.White);
                    rollInfoBuilder.Add(rollBonus.ToString(), ColorPalette.Success);
                }
                else
                {
                    rollInfoBuilder.Add(" - ", Colors.White);
                    rollInfoBuilder.Add((-rollBonus).ToString(), ColorPalette.Error);
                }
                rollInfoBuilder.Add(" = ", Colors.White);
                rollInfoBuilder.Add(totalRoll.ToString(), Colors.White);
            }
            
            if (selectedAction != null && selectedAction.Length > 0)
            {
                double actualSpeed = 0;
                if (source is Character charSource)
                {
                    actualSpeed = charSource.GetTotalAttackSpeed() * selectedAction.Length;
                }
                else if (source is Enemy enemySource)
                {
                    actualSpeed = enemySource.GetTotalAttackSpeed() * selectedAction.Length;
                }
                
                if (actualSpeed > 0)
                {
                    rollInfoBuilder.Add("|", Colors.Gray);
                    rollInfoBuilder.AddSpace();
                    rollInfoBuilder.Add("speed: ", ColorPalette.Info);
                    rollInfoBuilder.Add($"{actualSpeed:F1}s", Colors.White);
                }
            }
            
            rollInfoBuilder.Add(")", Colors.Gray);
            
            return (healingText, rollInfoBuilder.Build());
        }
    }
}

