using RPGGame;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Tracks statistics for action execution
    /// Handles recording of actions, damage, healing, combos, and one-shot kills
    /// </summary>
    public static class ActionStatisticsTracker
    {
        /// <summary>
        /// Records statistics for a successful attack action
        /// </summary>
        public static void RecordAttackAction(Character character, int totalRoll, int baseRoll, int rollBonus, int damage, Action selectedAction, Enemy? enemyTarget)
        {
            if (character == null) return;
            
            bool isCritical = totalRoll >= 20;
            bool isCriticalMiss = baseRoll == 1; // Natural 1 only
            
            character.RecordAction(true, isCritical, isCriticalMiss);
            character.RecordDamageDealt(damage, isCritical);
            
            bool isComboAction = selectedAction.Name != "BASIC ATTACK";
            if (isComboAction)
            {
                character.RecordCombo(character.ComboStep, damage);
            }
            
            if (enemyTarget != null && !enemyTarget.IsAlive && damage >= enemyTarget.GetEffectiveMaxHealth())
            {
                character.RecordOneShotKill();
            }
        }
        
        /// <summary>
        /// Records statistics for damage received
        /// </summary>
        public static void RecordDamageReceived(Character targetCharacter, int damage)
        {
            if (targetCharacter == null) return;
            
            targetCharacter.RecordDamageReceived(damage);
            targetCharacter.RecordHealthStatus(targetCharacter.GetHealthPercentage());
        }
        
        /// <summary>
        /// Records statistics for a miss
        /// </summary>
        public static void RecordMissAction(Character character, int baseRoll, int rollBonus)
        {
            if (character == null) return;
            
            bool isCriticalMiss = baseRoll == 1; // Natural 1 only
            character.RecordAction(false, false, isCriticalMiss);
        }
        
        /// <summary>
        /// Records statistics for healing received
        /// </summary>
        public static void RecordHealingReceived(Character targetCharacter, int healAmount)
        {
            if (targetCharacter == null) return;
            
            targetCharacter.RecordHealingReceived(healAmount);
        }
    }
}

