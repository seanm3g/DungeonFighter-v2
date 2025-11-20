using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character actions, combo system, and action-related mechanics
    /// </summary>
    public class CharacterActions
    {
        public List<Action> ComboSequence { get; private set; } = new List<Action>();

        public CharacterActions()
        {
        }

        public double CalculateTurnsFromActionLength(double actionLength)
        {
            // Basic action length to turns conversion
            // 1.0 action length = 1 turn
            return actionLength;
        }

        public void RemoveItemActions(Actor Actor)
        {
            // Remove actions that were added from items
            // Implementation tracks which actions came from items and removes them
            RemoveClassActions(Actor);
        }

        public void AddDefaultActions(Actor Actor)
        {
            DebugLogger.LogMethodEntry("CharacterActions", "AddDefaultActions");
            
            // Ensure BASIC ATTACK is always available - this is critical for basic attack rolls (6-13)
            EnsureBasicAttackAvailable(Actor);
        }
        
        /// <summary>
        /// Ensures that BASIC ATTACK is always available in the Actor's action pool
        /// This is critical for basic attack rolls (6-13) to work properly
        /// </summary>
        public void EnsureBasicAttackAvailable(Actor Actor)
        {
            // Check if BASIC ATTACK is already in the action pool
            bool hasBasicAttack = Actor.ActionPool.Any(a => 
                string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));
            
            if (!hasBasicAttack)
            {
                // Load BASIC ATTACK from JSON to get proper settings
                var basicAttack = ActionLoader.GetAction("BASIC ATTACK");
                if (basicAttack != null)
                {
                    Actor.AddAction(basicAttack, 1.0); // High probability for basic attack
                    DebugLogger.Log("CharacterActions", "Added BASIC ATTACK to ActionPool from JSON");
                }
                else
                {
                    // Fallback if JSON loading fails - create a proper BASIC ATTACK
                    var fallbackBasicAttack = new Action(
                        name: "BASIC ATTACK",
                        type: ActionType.Attack,
                        targetType: TargetType.SingleTarget,
                        baseValue: 0, // Damage comes from STR + weapon, not baseValue
                        range: 1,
                        cooldown: 0,
                        description: "A standard physical attack using STR + weapon damage",
                        comboOrder: 0,
                        damageMultiplier: 1.0,
                        length: 1.0,
                        causesBleed: false,
                        causesWeaken: false,
                        isComboAction: false // BASIC ATTACK should NOT be a combo action
                    );
                    Actor.AddAction(fallbackBasicAttack, 1.0);
                    DebugLogger.Log("CharacterActions", "Added fallback BASIC ATTACK to ActionPool");
                }
            }
            else
            {
                DebugLogger.Log("CharacterActions", "BASIC ATTACK already available in ActionPool");
            }
        }

        public void AddClassActions(Actor Actor, CharacterProgression progression, WeaponType? weaponType)
        {
            DebugLogger.LogMethodEntry("CharacterActions", "AddClassActions");
            
            // Remove existing class actions first
            RemoveClassActions(Actor);
            
            DebugLogger.LogClassPoints(progression.BarbarianPoints, progression.WarriorPoints, progression.RoguePoints, progression.WizardPoints);
            
            AddBarbarianActions(Actor, progression);
            AddWarriorActions(Actor, progression);
            AddRogueActions(Actor, progression);
            AddWizardActions(Actor, progression, weaponType);
            
            DebugLogger.LogActionPoolChange(Actor.Name, Actor.ActionPool.Count, "After AddClassActions");
        }

        private void AddBarbarianActions(Actor Actor, CharacterProgression progression)
        {
            // Add special Barbarian class action when they have at least 5 points
            if (progression.BarbarianPoints >= 5)
            {
                var berserkerRage = ActionLoader.GetAction("BERSERKER RAGE");
                if (berserkerRage != null)
                {
                    Actor.AddAction(berserkerRage, 1.0);
                }
            }
        }

        private void AddWarriorActions(Actor Actor, CharacterProgression progression)
        {
            // Add special Warrior class actions when they have at least 5 points
            if (progression.WarriorPoints >= 5)
            {
                var heroicStrike = ActionLoader.GetAction("HEROIC STRIKE");
                if (heroicStrike != null)
                {
                    Actor.AddAction(heroicStrike, 1.0);
                }
                
                var whirlwind = ActionLoader.GetAction("WHIRLWIND");
                if (whirlwind != null)
                {
                    Actor.AddAction(whirlwind, 1.0);
                }
            }
        }

        private void AddRogueActions(Actor Actor, CharacterProgression progression)
        {
            // Add special Rogue class action when they have at least 5 points
            if (progression.RoguePoints >= 5)
            {
                var shadowStrike = ActionLoader.GetAction("SHADOW STRIKE");
                if (shadowStrike != null)
                {
                    Actor.AddAction(shadowStrike, 1.0);
                }
            }
        }

        private void AddWizardActions(Actor Actor, CharacterProgression progression, WeaponType? weaponType)
        {
            // Only add wizard actions if the character is actually a wizard class
            bool isWizardClass = progression.IsWizardClass(weaponType);
            
            if (isWizardClass)
            {
                // Add FIREBALL as a basic wizard spell (available at 3+ wizard points)
                if (progression.WizardPoints >= 3)
                {
                    var fireball = ActionLoader.GetAction("FIREBALL");
                    if (fireball != null)
                    {
                        Actor.AddAction(fireball, 1.0);
                    }
                }
                
                // Add special Wizard class action when they have at least 5 points
                if (progression.WizardPoints >= 5)
                {
                    var meteor = ActionLoader.GetAction("METEOR");
