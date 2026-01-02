using System;
using System.Collections.Generic;
using System.Linq;

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
                if (statBonus.StatType == "RollBonus" || statBonus.StatType == "Roll")
                {
                    totalRollBonus += (int)statBonus.Value;
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
                if (statBonus.StatType == "RollBonus" || statBonus.StatType == "Roll")
                {
                    totalRollBonus += (int)statBonus.Value;
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
        /// Gets list of action names that should be added for given gear
        /// Returns the GearAction if it exists, plus all ActionBonuses
        /// For weapons: If no actions are found, falls back to weapon-type actions to ensure weapons always have at least one action
        /// </summary>
        public List<string> GetGearActions(Item gear)
        {
            var actions = new List<string>();
            
            if (gear == null) return actions;

            // Add the specific GearAction assigned to this item
            if (!string.IsNullOrEmpty(gear.GearAction))
            {
                actions.Add(gear.GearAction);
                DebugLogger.LogFormat("GearActionManager", 
                    "Found GearAction '{0}' on item '{1}'", gear.GearAction, gear.Name);
            }

            // Add action bonuses from gear (these are shown in item descriptions and should be available)
            if (gear.ActionBonuses != null)
            {
                foreach (var actionBonus in gear.ActionBonuses)
                {
                    if (!string.IsNullOrEmpty(actionBonus.Name))
                    {
                        actions.Add(actionBonus.Name);
                        DebugLogger.LogFormat("GearActionManager", 
                            "Found ActionBonus '{0}' on item '{1}'", actionBonus.Name, gear.Name);
                    }
                }
            }

            // For weapons: If no actions found, fall back to ALL weapon-type actions
            // This ensures weapons ALWAYS have at least one action when equipped
            // This matches what's shown in inventory (all weapon-type actions)
            if (gear is WeaponItem weapon && actions.Count == 0)
            {
                var weaponTypeActions = GetWeaponTypeActions(weapon.WeaponType);
                if (weaponTypeActions.Count > 0)
                {
                    // Add ALL weapon-type actions as fallback (matches inventory display)
                    actions.AddRange(weaponTypeActions);
                    DebugLogger.LogFormat("GearActionManager", 
                        "No GearAction or ActionBonuses found for weapon '{0}', using fallback weapon-type actions: {1}", 
                        weapon.Name, string.Join(", ", weaponTypeActions));
                }
                else
                {
                    DebugLogger.LogFormat("GearActionManager", 
                        "WARNING: Weapon '{0}' has no actions (no GearAction, no ActionBonuses, and no weapon-type actions found)", 
                        weapon.Name);
                }
            }

            return actions;
        }

        /// <summary>
        /// Gets base actions for a weapon type using tag-based matching from JSON.
        /// This ensures all actions with matching tags are included, not just hardcoded ones.
        /// </summary>
        private List<string> GetWeaponTypeActions(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            var allActions = ActionLoader.GetAllActions();

            // Get weapon-specific actions from JSON using tag matching
            // Actions must have both "weapon" tag and the weapon type tag (e.g., "wand", "mace")
            // Exclude "class" tagged actions (class-only actions cannot appear on weapons)
            var weaponActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            // No fallback - return empty list if no actions found
            // This ensures we don't add BASIC ATTACK
            return weaponActions;
        }

        /// <summary>
        /// Checks if gear has special armor actions
        /// </summary>
        private bool HasSpecialArmorActions(Item armor)
        {
            if (armor == null) return false;
            
            if (armor.Modifications != null && armor.Modifications.Count > 0) return true;
            if (armor.StatBonuses != null && armor.StatBonuses.Count > 0) return true;
            // Check if rarity is above Uncommon (Rare, Epic, Legendary)
            if (armor.Rarity == "Rare" || armor.Rarity == "Epic" || armor.Rarity == "Legendary") return true;

            return false;
        }

        /// <summary>
        /// Gets a random armor action name
        /// </summary>
        private string? GetRandomArmorActionName()
        {
            try
            {
                var allActions = ActionLoader.GetAllActions();
                var availableActions = allActions
                    .Where(action => action.IsComboAction)
                    .Select(action => action.Name)
                    .ToList();

                if (availableActions.Count > 0)
                {
                    int randomIndex = Random.Shared.Next(availableActions.Count);
                    return availableActions[randomIndex];
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionManager", 
                    "Error getting random armor action: {0}", ex.Message);
            }

            return null;
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
            try
            {
                if (entity == null)
                {
                    DebugLogger.LogFormat("GearActionManager", 
                        "ERROR: Entity is null when trying to load action '{0}'", actionName);
                    return;
                }
                
                if (string.IsNullOrEmpty(actionName))
                {
                    DebugLogger.LogFormat("GearActionManager", 
                        "ERROR: Action name is null or empty");
                    return;
                }
                
                DebugLogger.LogFormat("GearActionManager", 
                    "Attempting to load action '{0}' from JSON for entity '{1}'", actionName, entity.Name);
                
                // Ensure actions are loaded (GetAction will load if needed, but we ensure it here)
                ActionLoader.LoadActions();
                
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    // Mark gear actions as combo actions so they can be used in combo sequences
                    // GetActionPool() returns all actions, but marking as combo actions allows them to be added to combo sequences
                    action.IsComboAction = true;
                    DebugLogger.LogFormat("GearActionManager", 
                        "Marked gear action '{0}' as combo action", actionName);
                    
                    // Set ComboOrder for the action
                    if (entity.ActionPool != null)
                    {
                        var comboActions = entity.ActionPool
                            .Where(a => a.action.IsComboAction)
                            .ToList();
                        
                        int maxOrder = comboActions.Count > 0 
                            ? comboActions.Max(a => a.action.ComboOrder) 
                            : 0;
                        
                        action.ComboOrder = maxOrder + 1;
                    }

                    // Verify ActionPool is not null before adding
                    if (entity.ActionPool == null)
                    {
                        DebugLogger.LogFormat("GearActionManager", 
                            "ERROR: Entity ActionPool is null for entity '{0}'", entity.Name);
                        return;
                    }
                    
                    int poolSizeBefore = entity.ActionPool.Count;
                    entity.AddAction(action, 1.0);
                    int poolSizeAfter = entity.ActionPool.Count;
                    
                    // Verify action was actually added
                    bool actionExists = entity.ActionPool.Any(a => a.action.Name == actionName);
                    
                    DebugLogger.LogFormat("GearActionManager", 
                        "Successfully loaded and added gear action '{0}' to entity '{1}' action pool (pool size before: {2}, after: {3}, action exists: {4}, isComboAction: {5})", 
                        actionName, entity?.Name ?? "null", poolSizeBefore, poolSizeAfter, actionExists, action.IsComboAction);
                    
                    if (!actionExists)
                    {
                        DebugLogger.LogFormat("GearActionManager", 
                            "ERROR: Action '{0}' was not found in ActionPool after AddAction call", actionName);
                    }
                }
                else
                {
                    DebugLogger.LogFormat("GearActionManager", 
                        "WARNING: Action '{0}' not found in Actions.json - cannot add to action pool", actionName);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionManager", 
                    "Error loading gear action {0}: {1}\nStack trace: {2}", actionName, ex.Message, ex.StackTrace ?? "N/A");
            }
        }

        /// <summary>
        /// Removes actions from entity's action pool
        /// </summary>
        private void RemoveActionsFromPool(Actor entity, List<string> actionNames)
        {
            foreach (var actionName in actionNames)
            {
                var actionToRemove = entity.ActionPool.FirstOrDefault(a => 
                    a.action.Name == actionName);
                
                if (actionToRemove.action != null)
                {
                    try
                    {
                        entity.RemoveAction(actionToRemove.action);
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
}






