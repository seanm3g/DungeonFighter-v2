using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Template for adding class actions to eliminate duplication in ClassActionManager
    /// </summary>
    public static class ActionAdditionTemplate
    {
        /// <summary>
        /// Adds a class action to an Actor if the required points are met
        /// </summary>
        /// <param name="Actor">The Actor to add the action to</param>
        /// <param name="actionName">Name of the action to add</param>
        /// <param name="weight">Weight/chance of the action</param>
        /// <param name="requiredPoints">Required class points</param>
        /// <param name="currentPoints">Current class points</param>
        /// <param name="className">Name of the class (for logging)</param>
        /// <returns>True if action was added, false otherwise</returns>
        public static bool AddClassAction(Actor Actor, string actionName, double weight, int requiredPoints, int currentPoints, string className)
        {
            if (currentPoints >= requiredPoints)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    Actor.AddAction(action, weight);
                    DebugLogger.Log("ClassActionManager", $"Added {actionName} to {Actor.Name} ({className} Points: {currentPoints})");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds multiple class actions based on a configuration list
        /// </summary>
        /// <param name="Actor">The Actor to add actions to</param>
        /// <param name="actionConfigs">List of action configurations</param>
        /// <param name="currentPoints">Current class points</param>
        /// <param name="className">Name of the class (for logging)</param>
        /// <returns>Number of actions added</returns>
        public static int AddClassActions(Actor Actor, List<ClassActionConfig> actionConfigs, int currentPoints, string className)
        {
            int actionsAdded = 0;
            
            foreach (var config in actionConfigs)
            {
                if (AddClassAction(Actor, config.ActionName, config.Weight, config.RequiredPoints, currentPoints, className))
                {
                    actionsAdded++;
                }
            }
            
            return actionsAdded;
        }

        /// <summary>
        /// Gets the standard class action configurations
        /// </summary>
        /// <returns>Dictionary of class names to their action configurations</returns>
        public static Dictionary<string, List<ClassActionConfig>> GetStandardClassActionConfigs()
        {
            return new Dictionary<string, List<ClassActionConfig>>
            {
                ["Barbarian"] = new List<ClassActionConfig>
                {
                    new ClassActionConfig("FOLLOW THROUGH", 0.3, 2)
                },
                ["Warrior"] = new List<ClassActionConfig>
                {
                    new ClassActionConfig("TAUNT", 0.4, 2)
                },
                ["Rogue"] = new List<ClassActionConfig>
                {
                    new ClassActionConfig("MISDIRECT", 0.35, 2)
                },
                ["Wizard"] = new List<ClassActionConfig>
                {
                    new ClassActionConfig("CHANNEL", 0.4, 2),   // Tier 1
                    new ClassActionConfig("HEAL", 0.2, 20),            // Tier 2
                    new ClassActionConfig("BLESS", 0.15, 60)           // Tier 3
                }
            };
        }
    }

    /// <summary>
    /// Configuration for a class action
    /// </summary>
    public class ClassActionConfig
    {
        public string ActionName { get; set; }
        public double Weight { get; set; }
        public int RequiredPoints { get; set; }

        public ClassActionConfig(string actionName, double weight, int requiredPoints)
        {
            ActionName = actionName;
            Weight = weight;
            RequiredPoints = requiredPoints;
        }
    }
}


