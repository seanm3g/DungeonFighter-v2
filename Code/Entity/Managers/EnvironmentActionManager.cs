using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages environment-specific actions
    /// Handles adding and removing actions based on current dungeon environment
    /// </summary>
    public class EnvironmentActionManager
    {
        /// <summary>
        /// Adds environment-specific actions to actor
        /// </summary>
        public void AddEnvironmentActions(Actor entity, Environment environment)
        {
            if (environment != null && environment.ActionPool.Count > 0)
            {
                foreach (var (action, probability) in environment.ActionPool)
                {
                    entity.AddAction(action, probability * 0.5);
                }
                
                DebugLogger.LogFormat("EnvironmentActionManager", 
                    "Added {0} environment actions from {1}", 
                    environment.ActionPool.Count, 
                    environment.Name);
            }
        }

        /// <summary>
        /// Removes all environment-specific actions from entity
        /// </summary>
        public void ClearEnvironmentActions(Actor entity) {  
            var actionsToRemove = new List<Action>();
            
            foreach (var (action, probability) in entity.ActionPool)
            {
                if (IsEnvironmentAction(action))
                {
                    actionsToRemove.Add(action);
                }
            }
            
            foreach (var action in actionsToRemove)
            {
                try
                {
                    entity.RemoveAction(action);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogFormat("EnvironmentActionManager", 
                        "Error removing environment action {0}: {1}", 
                        action.Name, 
                        ex.Message);
                }
            }
            
            if (actionsToRemove.Count > 0)
            {
                DebugLogger.LogFormat("EnvironmentActionManager", 
                    "Cleared {0} environment actions", 
                    actionsToRemove.Count);
            }
        }

        /// <summary>
        /// Determines if an action is environment-specific
        /// </summary>
        private bool IsEnvironmentAction(Action action)
        {
            if (action == null) return false;
            
            if (action.Name.Contains("LAVA", StringComparison.OrdinalIgnoreCase) ||
                action.Name.Contains("ICE", StringComparison.OrdinalIgnoreCase) ||
                action.Name.Contains("SWAMP", StringComparison.OrdinalIgnoreCase) ||
                action.Name.Contains("FOREST", StringComparison.OrdinalIgnoreCase) ||
                action.Name.Contains("CAVE", StringComparison.OrdinalIgnoreCase) ||
                action.Name.Contains("GHOSTLY", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (action.Tags != null && action.Tags.Any(t => 
                t.Equals("environment", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}






