using System;

namespace RPGGame
{
    /// <summary>
    /// Manages character status effects, debuffs, and temporary conditions
    /// </summary>
    public class CharacterEffects
    {
        // Combo system state
        public int ComboStep { get; set; } = 0;
        public double ComboAmplifier { get; set; } = 1.0;
        public int ComboBonus { get; set; } = 0;
        public int TempComboBonus { get; set; } = 0;
        public int TempComboBonusTurns { get; set; } = 0;
        
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
        // DamageReduction is now handled by Actor base class
        public double LengthReduction { get; set; } = 0.0; // For Taunt
        public int LengthReductionTurns { get; set; } = 0;
        public double ComboAmplifierMultiplier { get; set; } = 1.0; // For Pretty Boy Swag
        public int ComboAmplifierTurns { get; set; } = 0;
        
        // Divine reroll system
        public int RerollCharges { get; set; } = 0;
        public bool UsedRerollThisTurn { get; set; } = false;
        public int RerollChargesUsed { get; set; } = 0;

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
            ComboStep = 0;
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
            ComboStep = 0;
            ComboAmplifier = 1.0;
            LastComboActionIdx = -1;
            ComboModeActive = false;
            
            // Clear temporary bonuses
            TempComboBonus = 0;
            TempComboBonusTurns = 0;
            ExtraDamage = 0;
            ExtraAttacks = 0;
            
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


