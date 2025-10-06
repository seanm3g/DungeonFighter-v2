using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages class-specific action loading and management
    /// Extracted from CharacterActions.cs to improve separation of concerns
    /// </summary>
    public static class ClassActionManager
    {
        /// <summary>
        /// Adds class-specific actions based on character progression and weapon type
        /// </summary>
        /// <param name="entity">The entity to add actions to</param>
        /// <param name="progression">The character's progression data</param>
        /// <param name="weaponType">The weapon type (optional)</param>
        public static void AddClassActions(Entity entity, CharacterProgression progression, WeaponType? weaponType)
        {
            DebugLogger.LogMethodEntry("ClassActionManager", "AddClassActions");
            
            // Remove existing class actions first
            RemoveClassActions(entity);
            
            DebugLogger.LogClassPoints(progression.BarbarianPoints, progression.WarriorPoints, progression.RoguePoints, progression.WizardPoints);
            
            AddBarbarianActions(entity, progression);
            AddWarriorActions(entity, progression);
            AddRogueActions(entity, progression);
            AddWizardActions(entity, progression, weaponType);
            
            DebugLogger.LogActionPoolChange(entity.Name, entity.ActionPool.Count, "After AddClassActions");
        }

        /// <summary>
        /// Removes all class-specific actions from the entity
        /// </summary>
        /// <param name="entity">The entity to remove actions from</param>
        public static void RemoveClassActions(Entity entity)
        {
            var classActions = new List<string> { "BERSERKER RAGE", "SHIELD WALL", "SHADOW STRIKE", "MAGIC MISSILE", "FIREBALL", "LIGHTNING BOLT", "HEAL", "BLESS" };
            
            foreach (var actionName in classActions)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    entity.RemoveAction(action);
                }
            }
        }

        private static void AddBarbarianActions(Entity entity, CharacterProgression progression)
        {
            // Add special Barbarian class action when they have at least 5 points
            if (progression.BarbarianPoints >= 5)
            {
                var berserkerRage = ActionLoader.GetAction("BERSERKER RAGE");
                if (berserkerRage != null)
                {
                    entity.AddAction(berserkerRage, 0.3); // 30% chance
                    DebugLogger.Log("ClassActionManager", $"Added BERSERKER RAGE to {entity.Name} (Barbarian Points: {progression.BarbarianPoints})");
                }
            }
        }

        private static void AddWarriorActions(Entity entity, CharacterProgression progression)
        {
            // Add special Warrior class action when they have at least 5 points
            if (progression.WarriorPoints >= 5)
            {
                var shieldWall = ActionLoader.GetAction("SHIELD WALL");
                if (shieldWall != null)
                {
                    entity.AddAction(shieldWall, 0.4); // 40% chance
                    DebugLogger.Log("ClassActionManager", $"Added SHIELD WALL to {entity.Name} (Warrior Points: {progression.WarriorPoints})");
                }
            }
        }

        private static void AddRogueActions(Entity entity, CharacterProgression progression)
        {
            // Add special Rogue class action when they have at least 5 points
            if (progression.RoguePoints >= 5)
            {
                var shadowStrike = ActionLoader.GetAction("SHADOW STRIKE");
                if (shadowStrike != null)
                {
                    entity.AddAction(shadowStrike, 0.35); // 35% chance
                    DebugLogger.Log("ClassActionManager", $"Added SHADOW STRIKE to {entity.Name} (Rogue Points: {progression.RoguePoints})");
                }
            }
        }

        private static void AddWizardActions(Entity entity, CharacterProgression progression, WeaponType? weaponType)
        {
            // Add special Wizard class actions when they have at least 5 points
            if (progression.WizardPoints >= 5)
            {
                // Add magic missile for all wizards
                var magicMissile = ActionLoader.GetAction("MAGIC MISSILE");
                if (magicMissile != null)
                {
                    entity.AddAction(magicMissile, 0.4); // 40% chance
                    DebugLogger.Log("ClassActionManager", $"Added MAGIC MISSILE to {entity.Name} (Wizard Points: {progression.WizardPoints})");
                }

                // Add weapon-specific wizard actions
                if (weaponType == WeaponType.Wand)
                {
                    var fireball = ActionLoader.GetAction("FIREBALL");
                    if (fireball != null)
                    {
                        entity.AddAction(fireball, 0.3); // 30% chance
                        DebugLogger.Log("ClassActionManager", $"Added FIREBALL to {entity.Name} (Staff + Wizard Points: {progression.WizardPoints})");
                    }
                }
                else if (weaponType == WeaponType.Wand)
                {
                    var lightningBolt = ActionLoader.GetAction("LIGHTNING BOLT");
                    if (lightningBolt != null)
                    {
                        entity.AddAction(lightningBolt, 0.3); // 30% chance
                        DebugLogger.Log("ClassActionManager", $"Added LIGHTNING BOLT to {entity.Name} (Wand + Wizard Points: {progression.WizardPoints})");
                    }
                }

                // Add healing spell for high-level wizards
                if (progression.WizardPoints >= 10)
                {
                    var heal = ActionLoader.GetAction("HEAL");
                    if (heal != null)
                    {
                        entity.AddAction(heal, 0.2); // 20% chance
                        DebugLogger.Log("ClassActionManager", $"Added HEAL to {entity.Name} (High Wizard Points: {progression.WizardPoints})");
                    }
                }

                // Add blessing spell for very high-level wizards
                if (progression.WizardPoints >= 15)
                {
                    var bless = ActionLoader.GetAction("BLESS");
                    if (bless != null)
                    {
                        entity.AddAction(bless, 0.15); // 15% chance
                        DebugLogger.Log("ClassActionManager", $"Added BLESS to {entity.Name} (Very High Wizard Points: {progression.WizardPoints})");
                    }
                }
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
