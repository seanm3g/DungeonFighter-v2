using System;

namespace RPGGame
{
    /// <summary>
    /// Manages damage-over-time effects for entities (poison, burn, bleed)
    /// Extracted from Entity.cs to improve maintainability and organization
    /// </summary>
    public class DamageEffectManager
    {
        // Poison/Burn system
        public int PoisonDamage { get; set; } = 0;
        public int PoisonStacks { get; set; } = 0;
        public double LastPoisonTick { get; set; } = 0.0;
        public bool IsBleeding { get; set; } = false;
        public int BurnDamage { get; set; } = 0;
        public int BurnStacks { get; set; } = 0;
        public double LastBurnTick { get; set; } = 0.0;

        /// <summary>
        /// Applies poison damage to the entity
        /// </summary>
        /// <param name="damage">Base damage per stack</param>
        /// <param name="stacks">Number of stacks to apply</param>
        /// <param name="isBleeding">Whether this is bleeding damage</param>
        public void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false)
        {
            if (PoisonStacks == 0 || PoisonDamage != damage)
            {
                PoisonDamage = damage;
            }
            PoisonStacks += stacks;
            LastPoisonTick = GameTicker.Instance.GetCurrentGameTime();
            IsBleeding = isBleeding;
        }

        /// <summary>
        /// Applies burn damage to the entity
        /// </summary>
        /// <param name="damage">Base damage per stack</param>
        /// <param name="stacks">Number of stacks to apply</param>
        public void ApplyBurn(int damage, int stacks = 1)
        {
            if (BurnStacks == 0 || BurnDamage != damage)
            {
                BurnDamage = damage;
            }
            BurnStacks += stacks;
            LastBurnTick = GameTicker.Instance.GetCurrentGameTime();
        }

        /// <summary>
        /// Processes poison damage over time
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from poison this tick</returns>
        public int ProcessPoison(double currentTime)
        {
            if (PoisonStacks > 0)
            {
                var poisonConfig = GameConfiguration.Instance.Poison;
                
                if (currentTime - LastPoisonTick >= poisonConfig.TickInterval)
                {
                    int totalDamage = PoisonDamage * PoisonStacks;
                    LastPoisonTick = currentTime;
                    
                    PoisonStacks--;
                    if (PoisonStacks <= 0)
                    {
                        PoisonDamage = 0;
                        PoisonStacks = 0;
                        IsBleeding = false;
                    }
                    return totalDamage;
                }
            }
            return 0;
        }

        /// <summary>
        /// Processes burn damage over time
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from burn this tick</returns>
        public int ProcessBurn(double currentTime)
        {
            if (BurnStacks > 0)
            {
                var poisonConfig = GameConfiguration.Instance.Poison;
                
                if (currentTime - LastBurnTick >= poisonConfig.TickInterval)
                {
                    int totalDamage = BurnDamage * BurnStacks;
                    LastBurnTick = currentTime;
                    
                    BurnStacks--;
                    if (BurnStacks <= 0)
                    {
                        BurnDamage = 0;
                        BurnStacks = 0;
                    }
                    return totalDamage;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the damage type text for poison effects
        /// </summary>
        /// <returns>Either "bleed" or "poison"</returns>
        public string GetDamageTypeText()
        {
            return IsBleeding ? "bleed" : "poison";
        }

        /// <summary>
        /// Clears all damage-over-time effects
        /// </summary>
        public void ClearAllEffects()
        {
            // Clear poison/bleed effects
            PoisonDamage = 0;
            PoisonStacks = 0;
            IsBleeding = false;
            LastPoisonTick = 0.0;
            
            // Clear burn effects
            BurnDamage = 0;
            BurnStacks = 0;
            LastBurnTick = 0.0;
        }

        /// <summary>
        /// Checks if the entity has any active damage-over-time effects
        /// </summary>
        /// <returns>True if the entity has active DoT effects</returns>
        public bool HasActiveEffects()
        {
            return PoisonStacks > 0 || BurnStacks > 0;
        }

        /// <summary>
        /// Gets the total number of active effect stacks
        /// </summary>
        /// <returns>Total number of poison and burn stacks</returns>
        public int GetTotalStacks()
        {
            return PoisonStacks + BurnStacks;
        }
    }
}
