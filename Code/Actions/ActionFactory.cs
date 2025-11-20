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
        /// Creates a BASIC ATTACK action with standard properties
        /// </summary>
        /// <returns>BASIC ATTACK action</returns>
        public static Action CreateBasicAttack()
        {
            return new Action(
                name: "BASIC ATTACK",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0, // Damage comes from STR + weapon
                range: 1,
                cooldown: 0,
                description: "A standard physical attack using STR + weapon damage",
                comboOrder: 0,
                damageMultiplier: 1.0,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                isComboAction: false
            );
        }

        /// <summary>
        /// Creates an emergency combo action when no combo actions are available
        /// </summary>
        /// <returns>Emergency combo action</returns>
        public static Action CreateEmergencyComboAction()
        {
            return new Action(
                name: "EMERGENCY STRIKE",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: "An emergency strike created when no combo actions were available",
                comboOrder: 1,
                damageMultiplier: 1.3,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                isComboAction: true
            );
        }

        /// <summary>
        /// Ensures BASIC ATTACK is available in an Actor's action pool
        /// </summary>
        /// <param name="Actor">The Actor to ensure has BASIC ATTACK</param>
        /// <returns>The BASIC ATTACK action (existing or newly created)</returns>
        public static Action EnsureBasicAttackAvailable(Actor Actor)
        {
            // Check if BASIC ATTACK is already in the action pool
            var existingBasicAttack = Actor.ActionPool
                .FirstOrDefault(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));

            if (existingBasicAttack.action != null)
            {
                return existingBasicAttack.action;
            }

            // Try to load BASIC ATTACK from JSON first
            var loadedAction = ActionLoader.GetAction("BASIC ATTACK");
            if (loadedAction != null)
            {
                Actor.AddAction(loadedAction, 1.0);
                DebugLogger.Log("ActionFactory", $"Added missing BASIC ATTACK to {Actor.Name}'s action pool from JSON");
                return loadedAction;
            }

            // Create BASIC ATTACK as fallback
            var basicAttack = CreateBasicAttack();
            Actor.AddAction(basicAttack, 1.0);
            DebugLogger.Log("ActionFactory", $"Created and added BASIC ATTACK to {Actor.Name}'s action pool");
            return basicAttack;
        }

        /// <summary>
        /// Gets or creates BASIC ATTACK for an Actor, with fallback creation
        /// </summary>
        /// <param name="Actor">The Actor to get BASIC ATTACK for</param>
        /// <returns>BASIC ATTACK action</returns>
        public static Action GetBasicAttack(Actor Actor)
        {
            // First try to find existing BASIC ATTACK
            var existingBasicAttack = Actor.ActionPool
                .FirstOrDefault(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));

            if (existingBasicAttack.action != null)
            {
                return existingBasicAttack.action;
            }

            // If not found, ensure it's available (will create if needed)
            return EnsureBasicAttackAvailable(Actor);
        }
    }
}


