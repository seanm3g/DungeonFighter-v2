using System;
using System.Collections.Generic;
using RPGGame.Entity.Managers;

namespace RPGGame
{
    /// <summary>
    /// Manages actions and bonuses from equipped gear (weapons and armor)
    /// Handles adding/removing actions from gear and applying roll bonuses
    /// </summary>
    public class GearActionManager
    {
        /// <summary>
        /// Adds weapon-specific actions to actor
        /// </summary>
        public void AddWeaponActions(Actor entity, WeaponItem? weapon)
        {
            if (weapon == null) return;
            
            try
            {
                AddGearActions(entity, weapon);
                ApplyRollBonusesFromGear(entity, weapon);
                DebugLogger.LogFormat("GearActionManager", 
                    "Added weapon actions for: {0}", weapon.Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionManager", 
                    "Error adding weapon actions: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Adds armor-specific actions to actor
        /// </summary>
        public void AddArmorActions(Actor entity, Item? armor)
        {
            if (armor == null) return;
            
            try
            {
                AddGearActions(entity, armor);
                ApplyRollBonusesFromGear(entity, armor);
                DebugLogger.LogFormat("GearActionManager", 
                    "Added armor actions for: {0}", armor.Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionManager", 
                    "Error adding armor actions: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Removes weapon-specific actions from entity
        /// </summary>
        public void RemoveWeaponActions(Actor entity, WeaponItem? weapon)
        {
            if (weapon == null) return;
            
            try
            {
                var actions = GetGearActions(weapon);
                RemoveActionsFromPool(entity, actions);
                RemoveRollBonusesFromGear(entity, weapon);
                DebugLogger.LogFormat("GearActionManager", 
                    "Removed weapon actions for: {0}", weapon.Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionManager", 
                    "Error removing weapon actions: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Removes armor-specific actions from entity
        /// </summary>
        public void RemoveArmorActions(Actor entity, Item? armor)
        {
            if (armor == null) return;
            
            try
            {
                var actions = GetGearActions(armor);
                RemoveActionsFromPool(entity, actions);
                RemoveRollBonusesFromGear(entity, armor);
                DebugLogger.LogFormat("GearActionManager", 
                    "Removed armor actions for: {0}", armor.Name);
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionManager", 
                    "Error removing armor actions: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Applies roll bonuses from gear to all actions
        /// </summary>
        public void ApplyRollBonusesFromGear(Actor entity, Item gear)
        {
            if (entity == null || gear == null) return;

            int totalRollBonus = 0;
            foreach (var statBonus in gear.StatBonuses)
            {
                foreach (var (contribType, contribValue) in statBonus.EnumerateContributions())
                {
                    if (string.Equals(contribType, "RollBonus", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contribType, "Roll", StringComparison.OrdinalIgnoreCase))
                        totalRollBonus += (int)contribValue;
                }
            }

            if (totalRollBonus > 0)
            {
                // Note: Roll bonuses would be applied to actions here if the Action class supported them
                // For now, this logic is commented out pending Action class enhancement
                DebugLogger.LogFormat("GearActionManager", 
                    "Gear {0} provides +{1} roll bonus", 
                    gear.Name, totalRollBonus);
            }
        }

        /// <summary>
        /// Removes roll bonuses from gear
        /// </summary>
        public void RemoveRollBonusesFromGear(Actor entity, Item gear)
        {
            if (entity == null || gear == null) return;

            int totalRollBonus = 0;
            foreach (var statBonus in gear.StatBonuses)
            {
                foreach (var (contribType, contribValue) in statBonus.EnumerateContributions())
                {
                    if (string.Equals(contribType, "RollBonus", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contribType, "Roll", StringComparison.OrdinalIgnoreCase))
                        totalRollBonus += (int)contribValue;
                }
            }

            if (totalRollBonus > 0)
            {
                // Note: Roll bonuses would be removed from actions here if the Action class supported them
                // For now, this logic is commented out pending Action class enhancement
                DebugLogger.LogFormat("GearActionManager", 
                    "Removing +{0} roll bonus from gear {1}", 
                    totalRollBonus, gear.Name);
            }
        }

        /// <summary>
        /// Gets list of action names that should be added for given gear (shared rules with inventory UI).
        /// </summary>
        public List<string> GetGearActions(Item gear)
        {
            var actions = GearActionNames.Resolve(gear);
            if (gear != null && actions.Count > 0)
            {
                DebugLogger.LogFormat("GearActionManager",
                    "GetGearActions for '{0}': {1}", gear.Name, string.Join(", ", actions));
            }
            else if (gear is WeaponItem w && actions.Count == 0)
            {
                DebugLogger.LogFormat("GearActionManager",
                    "WARNING: Weapon '{0}' has no resolved gear actions", w.Name);
            }

            return actions;
        }

        /// <summary>
        /// Adds general gear actions to actor
        /// </summary>
        private void AddGearActions(Actor entity, Item gear)
        {
            var gearActions = GetGearActions(gear);
            DebugLogger.LogFormat("GearActionManager", 
                "AddGearActions: Found {0} actions for gear '{1}': {2}", 
                gearActions.Count, gear?.Name ?? "null", string.Join(", ", gearActions));
            
            foreach (var actionName in gearActions)
            {
                LoadGearActionFromJson(entity, actionName);
            }
        }

        /// <summary>
        /// Loads a single gear action from JSON and Adds to actor
        /// </summary>
        private void LoadGearActionFromJson(Actor entity, string actionName)
        {
            GearActionLoader.LoadGearActionFromJson(entity, actionName);
        }

        /// <summary>
        /// Removes actions from entity's action pool
        /// Removes all instances of each action name to handle duplicates
        /// </summary>
        private void RemoveActionsFromPool(Actor entity, List<string> actionNames)
        {
            foreach (var actionName in actionNames)
            {
                try
                {
                    // Remove all instances of this action name (handles duplicates)
                    entity.RemoveAllActionsByName(actionName);
                    DebugLogger.LogFormat("GearActionManager", 
                        "Removed all instances of action '{0}' from action pool", actionName);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogFormat("GearActionManager", 
                        "Error removing action {0}: {1}", actionName, ex.Message);
                }
            }
        }
    }
}






