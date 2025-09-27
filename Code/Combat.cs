using System;
using System.Threading;

namespace RPGGame
{
    public static class Combat
    {
        private static BattleNarrative? currentBattleNarrative;
        private static Action? lastPlayerAction = null; // Track the last action for DEJA VU
        private static ActionSpeedSystem? currentActionSpeedSystem = null;
        private static BattleHealthTracker? currentHealthTracker = null;

        public static void StartBattleNarrative(string playerName, string enemyName, string locationName, int playerHealth, int enemyHealth)
        {
            currentBattleNarrative = new BattleNarrative(playerName, enemyName, locationName, playerHealth, enemyHealth);
            lastPlayerAction = null; // Reset last action for new battle
            currentActionSpeedSystem = new ActionSpeedSystem();
        }

        public static void EndBattleNarrative()
        {
            if (currentBattleNarrative != null)
            {
                currentBattleNarrative = null;
            }
            currentActionSpeedSystem = null;
        }

        public static BattleNarrative? GetCurrentBattleNarrative()
        {
            return currentBattleNarrative;
        }

        public static ActionSpeedSystem? GetCurrentActionSpeedSystem()
        {
            return currentActionSpeedSystem;
        }

        public static void InitializeCombatEntities(Character player, Enemy enemy, Environment? environment = null)
        {
            if (currentActionSpeedSystem == null) return;

            // New system: Use the attack speed directly as base speed
            double playerAttackSpeed = player.GetTotalAttackSpeed();
            double enemyAttackSpeed = enemy.GetTotalAttackSpeed();
            
            // For the new system, we use the attack speed directly as the base speed
            // This will be multiplied by action length in ExecuteAction
            currentActionSpeedSystem.AddEntity(player, playerAttackSpeed);
            currentActionSpeedSystem.AddEntity(enemy, enemyAttackSpeed);

            // Add environment to action speed system with a slow base speed (longer cooldowns)
            if (environment != null && environment.IsHostile)
            {
                double environmentBaseSpeed = 15.0; // Very slow - environment acts infrequently
                currentActionSpeedSystem.AddEntity(environment, environmentBaseSpeed);
            }
            
            // Initialize health tracker for battle participants
            var participants = new List<Entity> { player, enemy };
            if (environment != null && environment.IsHostile)
            {
                participants.Add(environment);
            }
            currentHealthTracker = new BattleHealthTracker();
            currentHealthTracker.InitializeBattle(participants);
        }

        /// <summary>
        /// Executes multiple attacks per turn based on the source's attack speed
        /// </summary>
        /// <param name="source">The entity performing the attacks</param>
        /// <param name="target">The entity receiving the attacks</param>
        /// <param name="environment">The environment affecting the attacks</param>
        /// <returns>A string describing the results of all attacks</returns>
        public static string ExecuteMultiAttack(Entity source, Entity target, Environment? environment = null)
        {
            if (source is Character character)
            {
                int attacksPerTurn = character.GetAttacksPerTurn();
                var results = new List<string>();
                
                for (int i = 0; i < attacksPerTurn; i++)
                {
                    // Check if target is alive (only Character and Enemy have IsAlive property)
                    bool isAlive = true;
                    if (target is Character targetCharacter)
                        isAlive = targetCharacter.IsAlive;
                    else if (target is Enemy targetEnemy)
                        isAlive = targetEnemy.IsAlive;
                    
                    if (!isAlive) break; // Stop if target is dead
                    
                    string result = ExecuteAction(source, target, environment);
                    if (!string.IsNullOrEmpty(result))
                    {
                        results.Add(result);
                    }
                }
                
                return string.Join("\n", results);
            }
            else
            {
                // For non-character entities, just execute one action
                return ExecuteAction(source, target, environment);
            }
        }

        /// <summary>
        /// Handles Divine reroll functionality for failed attacks
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="baseRoll">The original dice roll</param>
        /// <param name="totalRollBonus">Total roll bonus</param>
        /// <returns>New roll result if reroll was used, otherwise original roll</returns>
        private static (int newRoll, bool rerollUsed) HandleDivineReroll(Character player, int baseRoll, int totalRollBonus)
        {
            // Check if player has Divine reroll charges available
            if (player.GetRemainingRerollCharges() > 0)
            {
                // Use a Divine reroll charge
                if (player.UseRerollCharge())
                {
                    // Roll a new d20 and apply bonuses
                    int newBaseRoll = Dice.Roll(1, 20);
                    int newAttackRoll = newBaseRoll + totalRollBonus;
                    int newCappedRoll = Math.Min(newAttackRoll, 20);
                    
                    WriteCombatLog($"[{player.Name}] uses Divine Reroll! ({player.GetRemainingRerollCharges()} charges remaining)");
                    
                    return (newCappedRoll, true);
                }
            }
            
            // No reroll available or used
            return (baseRoll + totalRollBonus, false);
        }

        /// <summary>
        /// Executes a combat action from the source entity to the target entity.
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The entity receiving the action</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <returns>A string describing the result of the action (or empty if using narrative mode)</returns>
        public static string ExecuteAction(Entity source, Entity target, Environment? environment = null)
        {
            // Get the selected action for all entities
            var selectedAction = source.SelectAction();
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }
            
            // Initialize results list for all entities
            var results = new List<string>();
            
            // Initialize environment message for all entities
            string envMsg = "";
            
            // Initialize final effect for all entities
            int finalEffect = 0;
            
            // Only run combo logic for the player (not for enemies or other entities)
            if (source is Character player && !(player is Enemy))
            {
                // Check for unique action chance for player characters
                double uniqueActionChance = player.GetModificationUniqueActionChance();
                if (uniqueActionChance > 0.0)
                {
                    double roll = Dice.Roll(1, 100) / 100.0; // Roll 0.0 to 0.99
                    if (roll < uniqueActionChance)
                    {
                        // Unique action chance triggered - select a unique action
                        var availableUniqueActions = player.GetAvailableUniqueActions();
                        if (availableUniqueActions.Count > 0)
                        {
                            // Randomly select a unique action
                            int randomIndex = Dice.Roll(1, availableUniqueActions.Count) - 1;
                            selectedAction = availableUniqueActions[randomIndex];
                            WriteCombatLog($"[{player.Name}] channels unique power and uses [{selectedAction.Name}]!");
                        }
                    }
                }
                
                // Check if this is a unique action (not part of normal combo system)
                bool isUniqueAction = selectedAction.Tags.Contains("unique");
                
                // Get the current action to check for roll bonuses
                var comboActions = player.GetComboActions();
                int actionIndex = comboActions.Count > 0 ? player.ComboStep % comboActions.Count : 0;
                var currentAction = comboActions.Count > 0 ? comboActions[actionIndex] : null;
                
                // For unique actions, use the selected action instead of current combo action
                if (isUniqueAction)
                {
                    currentAction = selectedAction;
                }
                
                // Apply roll bonus from the action and Intelligence
                int actionRollBonus = currentAction?.RollBonus ?? 0;
                
                // Check for different combo scaling types
                if (currentAction != null)
                {
                    if (currentAction.Tags.Contains("comboScaling"))
                    {
                        // Scale with combo length (METEOR, SHADOW STRIKE)
                        actionRollBonus = player.ComboSequence.Count;
                    }
                    else if (currentAction.Tags.Contains("comboStepScaling"))
                    {
                        // Scale with combo step position (HEROIC STRIKE)
                        actionRollBonus = (player.ComboStep % player.ComboSequence.Count) + 1;
                    }
                    else if (currentAction.Tags.Contains("comboAmplificationScaling"))
                    {
                        // Scale with combo amplification (BERSERKER RAGE)
                        actionRollBonus = (int)(player.GetComboAmplifier() * 2); // +2 per amplification level
                    }
                }
                
                // For unique actions, don't advance combo step (they're standalone)
                if (isUniqueAction)
                {
                    // Unique actions don't advance the combo system
                    // They're executed as standalone actions
                }
                
                int intelligenceRollBonus = player.GetIntelligenceRollBonus();
                int modificationRollBonus = player.GetModificationRollBonus();
                int equipmentRollBonus = player.GetEquipmentRollBonus();
                int totalRollBonus = actionRollBonus + intelligenceRollBonus + modificationRollBonus + equipmentRollBonus;
                
                // Apply roll penalty (for effects like Dust Cloud)
                int rollPenaltyApplied = player.RollPenalty;
                totalRollBonus -= rollPenaltyApplied;
                
                // Handle unique actions separately (they don't use the normal combo system)
                if (isUniqueAction)
                {
                    // Unique actions are executed as standalone actions with their own damage calculation
                    int uniqueBaseRoll = Dice.Roll(1, 20);
                    int uniqueAttackRoll = uniqueBaseRoll + totalRollBonus;
                    int uniqueCappedRoll = Math.Min(uniqueAttackRoll, 20);
                    
                    var uniqueRollTuning = TuningConfig.Instance;
                    
                    // Check if the unique action hits
                    if (uniqueCappedRoll >= uniqueRollTuning.RollSystem.BasicAttackThreshold.Min)
                    {
                        // Calculate damage for unique action
                        int uniqueDamage = CalculateDamage(player, target, selectedAction, 1.0, 1.0, totalRollBonus, uniqueAttackRoll);
                        if (target is Character uniqueTargetChar)
                        {
                            uniqueTargetChar.TakeDamage(uniqueDamage);
                        }
                        // Check for bleed chance after damage
                        CheckAndApplyBleedChance(player, target);
                        
                        string uniqueRollText = totalRollBonus != 0 ? $"{uniqueBaseRoll} + {totalRollBonus} = {uniqueAttackRoll}" : uniqueAttackRoll.ToString();
                        int actualUniqueDamage = CalculateDamage(player, target, selectedAction, 1.0, 1.0, totalRollBonus, uniqueAttackRoll, false);
                        string uniqueDamageDisplay = FormatDamageDisplay(player, target, uniqueDamage, actualUniqueDamage, selectedAction, 1.0, 1.0, totalRollBonus, uniqueAttackRoll);
                        string uniqueArmorBreakdown = GetArmorBreakdown(player, target, actualUniqueDamage);
                        return $"[{player.Name}] uses [Unique {selectedAction.Name}] on [{target.Name}]: deals {uniqueDamageDisplay}.\n        (Rolled {uniqueRollText}{uniqueArmorBreakdown})";
                    }
                    else
                    {
                        // Unique action missed
                        string uniqueMissRollText = totalRollBonus != 0 ? $"{uniqueBaseRoll} + {totalRollBonus} = {uniqueAttackRoll}" : uniqueAttackRoll.ToString();
                        return $"[{player.Name}] attempts to use [Unique {selectedAction.Name}] but misses. (Rolled {uniqueMissRollText})";
                    }
                }
                
                // Check for autoSuccess modifications
                if (player.HasAutoSuccess())
                {
                    // Auto success - treat as critical hit
                    int autoSuccessRoll = 20;
                    var autoSuccessComboActions = player.GetComboActions();
                    int autoSuccessActionIndex = autoSuccessComboActions.Count > 0 ? player.ComboStep % autoSuccessComboActions.Count : 0;
                    var autoSuccessCurrentAction = autoSuccessComboActions.Count > 0 ? autoSuccessComboActions[autoSuccessActionIndex] : null;
                    
                    if (autoSuccessComboActions.Count == 0)
                    {
                        // No combo actions available, do critical basic attack
                        int criticalDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, autoSuccessRoll);
                        if (target is Character criticalTargetChar)
                        {
                            criticalTargetChar.TakeDamage(criticalDamage);
                        }
                        // Check for bleed chance after damage
                        CheckAndApplyBleedChance(player, target);
                        int actualCriticalDamage = CalculateDamage(player, target, null, 1.0, 1.0, 0, 20, false);
                        string criticalDamageDisplay = FormatDamageDisplay(player, target, criticalDamage, actualCriticalDamage, null, 1.0, 1.0, 0, 20);
                        string criticalArmorBreakdown = GetArmorBreakdown(player, target, actualCriticalDamage);
                        return $"[{player.Name}] uses [Perfect Auto-Success Attack] on [{target.Name}]: deals {criticalDamageDisplay}.\n        (AUTO-SUCCESS!{criticalArmorBreakdown})";
                    }
                    
                    // Execute critical combo action
                    var criticalAction = autoSuccessComboActions[autoSuccessActionIndex];
                    player.ComboStep++;
                    
                    // Calculate exponential combo amplification: amp^comboStep
                    double criticalBaseComboAmp = player.GetComboAmplifier();
                    double criticalExponentialComboAmp = Math.Pow(criticalBaseComboAmp, autoSuccessActionIndex + 1);
                    
                    int criticalComboDamage = CalculateDamage(player, target, criticalAction, criticalExponentialComboAmp, 1.0, totalRollBonus, autoSuccessRoll);
                    if (target is Character criticalComboTargetChar)
                    {
                        criticalComboTargetChar.TakeDamage(criticalComboDamage);
                    }
                    // Check for bleed chance after damage
                    CheckAndApplyBleedChance(player, target);
                    int actualCriticalComboDamage = CalculateDamage(player, target, criticalAction, criticalExponentialComboAmp, 1.0, 0, 20, false);
                    string criticalComboDamageDisplay = FormatDamageDisplay(player, target, criticalComboDamage, actualCriticalComboDamage, criticalAction, criticalExponentialComboAmp, 1.0, 0, 20);
                    string criticalComboArmorBreakdown = GetArmorBreakdown(player, target, actualCriticalComboDamage);
                    return $"[{player.Name}] uses [Perfect {criticalAction.Name}] on [{target.Name}]: deals {criticalComboDamageDisplay}.\n        (AUTO-SUCCESS!, combo step {autoSuccessActionIndex + 1}, {criticalExponentialComboAmp:F2}x amplification{criticalComboArmorBreakdown})";
                }
                
                // Implement the 1d20 attack system with bonus
                int baseRoll = Dice.Roll(1, 20);
                int attackRoll = baseRoll + totalRollBonus;
                
                // Cap attack roll at 20 for critical hit calculation
                int cappedRoll = Math.Min(attackRoll, 20);
                
                var rollTuning = TuningConfig.Instance;
                
                if (cappedRoll >= rollTuning.RollSystem.MissThreshold.Min && cappedRoll <= rollTuning.RollSystem.MissThreshold.Max)
                {
                    // Check for Divine reroll on miss
                    var (newRoll, rerollUsed) = HandleDivineReroll(player, baseRoll, totalRollBonus);
                    
                    if (rerollUsed)
                    {
                        // Check if reroll succeeded
                        if (newRoll >= rollTuning.RollSystem.BasicAttackThreshold.Min)
                        {
                            // Reroll succeeded - continue with basic attack
                            player.ResetCombo();
                        int basicDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, newRoll);
                        if (target is Character basicTargetChar)
                        {
                            basicTargetChar.TakeDamage(basicDamage);
                        }
                        // Check for bleed chance after damage
                        CheckAndApplyBleedChance(player, target);
                            string originalRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            string rerollText = totalRollBonus != 0 ? $"{newRoll - totalRollBonus} + {totalRollBonus} = {newRoll}" : newRoll.ToString();
                            int actualRerollDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, newRoll, false);
                            string rerollDamageDisplay = FormatDamageDisplay(player, target, basicDamage, actualRerollDamage, null, 1.0, 1.0, totalRollBonus, newRoll);
                            return $"[{player.Name}] attempts to attack but misses, then uses Divine reroll! (Original: {originalRollText}, Reroll: {rerollText}) - deals {rerollDamageDisplay}.";
                        }
                        else
                        {
                            // Reroll also failed
                            player.ResetCombo();
                            string originalRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            string rerollText = totalRollBonus != 0 ? $"{newRoll - totalRollBonus} + {totalRollBonus} = {newRoll}" : newRoll.ToString();
                            return $"[{player.Name}] attempts to attack but misses, then uses Divine reroll but still fails! (Original: {originalRollText}, Reroll: {rerollText})";
                        }
                    }
                    else
                    {
                        // No reroll available - normal miss
                        player.ResetCombo();
                        string rollText;
                        if (rollPenaltyApplied > 0)
                        {
                            rollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus + rollPenaltyApplied} - {rollPenaltyApplied} = {attackRoll}" : $"{baseRoll} - {rollPenaltyApplied} = {attackRoll}";
                        }
                        else
                        {
                            rollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                        }
                        return $"[{player.Name}] attempts to attack but misses. (Rolled {rollText})";
                    }
                }
                else if (cappedRoll >= rollTuning.RollSystem.BasicAttackThreshold.Min && cappedRoll <= rollTuning.RollSystem.BasicAttackThreshold.Max)
                {
                    // Basic attack - combo resets
                    player.ResetCombo();
                    int basicDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll);
                    if (target is Character basicTargetChar)
                    {
                        basicTargetChar.TakeDamage(basicDamage);
                    }
                    // Check for bleed chance after damage
                    CheckAndApplyBleedChance(player, target);
                    string rollText;
                    if (rollPenaltyApplied > 0)
                    {
                        rollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus + rollPenaltyApplied} - {rollPenaltyApplied} = {attackRoll}" : $"{baseRoll} - {rollPenaltyApplied} = {attackRoll}";
                    }
                    else
                    {
                        rollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                    }
                    int actualBasicDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll, false);
                    string basicDamageDisplay = FormatDamageDisplay(player, target, basicDamage, actualBasicDamage, null, 1.0, 1.0, totalRollBonus, attackRoll);
                    string basicArmorBreakdown = GetArmorBreakdown(player, target, actualBasicDamage);
                    return $"[{player.Name}] uses [Basic Attack] on [{target.Name}]: deals {basicDamageDisplay}.\n        (Rolled {rollText}{basicArmorBreakdown})";
                }
                else if (cappedRoll >= rollTuning.RollSystem.ComboThreshold.Min && cappedRoll <= rollTuning.RollSystem.ComboThreshold.Max)
                {
                    // Check if this is a critical hit (original roll >= 20)
                    if (attackRoll >= rollTuning.RollSystem.CriticalThreshold)
                    {
                        // Critical hit - execute combo action with critical damage
                        if (comboActions.Count == 0)
                        {
                            // No combo actions available, do critical basic attack
                            int criticalDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll);
                            if (target is Character criticalTargetChar)
                            {
                                criticalTargetChar.TakeDamage(criticalDamage);
                            }
                            // Check for bleed chance after damage
                            CheckAndApplyBleedChance(player, target);
                            string criticalRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            int actualCriticalDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll, false);
                            string criticalDamageDisplay = FormatDamageDisplay(player, target, criticalDamage, actualCriticalDamage, null, 1.0, 1.0, totalRollBonus, attackRoll);
                            string criticalBasicArmorBreakdown = GetArmorBreakdown(player, target, actualCriticalDamage);
                            return $"[{player.Name}] uses [Critical Basic Attack] on [{target.Name}]: deals {criticalDamageDisplay}.\n        (Rolled {criticalRollText}, CRITICAL HIT!{criticalBasicArmorBreakdown})";
                        }
                        
                        // Execute critical combo action
                        var criticalAction = comboActions[actionIndex];
                        player.ComboStep++;
                        
                        // Calculate exponential combo amplification: amp^comboStep
                        double criticalBaseComboAmp = player.GetComboAmplifier();
                        double criticalExponentialComboAmp = Math.Pow(criticalBaseComboAmp, actionIndex);
                        
                        int criticalComboDamage = CalculateDamage(player, target, criticalAction, criticalExponentialComboAmp, 1.0, totalRollBonus, attackRoll);
                        if (target is Character criticalComboTargetChar)
                        {
                            criticalComboTargetChar.TakeDamage(criticalComboDamage);
                        }
                        // Check for bleed chance after damage
                        CheckAndApplyBleedChance(player, target);
                        string criticalComboRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                        int actualCriticalComboDamage = CalculateDamage(player, target, criticalAction, criticalExponentialComboAmp, 1.0, totalRollBonus, attackRoll, false);
                        string criticalComboDamageDisplay = FormatDamageDisplay(player, target, criticalComboDamage, actualCriticalComboDamage, criticalAction, criticalExponentialComboAmp, 1.0, totalRollBonus, attackRoll);
                        return $"[{player.Name}] uses [Critical {criticalAction.Name}] on [{target.Name}]: deals {criticalComboDamageDisplay}. (Rolled {criticalComboRollText}, combo step {actionIndex + 1}, {criticalExponentialComboAmp:F2}x amplification, CRITICAL HIT!)";
                    }
                    
                    // Move in combo (non-critical)
                    if (comboActions.Count == 0)
                    {
                        // No combo actions available, do basic attack instead
                        int fallbackDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll);
                        if (target is Character fallbackTargetChar)
                        {
                            fallbackTargetChar.TakeDamage(fallbackDamage);
                        }
                        // Check for bleed chance after damage
                        CheckAndApplyBleedChance(player, target);
                        string fallbackRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                        int actualFallbackDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll, false);
                        string fallbackDamageDisplay = FormatDamageDisplay(player, target, fallbackDamage, actualFallbackDamage, null, 1.0, 1.0, totalRollBonus, attackRoll);
                        return $"[{player.Name}] uses [Basic Attack] on [{target.Name}]: deals {fallbackDamageDisplay}. (Rolled {fallbackRollText}, no combo actions available)";
                    }
                    
                    // Execute combo action
                    var action = comboActions[actionIndex];
                    player.ComboStep++;
                    
                    // Handle DEJA VU - repeat the last action
                    if (action.Name == "DEJA VU" && action.RepeatLastAction)
                    {
                        if (lastPlayerAction != null)
                        {
                            // Calculate exponential combo amplification for DEJA VU: amp^comboStep
                            double dejaVuBaseAmp = player.GetComboAmplifier();
                            double dejaVuExponentialAmp = Math.Pow(dejaVuBaseAmp, actionIndex);
                            
                            // Execute the last action instead
                            int dejaVuDamage = CalculateDamage(player, target, lastPlayerAction, dejaVuExponentialAmp, 1.0, totalRollBonus, attackRoll);
                            if (target is Character dejaVuTargetChar)
                            {
                                dejaVuTargetChar.TakeDamage(dejaVuDamage);
                            }
                            // Check for bleed chance after damage
                            CheckAndApplyBleedChance(player, target);
                            string dejaVuRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            int actualDejaVuDamage = CalculateDamage(player, target, lastPlayerAction, dejaVuExponentialAmp, 1.0, totalRollBonus, attackRoll, false);
                            string dejaVuDamageDisplay = FormatDamageDisplay(player, target, dejaVuDamage, actualDejaVuDamage, lastPlayerAction, dejaVuExponentialAmp, 1.0, totalRollBonus, attackRoll);
                            return $"[{player.Name}] uses [DEJA VU] to repeat [{lastPlayerAction.Name}] on [{target.Name}]: deals {dejaVuDamageDisplay}. (Rolled {dejaVuRollText}, combo step {actionIndex + 1}, {dejaVuExponentialAmp:F2}x amplification)";
                        }
                        else
                        {
                            // Calculate exponential combo amplification for fallback: amp^comboStep
                            double fallbackBaseAmp = player.GetComboAmplifier();
                            double fallbackExponentialAmp = Math.Pow(fallbackBaseAmp, actionIndex);
                            
                            // No previous action to repeat, do basic damage
                            int fallbackDamage = CalculateDamage(player, target, action, fallbackExponentialAmp, 1.0, totalRollBonus, attackRoll);
                            if (target is Character fallbackTargetChar)
                            {
                                fallbackTargetChar.TakeDamage(fallbackDamage);
                            }
                            // Check for bleed chance after damage
                            CheckAndApplyBleedChance(player, target);
                            string dejaVuFallbackRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            int actualDejaVuFallbackDamage = CalculateDamage(player, target, null, fallbackExponentialAmp, 1.0, totalRollBonus, attackRoll, false);
                            string dejaVuFallbackDamageDisplay = FormatDamageDisplay(player, target, fallbackDamage, actualDejaVuFallbackDamage, null, fallbackExponentialAmp, 1.0, totalRollBonus, attackRoll);
                            return $"[{player.Name}] uses [DEJA VU] but has no previous action to repeat on [{target.Name}]: deals {dejaVuFallbackDamageDisplay}. (Rolled {dejaVuFallbackRollText}, combo step {actionIndex + 1}, {fallbackExponentialAmp:F2}x amplification)";
                        }
                    }
                    
                    // Store this action as the last action for future DEJA VU
                    lastPlayerAction = action;
                    
                    // Apply enemy roll penalty if this action has one
                    if (action.EnemyRollPenalty > 0)
                    {
                        player.EnemyRollPenalty = action.EnemyRollPenalty;
                        player.EnemyRollPenaltyTurns = 1; // Apply to next enemy action
                    }
                    
                    // Calculate exponential combo amplification: amp^comboStep
                    double baseComboAmp = player.GetComboAmplifier();
                    double exponentialComboAmp = Math.Pow(baseComboAmp, actionIndex);
                    
                        int comboDamage = CalculateDamage(player, target, action, exponentialComboAmp, 1.0, totalRollBonus, attackRoll);
                        if (target is Character comboTargetChar)
                        {
                            comboTargetChar.TakeDamage(comboDamage);
                        }
                        // Check for bleed chance after damage
                        CheckAndApplyBleedChance(player, target);
                    string comboRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                    int actualComboDamage = CalculateDamage(player, target, action, exponentialComboAmp, 1.0, totalRollBonus, attackRoll, false);
                    string comboDamageDisplay = FormatDamageDisplay(player, target, comboDamage, actualComboDamage, action, exponentialComboAmp, 1.0, totalRollBonus, attackRoll);
                    string comboArmorBreakdown = GetArmorBreakdown(player, target, actualComboDamage);
                    return $"[{player.Name}] uses [{action.Name}] on [{target.Name}]: deals {comboDamageDisplay}.\n        (Rolled {comboRollText}, combo step {actionIndex + 1}, {exponentialComboAmp:F2}x amplification{comboArmorBreakdown})";
                }
                
                // This should never be reached with capped roll logic
                return $"[{player.Name}] attempts to attack but something went wrong. (Rolled {attackRoll})";
            }
            
            // For non-player entities (enemies, etc.), use the same 1d20 system
            // Apply roll bonus from the action
            int enemyRollBonus = selectedAction.RollBonus;
            int originalRollBonus = enemyRollBonus;
            int penaltyApplied = 0;
            
            // Apply enemy roll penalty from player actions (like Arcane Shield)
            if (target is Character playerTarget && playerTarget.EnemyRollPenaltyTurns > 0)
            {
                penaltyApplied = playerTarget.EnemyRollPenalty;
                enemyRollBonus -= penaltyApplied;
                playerTarget.EnemyRollPenaltyTurns--;
                if (playerTarget.EnemyRollPenaltyTurns <= 0)
                {
                    playerTarget.EnemyRollPenalty = 0;
                }
            }
            // Apply enemy roll penalty when the attacking enemy has been debuffed
            else if (source is Enemy enemySource && enemySource.EnemyRollPenaltyTurns > 0)
            {
                penaltyApplied = enemySource.EnemyRollPenalty;
                enemyRollBonus -= penaltyApplied;
                enemySource.EnemyRollPenaltyTurns--;
                if (enemySource.EnemyRollPenaltyTurns <= 0)
                {
                    enemySource.EnemyRollPenalty = 0;
                }
            }
            
            // Apply roll penalty (for effects like Dust Cloud)
            enemyRollBonus -= source.RollPenalty;
            
            // Implement the 1d20 attack system with bonus (same as player)
            int enemyBaseRoll = Dice.Roll(1, 20);
            int enemyAttackRoll = enemyBaseRoll + enemyRollBonus;
            
            // Cap attack roll at 20 for critical hit calculation
            int enemyCappedRoll = Math.Min(enemyAttackRoll, 20);
            
            var enemyTuning = TuningConfig.Instance;
            
            if (enemyCappedRoll >= enemyTuning.RollSystem.MissThreshold.Min && enemyCappedRoll <= enemyTuning.RollSystem.MissThreshold.Max)
            {
                // Miss
                string rollText;
                if (penaltyApplied > 0)
                {
                    rollText = $"{enemyBaseRoll} + {originalRollBonus} - {penaltyApplied} = {enemyAttackRoll}";
                }
                else if (enemyRollBonus != 0)
                {
                    rollText = $"{enemyBaseRoll} + {enemyRollBonus} = {enemyAttackRoll}";
                }
                else
                {
                    rollText = enemyAttackRoll.ToString();
                }
                return $"[{source.Name}] attempts to attack but misses. (Rolled {rollText})";
            }
            else if (enemyCappedRoll >= enemyTuning.RollSystem.BasicAttackThreshold.Min && enemyCappedRoll <= enemyTuning.RollSystem.BasicAttackThreshold.Max)
            {
                // Basic attack - use unified damage system
                finalEffect = CalculateDamage(source, target, selectedAction, 1.0, 1.0, enemyRollBonus, enemyCappedRoll);
                
                if (environment != null)
                {
                    // Apply environment effects to the calculated damage
                    double effectBeforeEnv = finalEffect;
                    double effectAfterEnv = environment.ApplyPassiveEffect(effectBeforeEnv);
                    if (effectAfterEnv != effectBeforeEnv)
                    {
                        finalEffect = (int)Math.Round(effectAfterEnv);
                        envMsg = $" (Environment modified: {effectBeforeEnv:F1} → {effectAfterEnv:F1})";
                    }
                }
                
                string rollText;
                if (penaltyApplied > 0)
                {
                    rollText = $"{enemyBaseRoll} + {originalRollBonus} - {penaltyApplied} = {enemyAttackRoll}";
                }
                else if (enemyRollBonus != 0)
                {
                    rollText = $"{enemyBaseRoll} + {enemyRollBonus} = {enemyAttackRoll}";
                }
                else
                {
                    rollText = enemyAttackRoll.ToString();
                }
                
                // Apply the action effect
                switch (selectedAction.Type)
                {
                    case ActionType.Attack:
                        if (target is Character character)
                        {
                            // Calculate damage with shield reduction
                            var (actualShieldDamage, shieldReduction, shieldUsed) = character.CalculateDamageWithShield(finalEffect);
                            
                            // Apply the damage
                            var damageNotifications = character.TakeDamageWithNotifications(finalEffect);
                            
                            // Show shield reduction message if shield was used
                            if (shieldUsed)
                            {
                                WriteCombatLog($"[{character.Name}]'s Arcane Shield reduces damage by {shieldReduction}!");
                            }
                            
                            // Apply armor spike damage if the target has armor spikes
                            double armorSpikeDamage = character.GetArmorSpikeDamage();
                            if (armorSpikeDamage > 0)
                            {
                                int spikeDamage = (int)(finalEffect * armorSpikeDamage);
                                if (spikeDamage > 0)
                                {
                                    if (source is Character sourceChar)
                                    {
                                        sourceChar.TakeDamage(spikeDamage);
                                    }
                                    else if (source is Enemy sourceEnemy)
                                    {
                                        sourceEnemy.TakeDamage(spikeDamage);
                                    }
                                }
                            }
                            
                            // Apply bleed effect if the action causes bleeding
                            if (selectedAction.CausesBleed)
                            {
                                var poisonConfig = TuningConfig.Instance.Poison;
                                character.ApplyPoison(poisonConfig.DamagePerTick, 1, true); // 1 stack of bleed, mark as bleeding
                                // Bleeding message will be added to return string below
                            }
                        }
                        break;
                    case ActionType.Heal:
                        if (target is Character healTarget)
                        {
                            healTarget.Heal(finalEffect);
                        }
                        break;
                }
                
                // Calculate actual damage dealt (after armor reduction)
                int actualDamage = CalculateDamage(source, target, selectedAction, 1.0, 1.0, enemyRollBonus, enemyCappedRoll);
                string damageDisplay = FormatDamageDisplay(source, target, finalEffect, actualDamage, selectedAction, 1.0, 1.0, enemyRollBonus, enemyCappedRoll);
                string enemyArmorBreakdown = GetArmorBreakdown(source, target, actualDamage);
                
                // Build the result message with bleeding/burning effect if applicable
                string result = $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: deals {damageDisplay}{envMsg}.\n        (Rolled {rollText}{enemyArmorBreakdown})";
                if (selectedAction.CausesBleed && target is Character bleedTarget)
                {
                    string effectType = GetEffectType(selectedAction);
                    result += $"\n[{target.Name}] is {effectType} from {selectedAction.Name}";
                }
                
                // Add health milestone notifications after the attack message
                var healthNotifications = Combat.GetAndClearPendingHealthNotifications();
                foreach (var notification in healthNotifications)
                {
                    result += $"\n{notification}";
                }
                
                return result;
            }
            else if (enemyCappedRoll >= enemyTuning.RollSystem.ComboThreshold.Min && enemyCappedRoll <= enemyTuning.RollSystem.ComboThreshold.Max)
            {
                // Successful attack (14+ threshold) - use unified damage system
                int comboEffect = CalculateDamage(source, target, selectedAction, 1.0, 1.0, enemyRollBonus, enemyCappedRoll);
                
                if (environment != null)
                {
                    // Apply environment effects to the calculated damage
                    double effectBeforeEnv = comboEffect;
                    double effectAfterEnv = environment.ApplyPassiveEffect(effectBeforeEnv);
                    if (effectAfterEnv != effectBeforeEnv)
                    {
                        comboEffect = (int)Math.Round(effectAfterEnv);
                        envMsg = $" (Environment modified: {effectBeforeEnv:F1} → {effectAfterEnv:F1})";
                    }
                }
                string rollText;
                if (penaltyApplied > 0)
                {
                    rollText = $"{enemyBaseRoll} + {originalRollBonus} - {penaltyApplied} = {enemyAttackRoll}";
                }
                else if (enemyRollBonus != 0)
                {
                    rollText = $"{enemyBaseRoll} + {enemyRollBonus} = {enemyAttackRoll}";
                }
                else
                {
                    rollText = enemyAttackRoll.ToString();
                }
                
                // Apply the action effect
                switch (selectedAction.Type)
                {
                    case ActionType.Attack:
                        if (target is Character character)
                        {
                            // Calculate damage with shield reduction
                            var (actualShieldDamage, shieldReduction, shieldUsed) = character.CalculateDamageWithShield(finalEffect);
                            
                            // Apply the damage
                            var damageNotifications = character.TakeDamageWithNotifications(finalEffect);
                            
                            // Show shield reduction message if shield was used
                            if (shieldUsed)
                            {
                                WriteCombatLog($"[{character.Name}]'s Arcane Shield reduces damage by {shieldReduction}!");
                            }
                            
                            // Apply armor spike damage if the target has armor spikes
                            double armorSpikeDamage = character.GetArmorSpikeDamage();
                            if (armorSpikeDamage > 0)
                            {
                                int spikeDamage = (int)(finalEffect * armorSpikeDamage);
                                if (spikeDamage > 0)
                                {
                                    if (source is Character sourceChar)
                                    {
                                        sourceChar.TakeDamage(spikeDamage);
                                    }
                                    else if (source is Enemy sourceEnemy)
                                    {
                                        sourceEnemy.TakeDamage(spikeDamage);
                                    }
                                }
                            }
                            
                            // Apply bleed effect if the action causes bleeding
                            if (selectedAction.CausesBleed)
                            {
                                var poisonConfig = TuningConfig.Instance.Poison;
                                character.ApplyPoison(poisonConfig.DamagePerTick, 1, true); // 1 stack of bleed, mark as bleeding
                                // Bleeding message will be added to return string below
                            }
                        }
                        break;
                    case ActionType.Heal:
                        if (target is Character healTarget)
                        {
                            healTarget.Heal(finalEffect);
                        }
                        break;
                    case ActionType.Buff:
                        // Handle specific buff actions
                        if (selectedAction.Name == "ARCANE SHIELD" && target is Character shieldTarget)
                        {
                            shieldTarget.ApplyShield();
                            WriteCombatLog($"[{source.Name}] applies Arcane Shield to [{target.Name}]");
                        }
                        else if (selectedAction.Name == "War Cry")
                        {
                            // War Cry buffs the caster and debuffs enemies
                            if (source is Enemy enemyCaster)
                            {
                                // Buff the caster (increase their effectiveness)
                                enemyCaster.SetTempComboBonus(2, 3); // +2 combo bonus for 3 turns
                                WriteCombatLog($"[{source.Name}] lets out a mighty War Cry! (+2 combo bonus for 3 turns)");
                                
                                // Debuff the target (weaken them)
                                if (target is Character characterTarget)
                                {
                                    characterTarget.ApplyWeaken(3); // Weaken for 3 turns
                                    WriteCombatLog($"[{target.Name}] is intimidated by the War Cry! (Weakened for 3 turns)");
                                }
                            }
                        }
                        else
                        {
                            // Other buff actions will be handled by the return message below
                        }
                        break;
                    case ActionType.Debuff:
                    // Debuff effects would be implemented here
                    break;
                case ActionType.Interact:
                    // Interaction effects would be implemented here
                    break;
                case ActionType.Move:
                    // Movement effects would be implemented here
                    break;
                case ActionType.UseItem:
                    // Item use effects would be implemented here
                    break;
                }
                
                // Calculate actual damage dealt (after armor reduction)
                int actualComboDamage = CalculateDamage(source, target, selectedAction, 1.0, 1.0, enemyRollBonus, enemyCappedRoll, false);
                string comboDamageDisplay = FormatDamageDisplay(source, target, comboEffect, actualComboDamage, selectedAction, 1.0, 1.0, enemyRollBonus, enemyCappedRoll);
                string enemyComboArmorBreakdown = GetArmorBreakdown(source, target, actualComboDamage);
                
                // Build the result message with bleeding/burning effect if applicable
                string comboResult = $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: deals {comboDamageDisplay}{envMsg}.\n        (Rolled {rollText}{enemyComboArmorBreakdown})";
                if (selectedAction.CausesBleed && target is Character comboBleedTarget)
                {
                    string effectType = GetEffectType(selectedAction);
                    comboResult += $"\n[{target.Name}] is {effectType} from {selectedAction.Name}";
                }
                
                // Add health milestone notifications after the attack message
                var comboHealthNotifications = Combat.GetAndClearPendingHealthNotifications();
                foreach (var notification in comboHealthNotifications)
                {
                    comboResult += $"\n{notification}";
                }
                
                return comboResult;
            }
            
            // This should never be reached with capped roll logic
            return $"[{source.Name}] attempts to attack but something went wrong. (Rolled {enemyAttackRoll})";
        }

        /// <summary>
        /// Determines the appropriate effect type for actions that cause damage over time
        /// </summary>
        private static string GetEffectType(Action action)
        {
            string actionName = action.Name.ToLower();
            
            // Check for poison-based actions
            if (actionName.Contains("poison") || actionName.Contains("venom") || actionName.Contains("bite"))
            {
                return "poisoned";
            }
            
            // Check for fire/heat-based actions
            if (actionName.Contains("acid") || actionName.Contains("lava") || actionName.Contains("fire") || 
                actionName.Contains("flame") || actionName.Contains("burn") || actionName.Contains("heat"))
            {
                return "burning";
            }
            
            // Default to bleeding for other causes
            return "bleeding";
        }

        /// <summary>
        /// Unified damage calculation system for both players and enemies
        /// </summary>
        public static int CalculateDamage(Entity attacker, Entity target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true)
        {
            // Get attacker's weapon damage and stats
            int weaponDamage = 0;
            int strength = 0;
            int highestAttribute = 0;
            
            if (attacker is Character character)
            {
                if (character.Weapon is WeaponItem weapon)
                {
                    weaponDamage = weapon.GetTotalDamage();
                }
                strength = character.GetEffectiveStrength();
                // Find highest attribute (STR, AGI, TEC, INT)
                highestAttribute = Math.Max(Math.Max(character.GetEffectiveStrength(), character.GetEffectiveAgility()), 
                                          Math.Max(character.GetEffectiveTechnique(), character.GetEffectiveIntelligence()));
            }
            else if (attacker is Enemy enemy)
            {
                weaponDamage = 0; // Enemies don't have weapons, just base damage
                strength = enemy.Strength;
                highestAttribute = strength; // For enemies, use strength as highest
                
                // Add level-based damage scaling for enemies
                weaponDamage = enemy.Level * 0; // Enemies get no level damage bonus (DPS scaling handles this)
            }
            
            // Calculate base damage: (STR + highest attribute) / 3 + weapon damage + equipment damage bonus
            // This further reduces the stat contribution to require ~10 actions to kill enemies
            int baseDamage = (strength + highestAttribute) / 3 + weaponDamage;
            
            // Add equipment damage bonuses for characters
            if (attacker is Character attackerChar)
            {
                baseDamage += attackerChar.GetEquipmentDamageBonus();
                baseDamage += attackerChar.GetModificationDamageBonus();
            }
            
            // Apply roll bonus to damage (roll bonus affects base damage)
            baseDamage += rollBonus;
            
            // Apply action multiplier if provided
            if (action != null)
            {
                baseDamage = (int)(baseDamage * action.DamageMultiplier);
            }
            
            // Apply combo amplifier
            baseDamage = (int)(baseDamage * comboAmplifier);
            
            // Apply damage multiplier
            baseDamage = (int)(baseDamage * damageMultiplier);
            
            // Apply modification damage multiplier for characters
            if (attacker is Character damageMultiplierChar)
            {
                baseDamage = (int)(baseDamage * damageMultiplierChar.GetModificationDamageMultiplier());
            }
            // Apply archetype damage multiplier for enemies
            else if (attacker is Enemy enemyAttacker)
            {
                baseDamage = (int)(baseDamage * enemyAttacker.GetArchetypeDamageMultiplier());
            }
            
            // Apply weaken debuff if attacker is weakened (works for both characters and enemies)
            if (attacker.IsWeakened)
            {
                int originalDamage = baseDamage;
                baseDamage = (int)(baseDamage * attacker.WeakenMultiplier);
                int damageReduction = originalDamage - baseDamage;
                if (damageReduction > 0 && showWeakenedMessage)
                {
                    WriteCombatLog($"[{attacker.Name}]'s weakened condition reduces damage by {damageReduction}!");
                }
            }
            
            // Get target's armor - only from equipped gear
            int armor = 0;
            if (target is Character targetChar)
            {
                // Only armor from equipped gear counts
                if (targetChar.Head is HeadItem head) armor += head.GetTotalArmor();
                if (targetChar.Body is ChestItem chest) armor += chest.GetTotalArmor();
                if (targetChar.Feet is FeetItem feet) armor += feet.GetTotalArmor();
            }
            else if (target is Enemy targetEnemy)
            {
                // Enemies have armor from their base stats
                armor = targetEnemy.Armor;
            }
            
            // Calculate final damage with configurable minimum
            var tuning = TuningConfig.Instance;
            int finalDamage = Math.Max(tuning.Combat.MinimumDamage, baseDamage - armor);
            
            // Critical hit based on tuning config
            if (roll >= tuning.Combat.CriticalHitThreshold)
            {
                finalDamage = (int)(finalDamage * tuning.Combat.CriticalHitMultiplier);
            }
            
            // Apply lifesteal for characters
            if (attacker is Character lifestealChar && finalDamage > 0)
            {
                double lifestealPercent = lifestealChar.GetModificationLifesteal();
                if (lifestealPercent > 0)
                {
                    int lifestealAmount = (int)(finalDamage * lifestealPercent);
                    if (lifestealAmount > 0)
                    {
                        lifestealChar.Heal(lifestealAmount);
                    }
                }
            }
            
            return finalDamage;
        }

        /// <summary>
        /// Applies intelligent delay system for text display
        /// </summary>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            var tuning = TuningConfig.Instance;
            if (tuning.UI.EnableTextDelays && isTextDisplayed)
            {
                // Calculate delay based on action length and combat speed using tuning config
                double baseDelayMs = actionLength * tuning.UI.BaseDelayPerAction;
                double adjustedDelayMs = baseDelayMs / tuning.UI.CombatSpeedMultiplier;
                int finalDelayMs = Math.Max(tuning.UI.MinimumDelay, (int)adjustedDelayMs);
                
                Thread.Sleep(finalDelayMs);
            }
        }

        /// <summary>
        /// Formats damage display - returns just the final damage for main line
        /// </summary>
        public static string FormatDamageDisplay(Entity attacker, Entity target, int rawDamage, int actualDamage, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
        {
            return $"{actualDamage} damage";
        }

        /// <summary>
        /// Gets armor breakdown for inline display
        /// </summary>
        public static string GetArmorBreakdown(Entity attacker, Entity target, int actualDamage)
        {
            // Get target's armor
            int armor = 0;
            if (target is Character targetChar)
            {
                if (targetChar.Head is HeadItem head) armor += head.GetTotalArmor();
                if (targetChar.Body is ChestItem chest) armor += chest.GetTotalArmor();
                if (targetChar.Feet is FeetItem feet) armor += feet.GetTotalArmor();
            }
            else if (target is Enemy targetEnemy)
            {
                armor = targetEnemy.Armor;
            }
            
            // If no armor, don't show breakdown
            if (armor == 0)
            {
                return "";
            }
            
            // Calculate the raw attack damage (before armor reduction)
            int rawAttackDamage = actualDamage + armor;
            
            // Return breakdown format: | X attack - Y armor
            return $" | {rawAttackDamage} attack - {armor} armor";
        }

        /// <summary>
        /// Writes a combat log message with configurable delay
        /// </summary>
        public static void WriteCombatLog(string message)
        {
            Console.WriteLine(message);
            
            var tuning = TuningConfig.Instance;
            if (tuning.UI.EnableTextDelays)
            {
                Thread.Sleep(tuning.UI.CombatLogDelay);
            }
        }


        /// <summary>
        /// Checks and applies bleed chance from modifications after damage is dealt
        /// </summary>
        public static void CheckAndApplyBleedChance(Character attacker, Entity target)
        {
            double bleedChance = attacker.GetModificationBleedChance();
            if (bleedChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0; // Roll 0.0 to 0.99
                if (roll < bleedChance)
                {
                    // Apply bleed effect
                    if (target is Character characterTarget)
                    {
                        var poisonConfig = TuningConfig.Instance.Poison;
                        characterTarget.ApplyPoison(poisonConfig.DamagePerTick, 2, true); // 2 stacks of bleed
                        WriteCombatLog($"[{target.Name}] is bleeding from {attacker.Name}'s attack (2 stacks, {poisonConfig.DamagePerTick} damage per stack)");
                    }
                    else if (target is Enemy enemyTarget)
                    {
                        // Only apply bleed to living enemies
                        if (enemyTarget.IsLiving)
                        {
                            var poisonConfig = TuningConfig.Instance.Poison;
                            enemyTarget.ApplyPoison(poisonConfig.DamagePerTick, 2, true); // 2 stacks of bleed
                            WriteCombatLog($"[{target.Name}] is bleeding from {attacker.Name}'s attack (2 stacks, {poisonConfig.DamagePerTick} damage per stack)");
                        }
                        else
                        {
                            WriteCombatLog($"[{target.Name}] cannot be bled (undead)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if we're in narrative mode
        /// </summary>
        public static bool IsInNarrativeMode()
        {
            return currentBattleNarrative != null;
        }

        /// <summary>
        /// Add a battle event to the current narrative
        /// </summary>
        public static void AddBattleEvent(BattleEvent battleEvent)
        {
            if (currentBattleNarrative != null)
            {
                // BattleNarrative doesn't have AddEvent method, so we'll just ignore for now
                // This maintains compatibility with existing code
            }
        }

        /// <summary>
        /// Executes an area of effect action from the source entity to all targets in the room
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="targets">List of all targets in the room</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">Optional pre-selected action to use instead of selecting a new one</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteAreaOfEffectAction(Entity source, List<Entity> targets, Environment? environment = null, Action? selectedAction = null)
        {
            if (selectedAction == null)
            {
                selectedAction = source.SelectAction();
            }
            
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }

            // Initialize results list for area of effect actions
            var results = new List<string>();

            int baseEffect = selectedAction.BaseValue > 0 ? selectedAction.BaseValue : 1;
            double effect = baseEffect * selectedAction.DamageMultiplier;
            double effectBeforeEnv = effect;
            string envMsg = "";
            if (environment != null)
            {
                effect = environment.ApplyPassiveEffect(effect);
                if (effect != effectBeforeEnv)
                {
                    envMsg = $" (Environment modified: {effectBeforeEnv:F1} → {effect:F1})";
                }
            }
            
            int finalEffect = (int)Math.Round(effect);

            // Apply the action effect to all targets
            // Update temporary effects for both source and targets based on action length BEFORE applying new debuffs
            if (source is Character sourceCharacter)
            {
                sourceCharacter.UpdateTempEffects(selectedAction.Length);
            }
            foreach (var target in targets)
            {
                if (target is Character targetCharacter)
                {
                    targetCharacter.UpdateTempEffects(selectedAction.Length);
                }
            }

            foreach (var target in targets)
            {
                // Check if target is alive (only Character and Enemy have IsAlive property)
                bool isAlive = true;
                if (target is Character character)
                    isAlive = character.IsAlive;
                else if (target is Enemy enemy)
                    isAlive = enemy.IsAlive;
                
                if (!isAlive) continue; // Skip dead targets

                // Check for poison effects first, regardless of action type
                if (selectedAction.CausesPoison)
                {
                    // Apply poison debuff using configuration
                    var poisonConfig = TuningConfig.Instance.Poison;
                    if (target is Character characterTarget)
                    {
                        characterTarget.ApplyPoison(poisonConfig.DamagePerTick, poisonConfig.StacksPerApplication);
                        results.Add($"[{target.Name}] is poisoned by {selectedAction.Name} (+{poisonConfig.StacksPerApplication} stacks, {poisonConfig.DamagePerTick} damage per stack)");
                    }
                    else if (target is Enemy enemyTarget)
                    {
                        // Only apply poison to living enemies
                        if (enemyTarget.IsLiving)
                        {
                            enemyTarget.ApplyPoison(poisonConfig.DamagePerTick, poisonConfig.StacksPerApplication);
                            results.Add($"[{target.Name}] is poisoned by {selectedAction.Name} (+{poisonConfig.StacksPerApplication} stacks, {poisonConfig.DamagePerTick} damage per stack)");
                        }
                        else
                        {
                            results.Add($"[{target.Name}] cannot be poisoned (undead)");
                        }
                    }
                }
                else if (selectedAction.CausesWeaken)
                {
                    // Apply weaken debuff based on action length
                    if (target is Character weakenTarget)
                    {
                        int weakenTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                        weakenTarget.ApplyWeaken(weakenTurns);
                        results.Add($"[{target.Name}] is weakened by {selectedAction.Name} ({weakenTurns} turns)");
                    }
                    else if (target is Enemy enemyWeakenTarget)
                    {
                        int weakenTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                        enemyWeakenTarget.ApplyWeaken(weakenTurns);
                        results.Add($"[{target.Name}] is weakened by {selectedAction.Name} ({weakenTurns} turns)");
                    }
                }
                else if (selectedAction.CausesSlow)
                {
                    // Apply slow debuff based on action length
                    if (target is Character slowTarget)
                    {
                        int slowTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                        slowTarget.ApplySlow(1.5, slowTurns); // 1.5x slower
                        results.Add($"[{target.Name}] is slowed by {selectedAction.Name} ({slowTurns} turns)");
                    }
                    else if (target is Enemy enemySlowTarget)
                    {
                        int slowTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                        enemySlowTarget.ApplySlow(1.5, slowTurns); // 1.5x slower
                        results.Add($"[{target.Name}] is slowed by {selectedAction.Name} ({slowTurns} turns)");
                    }
                }
                else if (selectedAction.CausesBleed)
                {
                    // Apply bleed debuff (using poison system for now)
                    if (target is Character bleedTarget)
                    {
                        var poisonConfig = TuningConfig.Instance.Poison;
                        bleedTarget.ApplyPoison(poisonConfig.DamagePerTick, 2, true); // 2 stacks of bleed, mark as bleeding
                        string effectType = GetEffectType(selectedAction);
                        results.Add($"[{target.Name}] is {effectType} from {selectedAction.Name}");
                    }
                    else if (target is Enemy enemyBleedTarget)
                    {
                        // Only apply bleed to living enemies
                        if (enemyBleedTarget.IsLiving)
                        {
                            var poisonConfig = TuningConfig.Instance.Poison;
                            enemyBleedTarget.ApplyPoison(poisonConfig.DamagePerTick, 2, true); // 2 stacks of bleed, mark as bleeding
                            string effectType = GetEffectType(selectedAction);
                        results.Add($"[{target.Name}] is {effectType} from {selectedAction.Name}");
                        }
                        else
                        {
                            results.Add($"[{target.Name}] cannot be bled (undead)");
                        }
                    }
                }
                else if (selectedAction.Name == "Dust Cloud")
                {
                    // Apply roll penalty for Dust Cloud
                    int rollPenaltyTurns = 3; // 3 turns
                    int rollPenalty = 3; // -3 to rolls
                    
                    if (target is Character dustTarget)
                    {
                        dustTarget.ApplyRollPenalty(rollPenalty, rollPenaltyTurns);
                        results.Add($"[{target.Name}] is affected by {selectedAction.Name} (-{rollPenalty} to rolls for {rollPenaltyTurns} turns)");
                    }
                    else if (target is Enemy enemyDustTarget)
                    {
                        enemyDustTarget.ApplyRollPenalty(rollPenalty, rollPenaltyTurns);
                        results.Add($"[{target.Name}] is affected by {selectedAction.Name} (-{rollPenalty} to rolls for {rollPenaltyTurns} turns)");
                    }
                }
                else if (selectedAction.CausesStun || selectedAction.Name.Contains("Stun") || selectedAction.Name.Contains("stunning") || 
                         selectedAction.Name.Contains("Falling Branch") || selectedAction.Name.Contains("Dungeon Collapse"))
                {
                    // Only apply stun if the target is not already stunned
                    if (!target.IsStunned)
                    {
                        // Apply stun debuff based on action length
                        int stunTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                        if (target is Character stunTarget)
                        {
                            stunTarget.IsStunned = true;
                            stunTarget.StunTurnsRemaining = stunTurns;
                            results.Add($"[{target.Name}] is stunned by {selectedAction.Name} ({stunTurns} turns)");
                        }
                        else if (target is Enemy enemyStunTarget)
                        {
                            enemyStunTarget.IsStunned = true;
                            enemyStunTarget.StunTurnsRemaining = stunTurns;
                            results.Add($"[{target.Name}] is stunned by {selectedAction.Name} ({stunTurns} turns)");
                        }
                    }
                    else
                    {
                        // Target is already stunned, don't apply new stun
                        results.Add($"[{target.Name}] is already stunned and unaffected by {selectedAction.Name}");
                    }
                }
                else switch (selectedAction.Type)
                {
                    case ActionType.Attack:
                        if (target is Character attackTarget)
                        {
                            // Calculate damage with shield reduction
                            var (actualDamage, shieldReduction, shieldUsed) = attackTarget.CalculateDamageWithShield(finalEffect);
                            
                            // Apply the damage
                            attackTarget.TakeDamage(finalEffect);
                            
                            // Show shield reduction message if shield was used
                            if (shieldUsed)
                            {
                                results.Add($"[{attackTarget.Name}]'s Arcane Shield reduces damage by {shieldReduction}!");
                            }
                            
                            results.Add($"[{target.Name}] takes {finalEffect} damage");
                            
                            // Apply armor spike damage if the target has armor spikes
                            double armorSpikeDamage = attackTarget.GetArmorSpikeDamage();
                            if (armorSpikeDamage > 0)
                            {
                                int spikeDamage = (int)(finalEffect * armorSpikeDamage);
                                if (spikeDamage > 0)
                                {
                                    if (source is Character sourceChar)
                                    {
                                        sourceChar.TakeDamage(spikeDamage);
                                    }
                                    else if (source is Enemy sourceEnemy)
                                    {
                                        sourceEnemy.TakeDamage(spikeDamage);
                                    }
                                    results.Add($"[{source.Name}] takes {spikeDamage} damage from armor spikes");
                                }
                            }
                            
                            // Apply bleed effect if the action causes bleeding
                            if (selectedAction.CausesBleed)
                            {
                                var poisonConfig = TuningConfig.Instance.Poison;
                                if (attackTarget is Enemy enemyTarget && !enemyTarget.IsLiving)
                                {
                                    results.Add($"[{attackTarget.Name}] is immune to bleeding (undead)");
                                }
                                else
                                {
                                    attackTarget.ApplyPoison(poisonConfig.DamagePerTick, 1, true); // 1 stack of bleed, mark as bleeding
                                    string effectType = GetEffectType(selectedAction);
                                    results.Add($"[{attackTarget.Name}] is {effectType} from {selectedAction.Name}");
                                }
                            }
                        }
                        else if (target is Enemy enemyAttackTarget)
                        {
                            // Apply damage to enemy
                            enemyAttackTarget.TakeDamage(finalEffect);
                            results.Add($"[{target.Name}] takes {finalEffect} damage");
                            
                            // Apply enemy roll penalty if this action has one (like Arcane Shield)
                            if (selectedAction.EnemyRollPenalty > 0)
                            {
                                enemyAttackTarget.EnemyRollPenalty = selectedAction.EnemyRollPenalty;
                                enemyAttackTarget.EnemyRollPenaltyTurns = 1; // Apply to next action
                                results.Add($"[{target.Name}] is weakened by {selectedAction.Name} (-{selectedAction.EnemyRollPenalty} to next roll)");
                            }
                            
                            // Apply other debuff effects if the action causes them
                            if (selectedAction.CausesWeaken)
                            {
                                int weakenTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                                enemyAttackTarget.ApplyWeaken(weakenTurns);
                                results.Add($"[{target.Name}] is weakened by {selectedAction.Name} ({weakenTurns} turns)");
                            }
                            else if (selectedAction.CausesSlow)
                            {
                                int slowTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                                enemyAttackTarget.ApplySlow(1.5, slowTurns);
                                results.Add($"[{target.Name}] is slowed by {selectedAction.Name} ({slowTurns} turns)");
                            }
                            else if (selectedAction.CausesPoison)
                            {
                                var poisonConfig = TuningConfig.Instance.Poison;
                                if (enemyAttackTarget.IsLiving)
                                {
                                    enemyAttackTarget.ApplyPoison(poisonConfig.DamagePerTick, poisonConfig.StacksPerApplication);
                                    results.Add($"[{target.Name}] is poisoned by {selectedAction.Name} (+{poisonConfig.StacksPerApplication} stacks, {poisonConfig.DamagePerTick} damage per stack)");
                                }
                                else
                                {
                                    results.Add($"[{target.Name}] cannot be poisoned (undead)");
                                }
                            }
                            else if (selectedAction.CausesBleed)
                            {
                                var poisonConfig = TuningConfig.Instance.Poison;
                                if (enemyAttackTarget.IsLiving)
                                {
                                    enemyAttackTarget.ApplyPoison(poisonConfig.DamagePerTick, 1, true); // 1 stack of bleed, mark as bleeding
                                    string effectType = GetEffectType(selectedAction);
                                    results.Add($"[{target.Name}] is {effectType} from {selectedAction.Name}");
                                }
                                else
                                {
                                    results.Add($"[{target.Name}] is immune to bleeding (undead)");
                                }
                            }
                        }
                        break;
                    case ActionType.Heal:
                        if (target is Character healTarget)
                        {
                            healTarget.Heal(finalEffect);
                            results.Add($"[{target.Name}] heals {finalEffect} health");
                        }
                        break;
                    case ActionType.Debuff:
                        // Handle different debuff types
                        if (selectedAction.CausesSlow && target is Character slowTarget)
                        {
                            // Apply slow debuff based on action length
                            int slowTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                            slowTarget.ApplySlow(1.5, slowTurns);
                            results.Add($"[{target.Name}] is slowed by {selectedAction.Name} ({slowTurns} turns)");
                        }
                        else if (selectedAction.CausesSlow && target is Enemy enemySlowTarget)
                        {
                            // Apply slow debuff based on action length
                            int slowTurns = (int)Math.Ceiling(Character.CalculateTurnsFromActionLength(selectedAction.Length));
                            enemySlowTarget.ApplySlow(1.5, slowTurns);
                            results.Add($"[{target.Name}] is slowed by {selectedAction.Name} ({slowTurns} turns)");
                        }
                        else
                        {
                            // Use the action's description to provide more context
                            string effectDescription = selectedAction.Description.ToLower();
                            if (effectDescription.Contains("weaken"))
                                results.Add($"[{target.Name}] is weakened by {selectedAction.Name}");
                            else if (effectDescription.Contains("slow"))
                                results.Add($"[{target.Name}] is slowed by {selectedAction.Name}");
                            else if (effectDescription.Contains("stun"))
                                results.Add($"[{target.Name}] is stunned by {selectedAction.Name}");
                            else if (effectDescription.Contains("bleed") || effectDescription.Contains("burn"))
                            {
                                string effectType = (selectedAction.Name.Contains("Acid") || selectedAction.Name.Contains("Lava")) ? "burning" : "bleeding";
                                results.Add($"[{target.Name}] is {effectType} from {selectedAction.Name}");
                            }
                            else if (effectDescription.Contains("poison"))
                                results.Add($"[{target.Name}] is poisoned by {selectedAction.Name}");
                            else
                                results.Add($"[{target.Name}] is affected by {selectedAction.Name}");
                        }
                        break;
                    case ActionType.Buff:
                        // Handle specific buff actions
                        if (selectedAction.Name == "ARCANE SHIELD" && target is Character shieldTarget)
                        {
                            shieldTarget.ApplyShield();
                            results.Add($"[{target.Name}] gains Arcane Shield protection");
                        }
                        else if (selectedAction.Name == "War Cry")
                        {
                            // War Cry buffs the caster and debuffs enemies
                            if (source is Enemy enemyCaster)
                            {
                                // Buff the caster (increase their effectiveness)
                                enemyCaster.SetTempComboBonus(2, 3); // +2 combo bonus for 3 turns
                                results.Add($"[{source.Name}] lets out a mighty War Cry! (+2 combo bonus for 3 turns)");
                                
                                // Debuff the target (weaken them)
                                if (target is Character characterTarget)
                                {
                                    characterTarget.ApplyWeaken(3); // Weaken for 3 turns
                                    results.Add($"[{target.Name}] is intimidated by the War Cry! (Weakened for 3 turns)");
                                }
                            }
                        }
                        else
                        {
                            results.Add($"[{target.Name}] is buffed by {selectedAction.Name}");
                        }
                        break;
                }
            }

            if (selectedAction.Cooldown > 0)
                selectedAction.ResetCooldown();

            string result = $"[{source.Name}] uses [{selectedAction.Name}]: ";
            if (results.Count > 0)
            {
                // Format results with each target on a new indented line
                string formattedResults = string.Join("\n\t", results);
                result += $"\n\t{formattedResults}{envMsg}.";
            }
            else
            {
                result += "no effect.";
            }
            
            return result;
        }

        /// <summary>
        /// Executes an action with proper speed tracking
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The entity receiving the action</param>
        /// <param name="action">The specific action being executed</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="rollResult">The roll result to determine if this is a basic attack or combo</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteActionWithSpeed(Entity source, Entity target, Action action, Environment? environment = null)
        {
            // Execute the action normally
            string result = ExecuteAction(source, target, environment);
            
            // Track the action speed and get duration
            double actionDuration = 0.0;
            if (currentActionSpeedSystem != null)
            {
                // Parse roll result from the result string to determine if this is a basic attack
                int rollResult = ParseRollResultFromString(result);
                var rollTuning = TuningConfig.Instance;
                bool isBasicAttack = rollResult >= rollTuning.RollSystem.BasicAttackThreshold.Min && 
                                   rollResult <= rollTuning.RollSystem.BasicAttackThreshold.Max;
                
                actionDuration = currentActionSpeedSystem.ExecuteAction(source, action, isBasicAttack);
            }
            
            // Format the result to move roll details to a separate indented line
            result = FormatCombatMessage(result, actionDuration);
            
            return result;
        }
        
        /// <summary>
        /// Formats combat messages to separate action from roll details
        /// </summary>
        /// <param name="originalMessage">The original combat message</param>
        /// <param name="actionDuration">The duration of the action</param>
        /// <returns>Formatted message with roll details on separate line</returns>
        private static string FormatCombatMessage(string originalMessage, double actionDuration)
        {
            // Split the message into lines to handle status effects and roll details
            string[] lines = originalMessage.Split('\n');
            string mainMessage = lines[0];
            string? statusEffectMessage = null;
            string? existingRollDetails = null;
            
            // Check if there are additional lines with roll details or status effects
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("(") && line.EndsWith(")"))
                {
                    // This is roll details - extract content without parentheses
                    existingRollDetails = line.Substring(1, line.Length - 2);
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    // This is a status effect message
                    statusEffectMessage = line;
                }
            }
            
            // Look for patterns like "(Rolled X, combo step Y, amplification, CRITICAL HIT)" in main message
            var match = System.Text.RegularExpressions.Regex.Match(mainMessage, @"^(.*?)\s*\((.*)\)$");
            
            string rollDetails = "";
            if (match.Success)
            {
                // Extract roll details from main message
                rollDetails = match.Groups[2].Value.Trim();
                mainMessage = match.Groups[1].Value.Trim();
            }
            
            // Use existing roll details if found, otherwise use main message roll details
            string finalRollDetails = !string.IsNullOrEmpty(existingRollDetails) ? existingRollDetails : rollDetails;
            
            // Add duration to roll details if present
            if (actionDuration > 0.0)
            {
                if (!string.IsNullOrEmpty(finalRollDetails))
                {
                    finalRollDetails += $" | Duration: {actionDuration:F2}s";
                }
                else
                {
                    finalRollDetails = $"Duration: {actionDuration:F2}s";
                }
            }
            
            // Build the result with roll details on indented line
            string result = mainMessage;
            if (!string.IsNullOrEmpty(finalRollDetails))
            {
                result += $"\n        ({finalRollDetails})";
            }
            
            // Add status effect message indented if present
            if (!string.IsNullOrEmpty(statusEffectMessage))
            {
                result += $"\n        {statusEffectMessage}";
            }
            
            return result;
        }
        
        /// <summary>
        /// Checks for health milestones and leadership changes after damage is dealt
        /// </summary>
        /// <param name="entity">The entity that took damage</param>
        /// <param name="damageDealt">The amount of damage dealt</param>
        /// <returns>List of health milestone notifications</returns>
        public static List<string> CheckHealthMilestones(Entity entity, int damageDealt)
        {
            if (currentHealthTracker == null)
                return new List<string>();
                
            return currentHealthTracker.CheckHealthMilestones(entity, damageDealt);
        }
        
        /// <summary>
        /// Gets and clears pending health milestone notifications
        /// </summary>
        /// <returns>List of pending notifications</returns>
        public static List<string> GetAndClearPendingHealthNotifications()
        {
            if (currentHealthTracker == null)
                return new List<string>();
                
            return currentHealthTracker.GetAndClearPendingNotifications();
        }
        
        /// <summary>
        /// Parses the roll result from a combat result string
        /// </summary>
        /// <param name="resultString">The combat result string</param>
        /// <returns>The roll result, or 0 if not found</returns>
        private static int ParseRollResultFromString(string resultString)
        {
            // Look for patterns like "Rolled 16 + 4 = 20" or "Rolled 5"
            var match = System.Text.RegularExpressions.Regex.Match(resultString, @"Rolled\s+(\d+)(?:\s*\+\s*\d+\s*=\s*(\d+))?");
            if (match.Success)
            {
                // If there's a total (like "16 + 4 = 20"), use the total
                if (match.Groups[2].Success)
                {
                    return int.Parse(match.Groups[2].Value);
                }
                // Otherwise use the base roll
                else
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            return 0; // Default to 0 if no roll found
        }

        /// <summary>
        /// Gets the next entity that should act based on action speed
        /// </summary>
        /// <returns>The next entity to act, or null if none are ready</returns>
        public static Entity? GetNextEntityToAct()
        {
            return currentActionSpeedSystem?.GetNextEntityToAct();
        }

        /// <summary>
        /// Checks if an entity is ready to act
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the entity is ready to act</returns>
        public static bool IsEntityReady(Entity entity)
        {
            return currentActionSpeedSystem?.IsEntityReady(entity) ?? true;
        }
    }
}