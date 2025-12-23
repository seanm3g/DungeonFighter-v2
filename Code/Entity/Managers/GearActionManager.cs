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
        public void AddWeaponActions(Actor entity, WeaponItem weapon)
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
        public void AddArmorActions(Actor entity, Item armor)
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
            var weaponActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)))
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
                    return;
                }
                
                DebugLogger.LogFormat("GearActionManager", 
                    "Attempting to load action '{0}' from JSON for entity '{1}'", actionName, entity.Name);
                
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    // CRITICAL: Mark gear actions as combo actions so they appear in the action pool
                    // The GetActionPool() method only returns actions with IsComboAction == true
                    if (!action.IsComboAction)
                    {
                        action.IsComboAction = true;
                        DebugLogger.LogFormat("GearActionManager", 
                            "Marked action '{0}' as combo action so it appears in action pool", actionName);
                    }
                    
                    if (action.IsComboAction && entity.ActionPool != null)
                    {
                        var comboActions = entity.ActionPool
                            .Where(a => a.action.IsComboAction)
                            .ToList();
                        
                        int maxOrder = comboActions.Count > 0 
                            ? comboActions.Max(a => a.action.ComboOrder) 
                            : 0;
                        
                        action.ComboOrder = maxOrder + 1;
                    }

                    entity.AddAction(action, 1.0);
                    DebugLogger.LogFormat("GearActionManager", 
                        "Successfully loaded and added gear action '{0}' to entity '{1}' action pool (pool size: {2}, isComboAction: {3})", 
                        actionName, entity?.Name ?? "null", entity?.ActionPool?.Count ?? 0, action.IsComboAction);
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
                    "Error loading gear action {0}: {1}", actionName, ex.Message);
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






