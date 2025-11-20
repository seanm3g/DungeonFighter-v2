using System;

namespace RPGGame
{
    /// <summary>
    /// Manages status effects for entities (stun, weaken, etc.)
    /// Extracted from Actor.cs to improve maintainability and organization
    /// </summary>
    public class StatusEffectManager
    {
        // Stun debuff system
        public bool IsStunned { get; set; } = false;
        public int StunTurnsRemaining { get; set; } = 0;
        
        // Weaken debuff system
        public bool IsWeakened { get; set; } = false;
        public int WeakenTurns { get; set; } = 0;
        public double WeakenMultiplier { get; set; } = 0.5; // Damage reduction when weakened (50% outgoing damage)

        /// <summary>
        /// Applies stun effect to the Actor
        /// </summary>
        /// <param name="turns">Number of turns to be stunned</param>
        public void ApplyStun(int turns)
        {
            IsStunned = true;
            StunTurnsRemaining = turns;
        }

        /// <summary>
        /// Applies weaken debuff to the Actor
        /// </summary>
        /// <param name="turns">Number of turns to be weakened</param>
        public void ApplyWeaken(int turns)
        {
            IsWeakened = true;
            WeakenTurns = turns;
        }

        /// <summary>
        /// Updates status effects that decay over time
        /// </summary>
        /// <param name="turnsPassed">Number of turns that have passed</param>
        public void UpdateEffects(double turnsPassed)
        {
            // Update stun effects
            if (StunTurnsRemaining > 0)
            {
                StunTurnsRemaining = Math.Max(0, StunTurnsRemaining - (int)Math.Ceiling(turnsPassed));
                if (StunTurnsRemaining == 0)
                    IsStunned = false;
            }
            
            // Update weaken debuff
            if (WeakenTurns > 0)
            {
                WeakenTurns = Math.Max(0, WeakenTurns - (int)Math.Ceiling(turnsPassed));
                if (WeakenTurns == 0)
                {
                    IsWeakened = false;
                }
            }
        }

        /// <summary>
        /// Clears all status effects
        /// </summary>
        public void ClearAllEffects()
        {
            // Clear stun effects
            IsStunned = false;
            StunTurnsRemaining = 0;
            
            // Clear weaken effects
            IsWeakened = false;
            WeakenTurns = 0;
        }

        /// <summary>
        /// Checks if the Actor can act (not stunned)
        /// </summary>
        /// <returns>True if the Actor can act, false if stunned</returns>
        public bool CanAct()
        {
            return !IsStunned;
        }

        /// <summary>
        /// Gets the damage multiplier based on current status effects
        /// </summary>
        /// <returns>Damage multiplier (1.0 = normal, 0.5 = weakened)</returns>
        public double GetDamageMultiplier()
        {
            return IsWeakened ? WeakenMultiplier : 1.0;
        }
    }
}


