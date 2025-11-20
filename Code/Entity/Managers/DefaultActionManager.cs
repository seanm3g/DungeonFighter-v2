using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages default actions and fallback behavior
    /// Ensures basic attack is always available and handles unique action availability
    /// </summary>
    public class DefaultActionManager
    {
        /// <summary>
        /// Adds default actions to actor
        /// </summary>
        public void AddDefaultActions(Actor entity)
        {
            DebugLogger.LogMethodEntry("DefaultActionManager", "AddDefaultActions");
            EnsureBasicAttackAvailable(entity);
        }

        /// <summary>
        /// Ensures basic attack is available in the action pool
        /// </summary>
        public void EnsureBasicAttackAvailable(Actor entity)
        {
            bool hasBasicAttack = entity.ActionPool.Any(a => 
                string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));
            
            if (!hasBasicAttack)
            {
                var basicAttack = ActionLoader.GetAction("BASIC ATTACK");
                if (basicAttack != null)
                {
                    entity.AddAction(basicAttack, 1.0);
                    DebugLogger.Log("DefaultActionManager", "Added BASIC ATTACK to ActionPool from JSON");
                }
                else
                {
                    var fallbackBasicAttack = CreateFallbackBasicAttack();
                    entity.AddAction(fallbackBasicAttack, 1.0);
                    DebugLogger.Log("DefaultActionManager", "Added fallback BASIC ATTACK to ActionPool");
                }
            }
        }

        /// <summary>
        /// Gets list of available unique actions for a weapon
        /// </summary>
        public List<Action> GetAvailableUniqueActions(WeaponItem? weapon)
        {
            var uniqueActions = new List<Action>();
            var allActions = ActionLoader.GetAllActions();
            
            if (weapon != null)
            {
                string weaponType = weapon.WeaponType.ToString().ToLower();
                var weaponUniqueActions = allActions.Where(action => 
                    action.Name.Contains("unique", StringComparison.OrdinalIgnoreCase) || 
                    (action.Tags != null && action.Tags.Any(t => 
                        t.Contains("unique") && t.Contains(weaponType)))
                ).ToList();
                
                uniqueActions.AddRange(weaponUniqueActions);
            }
            
            var classUniqueActions = allActions.Where(action => 
                action.Name.Contains("class unique", StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            uniqueActions.AddRange(classUniqueActions);
            
            return uniqueActions;
        }

        /// <summary>
        /// Updates combo bonus from equipped gear
        /// </summary>
        public void UpdateComboBonus(CharacterEquipment equipment)
        {
            int bonus = 0;
            
            if (equipment.Head != null)
            {
                var stat = equipment.Head.StatBonuses.FirstOrDefault(s => s.StatType == "ComboBonus");
                if (stat != null) bonus += (int)stat.Value;
            }
            
            if (equipment.Body != null)
            {
                var stat = equipment.Body.StatBonuses.FirstOrDefault(s => s.StatType == "ComboBonus");
                if (stat != null) bonus += (int)stat.Value;
            }
            
            if (equipment.Weapon != null)
            {
                var stat = equipment.Weapon.StatBonuses.FirstOrDefault(s => s.StatType == "ComboBonus");
                if (stat != null) bonus += (int)stat.Value;
            }
            
            if (equipment.Feet != null)
            {
                var stat = equipment.Feet.StatBonuses.FirstOrDefault(s => s.StatType == "ComboBonus");
                if (stat != null) bonus += (int)stat.Value;
            }
        }

        /// <summary>
        /// Creates a fallback basic attack when none is available in JSON
        /// </summary>
        private Action CreateFallbackBasicAttack()
        {
            return new Action(
                name: "BASIC ATTACK",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: "A standard physical attack using STR + weapon damage",
                comboOrder: 0,
                damageMultiplier: 1.0,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                isComboAction: false
            );
        }
    }
}





