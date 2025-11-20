using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Simplified class action manager using templates to eliminate duplication
    /// Replaces the original ClassActionManager with cleaner, more maintainable code
    /// </summary>
    public static class ClassActionManagerSimplified
    {
        /// <summary>
        /// Adds class-specific actions based on character progression and weapon type
        /// </summary>
        /// <param name="Actor">The Actor to add actions to</param>
        /// <param name="progression">The character's progression data</param>
        /// <param name="weaponType">The weapon type (optional)</param>
        public static void AddClassActions(Actor Actor, CharacterProgression progression, WeaponType? weaponType)
        {
            DebugLogger.LogMethodEntry("ClassActionManagerSimplified", "AddClassActions");
            
            // Remove existing class actions first
            RemoveClassActions(Actor);
            
            DebugLogger.LogClassPoints(progression.BarbarianPoints, progression.WarriorPoints, progression.RoguePoints, progression.WizardPoints);
            
            // Add actions using the template system
            AddBarbarianActions(Actor, progression);
            AddWarriorActions(Actor, progression);
            AddRogueActions(Actor, progression);
            AddWizardActions(Actor, progression, weaponType);
            
            DebugLogger.LogActionPoolChange(Actor.Name, Actor.ActionPool.Count, "After AddClassActions");
        }

        /// <summary>
        /// Removes all class-specific actions from the Actor
        /// </summary>
        /// <param name="Actor">The Actor to remove actions from</param>
        public static void RemoveClassActions(Actor Actor)
        {
            var classActions = GetClassActionNames();
            
            foreach (var actionName in classActions)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    Actor.RemoveAction(action);
                }
            }
        }

        /// <summary>
        /// Adds Barbarian class actions using the template system
        /// </summary>
        private static void AddBarbarianActions(Actor Actor, CharacterProgression progression)
        {
            var configs = ActionAdditionTemplate.GetStandardClassActionConfigs()["Barbarian"];
            ActionAdditionTemplate.AddClassActions(Actor, configs, progression.BarbarianPoints, "Barbarian");
        }

        /// <summary>
        /// Adds Warrior class actions using the template system
        /// </summary>
        private static void AddWarriorActions(Actor Actor, CharacterProgression progression)
        {
            var configs = ActionAdditionTemplate.GetStandardClassActionConfigs()["Warrior"];
            ActionAdditionTemplate.AddClassActions(Actor, configs, progression.WarriorPoints, "Warrior");
        }

        /// <summary>
        /// Adds Rogue class actions using the template system
        /// </summary>
        private static void AddRogueActions(Actor Actor, CharacterProgression progression)
        {
            var configs = ActionAdditionTemplate.GetStandardClassActionConfigs()["Rogue"];
            ActionAdditionTemplate.AddClassActions(Actor, configs, progression.RoguePoints, "Rogue");
        }

        /// <summary>
        /// Adds Wizard class actions using the template system with weapon-specific logic
        /// </summary>
        private static void AddWizardActions(Actor Actor, CharacterProgression progression, WeaponType? weaponType)
        {
            // Add standard wizard actions
            var standardConfigs = ActionAdditionTemplate.GetStandardClassActionConfigs()["Wizard"];
            ActionAdditionTemplate.AddClassActions(Actor, standardConfigs, progression.WizardPoints, "Wizard");

            // Add weapon-specific wizard actions
            if (progression.WizardPoints >= 5)
            {
                AddWeaponSpecificWizardActions(Actor, weaponType, progression.WizardPoints);
            }
        }

        /// <summary>
        /// Adds weapon-specific wizard actions
        /// </summary>
        private static void AddWeaponSpecificWizardActions(Actor Actor, WeaponType? weaponType, int wizardPoints)
        {
            switch (weaponType)
            {
                case WeaponType.Wand:
                    ActionAdditionTemplate.AddClassAction(Actor, "FIREBALL", 0.3, 5, wizardPoints, "Wizard");
                    ActionAdditionTemplate.AddClassAction(Actor, "LIGHTNING BOLT", 0.3, 5, wizardPoints, "Wizard");
                    break;
                // Note: The original code had both cases as Wand, which was likely a bug
                // This implementation adds both spells for wand users
            }
        }

        /// <summary>
        /// Gets all class-specific action names
        /// </summary>
        /// <returns>List of class action names</returns>
        public static List<string> GetClassActionNames()
        {
            return new List<string> 
            { 
                "BERSERKER RAGE", 
                "SHIELD WALL", 
                "SHADOW STRIKE", 
                "MAGIC MISSILE", 
                "FIREBALL", 
                "LIGHTNING BOLT", 
                "HEAL", 
                "BLESS" 
            };
        }

        /// <summary>
        /// Checks if an action is a class-specific action
        /// </summary>
        /// <param name="actionName">The action name to check</param>
        /// <returns>True if it's a class action</returns>
        public static bool IsClassAction(string actionName)
        {
            return GetClassActionNames().Contains(actionName);
        }

        /// <summary>
        /// Gets the class that an action belongs to
        /// </summary>
        /// <param name="actionName">The action name</param>
        /// <returns>The class name, or null if not a class action</returns>
        public static string? GetActionClass(string actionName)
        {
            return actionName switch
            {
                "BERSERKER RAGE" => "Barbarian",
                "SHIELD WALL" => "Warrior",
                "SHADOW STRIKE" => "Rogue",
                "MAGIC MISSILE" or "FIREBALL" or "LIGHTNING BOLT" or "HEAL" or "BLESS" => "Wizard",
                _ => null
            };
        }
    }
}


