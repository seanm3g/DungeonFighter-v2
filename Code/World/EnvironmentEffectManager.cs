using System;

namespace RPGGame
{
    /// <summary>
    /// Manages passive and active effects that the environment applies to combat.
    /// Handles effect types, values, and their application to entities.
    /// </summary>
    public class EnvironmentEffectManager
    {
        public PassiveEffectType PassiveEffectType { get; set; } = PassiveEffectType.None;
        public double PassiveEffectValue { get; set; } = 1.0;
        public Action? ActiveEffectAction { get; private set; }

        /// <summary>
        /// Applies a passive effect to a value (e.g., damage multiplier).
        /// </summary>
        /// <param name="value">The base value to apply the effect to</param>
        /// <returns>The value after the passive effect is applied</returns>
        public double ApplyPassiveEffect(double value)
        {
            if (PassiveEffectType == PassiveEffectType.DamageMultiplier)
                return value * PassiveEffectValue;
            
            if (PassiveEffectType == PassiveEffectType.SpeedMultiplier)
                return value * PassiveEffectValue;
            
            return value;
        }

        /// <summary>
        /// Applies the active effect (if any) to both the player and an enemy.
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="enemy">The enemy to apply the effect to</param>
        public void ApplyActiveEffect(Character player, Enemy enemy)
        {
            if (ActiveEffectAction != null)
            {
                // Environmental effects don't use BaseValue, use 0 damage or calculate from action type
                int dmg = 0;
                player.TakeDamage(dmg);
                enemy.TakeDamage(dmg);
            }
        }

        /// <summary>
        /// Sets up a passive effect with a specific type and value.
        /// </summary>
        public void SetPassiveEffect(PassiveEffectType type, double value)
        {
            PassiveEffectType = type;
            PassiveEffectValue = value;
        }

        /// <summary>
        /// Clears all passive effects.
        /// </summary>
        public void ClearPassiveEffect()
        {
            PassiveEffectType = PassiveEffectType.None;
            PassiveEffectValue = 1.0;
        }

        /// <summary>
        /// Sets up an active effect action.
        /// </summary>
        public void SetActiveEffectAction(Action action)
        {
            ActiveEffectAction = action;
        }

        /// <summary>
        /// Clears the active effect action.
        /// </summary>
        public void ClearActiveEffect()
        {
            ActiveEffectAction = null;
        }
    }
}

