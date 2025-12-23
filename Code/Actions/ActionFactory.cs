using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Factory class for creating common actions, eliminating duplication across the codebase
    /// </summary>
    public static class ActionFactory
    {
        /// <summary>
        /// Creates a BASIC ATTACK action - REMOVED
        /// BASIC ATTACK has been removed from the game
        /// </summary>
        [Obsolete("BASIC ATTACK has been removed from the game. This method always returns null.")]
        public static Action? CreateBasicAttack()
        {
            return null;
        }

        /// <summary>
        /// Ensures BASIC ATTACK is available - REMOVED
        /// BASIC ATTACK has been removed from the game
        /// </summary>
        [Obsolete("BASIC ATTACK has been removed from the game. This method always returns null.")]
        public static Action? EnsureBasicAttackAvailable(Actor Actor)
        {
            return null;
        }

        /// <summary>
        /// Gets BASIC ATTACK - REMOVED
        /// BASIC ATTACK has been removed from the game
        /// </summary>
        [Obsolete("BASIC ATTACK has been removed from the game. This method always returns null.")]
        public static Action? GetBasicAttack(Actor Actor)
        {
            return null;
        }

        /// <summary>
        /// Creates a basic attack action for normal attacks (rolls 6-13)
        /// This is a non-combo action used when the roll is below the combo threshold (14)
        /// </summary>
        /// <returns>A basic attack action that is NOT a combo action</returns>
        public static Action CreateNormalAttack()
        {
            return new Action(
                name: "BASIC ATTACK",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                cooldown: 0,
                description: "A basic attack",
                comboOrder: 0,
                damageMultiplier: 1.0,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                causesPoison: false,
                causesStun: false,
                isComboAction: false, // Important: This is NOT a combo action
                comboBonusAmount: 0,
                comboBonusDuration: 0
            );
        }
    }
}


