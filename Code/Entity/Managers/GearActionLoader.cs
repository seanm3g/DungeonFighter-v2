using RPGGame;
using RPGGame.Data;
using System;
using System.Linq;

namespace RPGGame.Entity.Managers
{
    /// <summary>
    /// Loads gear actions from JSON and adds them to entities.
    /// Extracted from GearActionManager to reduce size and improve Single Responsibility Principle compliance.
    /// </summary>
    public static class GearActionLoader
    {
        /// <summary>
        /// Loads a single gear action from JSON and adds it to actor
        /// </summary>
        public static void LoadGearActionFromJson(Actor entity, string actionName)
        {
            try
            {
                if (entity == null)
                {
                    DebugLogger.LogFormat("GearActionLoader", 
                        "ERROR: Entity is null when trying to load action '{0}'", actionName);
                    return;
                }
                
                if (string.IsNullOrEmpty(actionName))
                {
                    DebugLogger.LogFormat("GearActionLoader", 
                        "ERROR: Action name is null or empty");
                    return;
                }
                
                DebugLogger.LogFormat("GearActionLoader", 
                    "Attempting to load action '{0}' from JSON for entity '{1}'", actionName, entity.Name);
                
                // Ensure actions are loaded (GetAction will load if needed, but we ensure it here)
                ActionLoader.LoadActions();
                
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    // Mark gear actions as combo actions so they can be used in combo sequences
                    // GetActionPool() returns all actions, but marking as combo actions allows them to be added to combo sequences
                    action.IsComboAction = true;
                    DebugLogger.LogFormat("GearActionLoader", 
                        "Marked gear action '{0}' as combo action", actionName);
                    
                    // Verify ActionPool is not null before adding
                    if (entity.ActionPool == null)
                    {
                        DebugLogger.LogFormat("GearActionLoader", 
                            "ERROR: Entity ActionPool is null for entity '{0}'", entity.Name);
                        return;
                    }
                    
                    // Use AddActionAllowDuplicates to support items with duplicate actions
                    // This allows the same action to appear multiple times if it's listed multiple times on an item
                    int poolSizeBefore = entity.ActionPool.Count;
                    entity.AddActionAllowDuplicates(action, 1.0);
                    int poolSizeAfter = entity.ActionPool.Count;
                    
                    // Verify action was actually added
                    bool actionExists = entity.ActionPool.Any(a => a.action.Name == actionName);
                    
                    DebugLogger.LogFormat("GearActionLoader", 
                        "Successfully loaded and added gear action '{0}' to entity '{1}' action pool (pool size before: {2}, after: {3}, action exists: {4}, isComboAction: {5})", 
                        actionName, entity?.Name ?? "null", poolSizeBefore, poolSizeAfter, actionExists, action.IsComboAction);
                    
                    if (!actionExists)
                    {
                        DebugLogger.LogFormat("GearActionLoader", 
                            "ERROR: Action '{0}' was not found in ActionPool after AddAction call", actionName);
                    }
                }
                else
                {
                    DebugLogger.LogFormat("GearActionLoader", 
                        "WARNING: Action '{0}' not found in Actions.json - cannot add to action pool", actionName);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("GearActionLoader", 
                    "Error loading gear action {0}: {1}\nStack trace: {2}", actionName, ex.Message, ex.StackTrace ?? "N/A");
            }
        }
    }
}
