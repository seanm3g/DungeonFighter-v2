using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Manages character status effects, debuffs, and temporary conditions
    /// </summary>
    public class CharacterEffects
    {
        // Combo system state
        // Start at step 0 (first action in combo sequence)
        // Step 0: First action in sequence (no bonus)
        // Step 1+: Subsequent actions (bonus may apply based on combo mechanics)
        // ComboStep is used as 0-based index into combo sequence: ComboStep % comboActions.Count
        public int ComboStep { get; set; } = 0;
        public double ComboAmplifier { get; set; } = 1.0;
        public int ComboBonus { get; set; } = 0;
        public int TempComboBonus { get; set; } = 0;
        public int TempComboBonusTurns { get; set; } = 0;
        
        // Temporary roll bonus (for actions that grant roll bonuses for multiple turns)
        public int TempRollBonus { get; set; } = 0;
        public int TempRollBonusTurns { get; set; } = 0;
        
        // Enemy roll penalty from actions like Arcane Shield
        public int EnemyRollPenalty { get; set; } = 0;
        public int EnemyRollPenaltyTurns { get; set; } = 0;
        
        // Slow debuff from environmental effects
        public double SlowMultiplier { get; set; } = 1.0;
        public int SlowTurns { get; set; } = 0;
        
        // Poison and burn effects are now handled by Actor base class
        
        // Shield buff from Arcane SHIELD
        public bool HasShield { get; set; } = false;
        public int LastShieldReduction { get; set; } = 0;
        
        // Combo mode tracking
        public bool ComboModeActive { get; set; } = false;
        public int LastComboActionIdx { get; set; } = -1;
        
        // Advanced action mechanics
        public Action? LastAction { get; set; } = null; // For Deja Vu
        public bool SkipNextTurn { get; set; } = false; // For True Strike
        public bool GuaranteeNextSuccess { get; set; } = false; // For True Strike
        public int ExtraAttacks { get; set; } = 0; // For Flurry/Precision Strike
        public int ExtraDamage { get; set; } = 0; // For Opening Volley
        public double NextAttackDamageMultiplier { get; set; } = 1.0; // For Follow Through - damage multiplier for next attack
        public int NextAttackStatBonus { get; set; } = 0; // For Follow Through - stat bonus to apply on next attack
        public string NextAttackStatBonusType { get; set; } = ""; // Stat type for next attack bonus
        public int NextAttackStatBonusDuration { get; set; } = 0; // Duration for next attack stat bonus
        // DamageReduction is now handled by Actor base class
        public double LengthReduction { get; set; } = 0.0; // For Taunt
        public int LengthReductionTurns { get; set; } = 0;
        public double ComboAmplifierMultiplier { get; set; } = 1.0; // For Pretty Boy Swag
        public int ComboAmplifierTurns { get; set; } = 0;
        
        // Divine reroll system
        public int RerollCharges { get; set; } = 0;
        public bool UsedRerollThisTurn { get; set; } = false;
        public int RerollChargesUsed { get; set; } = 0;
        
        // ACTION/ATTACK keyword bonuses tracking
        // Queue of bonuses waiting for next action in sequence (ACTION keyword)
        public List<ActionAttackBonusGroup> ActionBonuses { get; set; } = new List<ActionAttackBonusGroup>();
        
        // Queue of bonuses for next roll attempts (ATTACK keyword)
        public List<ActionAttackBonusGroup> AttackBonuses { get; set; } = new List<ActionAttackBonusGroup>();

        // Stun, weaken, and roll penalty effects are now handled by Actor base class

        public void UpdateTempEffects(double actionLength = 1.0)
        {
            // Calculate how many turns this action represents
            double turnsPassed = actionLength / Character.DEFAULT_ACTION_LENGTH;

            // Update length reduction
            if (LengthReductionTurns > 0)
            {
                LengthReductionTurns = Math.Max(0, LengthReductionTurns - (int)Math.Ceiling(turnsPassed));
                if (LengthReductionTurns == 0)
                    LengthReduction = 0.0;
            }

            // Update enemy roll penalty
            if (EnemyRollPenaltyTurns > 0)
            {
                EnemyRollPenaltyTurns = Math.Max(0, EnemyRollPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (EnemyRollPenaltyTurns == 0)
                    EnemyRollPenalty = 0;
            }

            // Update combo amplifier multiplier
            if (ComboAmplifierTurns > 0)
            {
                ComboAmplifierTurns = Math.Max(0, ComboAmplifierTurns - (int)Math.Ceiling(turnsPassed));
                if (ComboAmplifierTurns == 0)
                    ComboAmplifierMultiplier = 1.0;
            }

            // Update extra damage decay (per turn, not per action)
            if (ExtraDamage > 0)
            {
                ExtraDamage = Math.Max(0, ExtraDamage - (int)Math.Ceiling(turnsPassed));
            }

            // Update temporary roll bonus (decrements per roll, not per turn)
            // This is handled in ConsumeTempRollBonus() instead

            // Update slow debuff
            if (SlowTurns > 0)
            {
                SlowTurns = Math.Max(0, SlowTurns - (int)Math.Ceiling(turnsPassed));
                if (SlowTurns == 0)
                {
                    SlowMultiplier = 1.0; // Reset to normal speed
                }
            }

            // Reset reroll usage for next turn
            ResetRerollUsage();
        }

        public void SetTempComboBonus(int bonus, int turns)
        {
            TempComboBonus = bonus;
            TempComboBonusTurns = turns;
        }

        public int ConsumeTempComboBonus()
        {
            int bonus = TempComboBonus;
            if (TempComboBonusTurns > 0)
            {
                TempComboBonusTurns--;
                if (TempComboBonusTurns == 0)
                    TempComboBonus = 0;
            }
            return bonus;
        }

        public void SetTempRollBonus(int bonus, int turns)
        {
            TempRollBonus = bonus;
            TempRollBonusTurns = turns;
        }

        public int GetTempRollBonus()
        {
            // Get the roll bonus without consuming it (for action selection)
            return TempRollBonus;
        }

        public int ConsumeTempRollBonus()
        {
            int bonus = TempRollBonus;
            if (TempRollBonusTurns > 0)
            {
                TempRollBonusTurns--;
                if (TempRollBonusTurns == 0)
                    TempRollBonus = 0;
            }
            return bonus;
        }

        /// <summary>
        /// Consumes the next attack damage multiplier (for Follow Through and similar effects)
        /// Returns the multiplier and resets it to 1.0
        /// </summary>
        public double ConsumeNextAttackDamageMultiplier()
        {
            double multiplier = NextAttackDamageMultiplier;
            NextAttackDamageMultiplier = 1.0; // Reset after use
            return multiplier;
        }

        /// <summary>
        /// Consumes the next attack stat bonus (for Follow Through and similar effects)
        /// Returns the bonus values and resets them
        /// </summary>
        public (int bonus, string statType, int duration) ConsumeNextAttackStatBonus()
        {
            var result = (NextAttackStatBonus, NextAttackStatBonusType, NextAttackStatBonusDuration);
            NextAttackStatBonus = 0;
            NextAttackStatBonusType = "";
            NextAttackStatBonusDuration = 0;
            return result;
        }

        public void ActivateComboMode()
        {
            ComboModeActive = true;
        }

        public void DeactivateComboMode()
        {
            ComboModeActive = false;
        }

        public void ResetCombo()
        {
            ComboStep = 0; // Reset to step 0 (first action in combo sequence, no bonus)
            ComboAmplifier = 1.0;
            LastComboActionIdx = -1;
            ComboModeActive = false;
        }

        public void ApplySlow(double slowMultiplier, int duration)
        {
            SlowMultiplier = slowMultiplier;
            SlowTurns = duration;
        }

        // ApplyPoison and ApplyBurn are now handled by Actor base class

        public void ApplyShield()
        {
            HasShield = true;
        }

        public bool ConsumeShield()
        {
            if (HasShield)
            {
                HasShield = false;
                return true;
            }
            return false;
        }


        public void ClearAllTempEffects()
        {
            // Clear character-specific effects
            SlowTurns = 0;
            SlowMultiplier = 1.0;
            
            // Clear combo effects
            ComboStep = 0; // Reset to step 0 (first action in combo sequence, no bonus)
            ComboAmplifier = 1.0;
            LastComboActionIdx = -1;
            ComboModeActive = false;
            
            // Clear temporary bonuses
            TempComboBonus = 0;
            TempComboBonusTurns = 0;
            TempRollBonus = 0;
            TempRollBonusTurns = 0;
            ExtraDamage = 0;
            ExtraAttacks = 0;
            NextAttackDamageMultiplier = 1.0;
            NextAttackStatBonus = 0;
            NextAttackStatBonusType = "";
            NextAttackStatBonusDuration = 0;
            
            // Clear advanced action mechanics
            LastAction = null;
            SkipNextTurn = false;
            GuaranteeNextSuccess = false;
            LengthReduction = 0.0;
            LengthReductionTurns = 0;
            ComboAmplifierMultiplier = 1.0;
            ComboAmplifierTurns = 0;
            
            // Clear shield
            HasShield = false;
            LastShieldReduction = 0;
            
            // Clear enemy roll penalty
            EnemyRollPenalty = 0;
            EnemyRollPenaltyTurns = 0;
            
            // Clear reroll system
            RerollCharges = 0;
            UsedRerollThisTurn = false;
            RerollChargesUsed = 0;
            
            // Clear ACTION/ATTACK bonuses
            ActionBonuses.Clear();
            AttackBonuses.Clear();
        }
        
        /// <summary>
        /// Adds ACTION/ATTACK bonuses from an action
        /// </summary>
        public void AddActionAttackBonuses(ActionAttackBonuses? bonuses)
        {
            if (bonuses == null || bonuses.BonusGroups == null)
                return;
            
            foreach (var group in bonuses.BonusGroups)
            {
                if (group.Keyword == "ACTION")
                {
                    ActionBonuses.Add(group);
                }
                else if (group.Keyword == "ATTACK")
                {
                    AttackBonuses.Add(group);
                }
            }
        }
        
        /// <summary>
        /// Gets and consumes ACTION bonuses for the next action (only if action succeeds)
        /// Returns all bonuses that should be applied
        /// </summary>
        public List<ActionAttackBonusItem> GetAndConsumeActionBonuses(bool actionSucceeded)
        {
            var result = new List<ActionAttackBonusItem>();
            
            if (!actionSucceeded)
            {
                // ACTION bonuses are NOT consumed on failure
                return result;
            }
            
            // Collect bonuses from all ACTION groups
            var groupsToRemove = new List<ActionAttackBonusGroup>();
            
            foreach (var group in ActionBonuses)
            {
                if (group.Count > 0 && group.Bonuses != null)
                {
                    // Add all bonuses from this group
                    result.AddRange(group.Bonuses);
                    
                    // Decrement count
                    group.Count--;
                    
                    // Remove group if count reaches 0
                    if (group.Count <= 0)
                    {
                        groupsToRemove.Add(group);
                    }
                }
            }
            
            // Remove exhausted groups
            foreach (var group in groupsToRemove)
            {
                ActionBonuses.Remove(group);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets and consumes ATTACK bonuses for the next roll (consumed regardless of hit/miss)
        /// Returns all bonuses that should be applied
        /// </summary>
        public List<ActionAttackBonusItem> GetAndConsumeAttackBonuses()
        {
            var result = new List<ActionAttackBonusItem>();
            
            // Collect bonuses from all ATTACK groups
            var groupsToRemove = new List<ActionAttackBonusGroup>();
            
            foreach (var group in AttackBonuses)
            {
                if (group.Count > 0 && group.Bonuses != null)
                {
                    // Add all bonuses from this group
                    result.AddRange(group.Bonuses);
                    
                    // Decrement count (ATTACK bonuses are always consumed)
                    group.Count--;
                    
                    // Remove group if count reaches 0
                    if (group.Count <= 0)
                    {
                        groupsToRemove.Add(group);
                    }
                }
            }
            
            // Remove exhausted groups
            foreach (var group in groupsToRemove)
            {
                AttackBonuses.Remove(group);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets ACTION bonuses without consuming them (for preview/display)
        /// </summary>
        public List<ActionAttackBonusItem> PeekActionBonuses()
        {
            var result = new List<ActionAttackBonusItem>();
            
            foreach (var group in ActionBonuses)
            {
                if (group.Bonuses != null)
                {
                    result.AddRange(group.Bonuses);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets ATTACK bonuses without consuming them (for preview/display)
        /// </summary>
        public List<ActionAttackBonusItem> PeekAttackBonuses()
        {
            var result = new List<ActionAttackBonusItem>();
            
            foreach (var group in AttackBonuses)
            {
                if (group.Bonuses != null)
                {
                    result.AddRange(group.Bonuses);
                }
            }
            
            return result;
        }

        public bool UseReroll()
        {
            if (RerollCharges > 0 && !UsedRerollThisTurn)
            {
                RerollCharges--;
                UsedRerollThisTurn = true;
                return true;
            }
            return false;
        }

        public void ResetRerollUsage()
        {
            UsedRerollThisTurn = false;
        }

        public void ResetRerollCharges()
        {
            RerollChargesUsed = 0;
        }

        public int GetRemainingRerollCharges(int totalRerollCharges)
        {
            return Math.Max(0, totalRerollCharges - RerollChargesUsed);
        }

        public bool UseRerollCharge()
        {
            if (RerollCharges > 0)
            {
                RerollChargesUsed++;
                return true;
            }
            return false;
        }
    }
}


