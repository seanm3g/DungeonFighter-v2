using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages class-specific actions and abilities
    /// Handles adding/removing actions based on character progression
    /// </summary>
    public class ClassActionManager
    {
        private static readonly List<string> AllClassActions = new()
        {
            "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE",
            "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
            "FOCUS", "READ BOOK", "HEROIC STRIKE", "WHIRLWIND", "BERSERKER RAGE",
            "SHADOW STRIKE", "FIREBALL", "METEOR", "ICE STORM", "LIGHTNING BOLT",
            "FOLLOW THROUGH", "MISDIRECT", "CHANNEL"
        };

        /// <summary>
        /// Adds class-specific actions based on character progression
        /// </summary>
        public void AddClassActions(Actor entity, CharacterProgression? progression, WeaponType? weaponType)
        {
            // Only remove class actions if character has class points (to avoid removing gear actions)
            // If character has no class points, skip removal to preserve gear actions with class action names
            if (progression != null && HasAnyClassPoints(progression))
            {
                RemoveClassActions(entity, progression, weaponType);
            }
            
            AddBarbarianActions(entity, progression);
            AddWarriorActions(entity, progression);
            AddRogueActions(entity, progression);
            AddWizardActions(entity, progression, weaponType);
        }
        
        /// <summary>
        /// Checks if character has any class points
        /// </summary>
        private bool HasAnyClassPoints(CharacterProgression progression)
        {
            return progression.BarbarianPoints > 0 || 
                   progression.WarriorPoints > 0 || 
                   progression.RoguePoints > 0 || 
                   progression.WizardPoints > 0;
        }

        /// <summary>
        /// Adds barbarian-specific actions
        /// </summary>
        private void AddBarbarianActions(Actor entity, CharacterProgression? progression)
        {
            if (progression == null) return;

            if (progression.BarbarianPoints >= 2)
            {
                AddActionIfExists(entity, "FOLLOW THROUGH");
            }

            if (progression.BarbarianPoints >= 3)
            {
                AddActionIfExists(entity, "BERSERK");
            }
        }

        /// <summary>
        /// Adds warrior-specific actions
        /// </summary>
        private void AddWarriorActions(Actor entity, CharacterProgression? progression)
        {
            if (progression == null) return;

            if (progression.WarriorPoints >= 1)
            {
                AddActionIfExists(entity, "TAUNT");
            }

            if (progression.WarriorPoints >= 3)
            {
                AddActionIfExists(entity, "SHIELD BASH");
                AddActionIfExists(entity, "DEFENSIVE STANCE");
            }


        }

        /// <summary>
        /// Adds rogue-specific actions
        /// </summary>
        private void AddRogueActions(Actor entity, CharacterProgression? progression)
        {
            if (progression == null) return;

            if (progression.RoguePoints >= 2)
            {
                AddActionIfExists(entity, "MISDIRECT");
            }

            if (progression.RoguePoints >= 3)
            {
                AddActionIfExists(entity, "QUICK REFLEXES");
            }
        }

        /// <summary>
        /// Adds wizard-specific actions
        /// </summary>
        private void AddWizardActions(Actor entity, CharacterProgression? progression, WeaponType? weaponType)
        {
            if (progression == null) return;

            bool isWizardClass = IsWizardClass(progression, weaponType);
            
            if (isWizardClass)
            {
                if (progression.WizardPoints >= 1)
                {
                    AddActionIfExists(entity, "CHANNEL");
                }
                
                if (progression.WizardPoints >= 3)
                {
                    AddActionIfExists(entity, "FIREBALL");
                    AddActionIfExists(entity, "FOCUS");
                }
            }
        }

        /// <summary>
        /// Adds an action to actor if it exists in action loader
        /// </summary>
        private void AddActionIfExists(Actor entity, string actionName)
        {
            try
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    // CRITICAL: ALWAYS mark class actions as combo actions so they appear in GetActionPool()
                    // The GetActionPool() method only returns actions with IsComboAction == true
                    // Class actions should always be available in the action pool regardless of their JSON setting
                    action.IsComboAction = true;
                    DebugLogger.LogFormat("ClassActionManager", 
                        "Marked class action '{0}' as combo action", actionName);
                    
                    entity.AddAction(action, 1.0);
                    DebugLogger.LogFormat("ClassActionManager", 
                        "Added class action: {0} (isComboAction: {1})", actionName, action.IsComboAction);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogFormat("ClassActionManager", 
                    "Error adding action {0}: {1}", actionName, ex.Message);
            }
        }

        /// <summary>
        /// Removes class-specific actions from entity that the character should have based on progression
        /// Only removes actions that would be added by the class system, preserving gear actions
        /// </summary>
        private void RemoveClassActions(Actor entity, CharacterProgression? progression, WeaponType? weaponType) {
            if (progression == null) return;
            
            var actionsToRemove = new List<(Action action, double probability)>();
            
            // Build list of actions that should be present based on class points
            var expectedClassActions = new HashSet<string>();
            
            // Barbarian actions
            if (progression.BarbarianPoints >= 2)
            {
                expectedClassActions.Add("FOLLOW THROUGH");
            }
            if (progression.BarbarianPoints >= 3)
            {
                expectedClassActions.Add("BERSERK");
            }
            
            // Warrior actions
            if (progression.WarriorPoints >= 1)
            {
                expectedClassActions.Add("TAUNT");
            }
            if (progression.WarriorPoints >= 3)
            {
                expectedClassActions.Add("SHIELD BASH");
                expectedClassActions.Add("DEFENSIVE STANCE");
            }
            
            // Rogue actions
            if (progression.RoguePoints >= 2)
            {
                expectedClassActions.Add("MISDIRECT");
            }
            if (progression.RoguePoints >= 3)
            {
                expectedClassActions.Add("QUICK REFLEXES");
            }
            
            // Wizard actions
            bool isWizardClass = IsWizardClass(progression, weaponType);
            if (isWizardClass)
            {
                if (progression.WizardPoints >= 1)
                {
                    expectedClassActions.Add("CHANNEL");
                }
                if (progression.WizardPoints >= 3)
                {
                    expectedClassActions.Add("FIREBALL");
                    expectedClassActions.Add("FOCUS");
                }
            }
            
            // Only remove actions that are class actions AND the character should have from class points
            // This preserves gear actions that happen to have the same name
            foreach (var actionEntry in entity.ActionPool)
            {
                if (AllClassActions.Contains(actionEntry.action.Name) && 
                    expectedClassActions.Contains(actionEntry.action.Name))
                {
                    actionsToRemove.Add(actionEntry);
                }
            }
            
            foreach (var (action, _) in actionsToRemove)
            {
                try
                {
                    entity.RemoveAction(action);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogFormat("ClassActionManager", 
                        "Error removing action {0}: {1}", action.Name, ex.Message);
                }
            }

            if (actionsToRemove.Count > 0)
            {
                DebugLogger.LogFormat("ClassActionManager", 
                    "Removed {0} class actions (preserved gear actions with same names)", actionsToRemove.Count);
            }
        }

        /// <summary>
        /// Determines if character is wizard class
        /// </summary>
        private bool IsWizardClass(CharacterProgression progression, WeaponType? weaponType)
        {
            if (weaponType == WeaponType.Staff)
                return true;

            if (progression != null && progression.WizardPoints > 0)
                return true;

            return false;
        }
    }
}






