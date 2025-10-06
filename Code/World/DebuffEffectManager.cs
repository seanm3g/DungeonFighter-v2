using System;

namespace RPGGame
{
    /// <summary>
    /// Manages debuff effects for entities (roll penalties, damage reduction, critical miss penalties)
    /// Extracted from Entity.cs to improve maintainability and organization
    /// </summary>
    public class DebuffEffectManager
    {
        // Roll penalty system (for effects like Dust Cloud)
        public int RollPenalty { get; set; } = 0;
        public int RollPenaltyTurns { get; set; } = 0;
        
        // Damage reduction system
        public double DamageReduction { get; set; } = 0.0;
        
        // Critical miss system - doubles action speed for next turn
        public bool HasCriticalMissPenalty { get; set; } = false;
        public int CriticalMissPenaltyTurns { get; set; } = 0;

        /// <summary>
        /// Applies a roll penalty to the entity
        /// </summary>
        /// <param name="penalty">Amount to reduce rolls by</param>
        /// <param name="turns">Number of turns the penalty lasts</param>
        public void ApplyRollPenalty(int penalty, int turns)
        {
            RollPenalty = penalty;
            RollPenaltyTurns = turns;
        }

        /// <summary>
        /// Applies damage reduction to the entity
        /// </summary>
        /// <param name="reduction">Amount of damage reduction (0.0 = no reduction, 1.0 = complete immunity)</param>
        public void ApplyDamageReduction(double reduction)
        {
            DamageReduction = Math.Max(DamageReduction, reduction);
        }

        /// <summary>
        /// Applies critical miss penalty to the entity
        /// </summary>
        /// <param name="turns">Number of turns the penalty lasts</param>
        public void ApplyCriticalMissPenalty(int turns)
        {
            HasCriticalMissPenalty = true;
            CriticalMissPenaltyTurns = turns;
        }

        /// <summary>
        /// Updates debuff effects that decay over time
        /// </summary>
        /// <param name="turnsPassed">Number of turns that have passed</param>
        public void UpdateEffects(double turnsPassed)
        {
            // Update roll penalty effects
            if (RollPenaltyTurns > 0)
            {
                RollPenaltyTurns = Math.Max(0, RollPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (RollPenaltyTurns == 0)
                    RollPenalty = 0;
            }
            
            // Update damage reduction decay (per turn, not per action)
            if (DamageReduction > 0)
            {
                DamageReduction = Math.Max(0.0, DamageReduction - (0.1 * Math.Ceiling(turnsPassed)));
            }
            
            // Update critical miss penalty
            if (CriticalMissPenaltyTurns > 0)
            {
                CriticalMissPenaltyTurns = Math.Max(0, CriticalMissPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (CriticalMissPenaltyTurns == 0)
                    HasCriticalMissPenalty = false;
            }
        }

        /// <summary>
        /// Clears all debuff effects
        /// </summary>
        public void ClearAllEffects()
        {
            // Clear roll penalty effects
            RollPenalty = 0;
            RollPenaltyTurns = 0;
            
            // Clear damage reduction
            DamageReduction = 0.0;
            
            // Clear critical miss penalty
            HasCriticalMissPenalty = false;
            CriticalMissPenaltyTurns = 0;
        }

        /// <summary>
        /// Calculates the effective damage after applying damage reduction
        /// </summary>
        /// <param name="baseDamage">Base damage amount</param>
        /// <returns>Damage after reduction is applied</returns>
        public int CalculateReducedDamage(int baseDamage)
        {
            if (DamageReduction <= 0)
                return baseDamage;
                
            return (int)Math.Max(1, baseDamage * (1.0 - DamageReduction));
        }

        /// <summary>
        /// Calculates the effective roll after applying roll penalty
        /// </summary>
        /// <param name="baseRoll">Base roll value</param>
        /// <returns>Roll after penalty is applied</returns>
        public int CalculatePenalizedRoll(int baseRoll)
        {
            return Math.Max(1, baseRoll - RollPenalty);
        }

        /// <summary>
        /// Checks if the entity has any active debuff effects
        /// </summary>
        /// <returns>True if the entity has active debuff effects</returns>
        public bool HasActiveEffects()
        {
            return RollPenaltyTurns > 0 || DamageReduction > 0 || CriticalMissPenaltyTurns > 0;
        }
    }
}
