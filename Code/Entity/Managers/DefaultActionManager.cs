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
        /// <exception cref="InvalidOperationException">Thrown when basic attack is not found in Actions.json</exception>
        public void EnsureBasicAttackAvailable(Actor entity)
        {
            string basicAttackName = GameConstants.BasicAttackName;
            bool hasBasicAttack = entity.ActionPool.Any(a => 
                string.Equals(a.action.Name, basicAttackName, StringComparison.OrdinalIgnoreCase));
            
            if (!hasBasicAttack)
            {
                var basicAttack = ActionLoader.GetAction(basicAttackName);
                if (basicAttack == null)
                {
                    throw new InvalidOperationException($"{basicAttackName} action not found in Actions.json. Please ensure Actions.json contains a {basicAttackName} action.");
                }
                
                // CRITICAL: Mark BASIC ATTACK as combo action so it appears in GetActionPool()
                // The GetActionPool() method only returns actions with IsComboAction == true
                if (!basicAttack.IsComboAction)
                {
                    basicAttack.IsComboAction = true;
                    DebugLogger.Log("DefaultActionManager", $"Marked {basicAttackName} as combo action");
                }
                
                entity.AddAction(basicAttack, 1.0);
                DebugLogger.Log("DefaultActionManager", $"Added {basicAttackName} to ActionPool from JSON (isComboAction: {basicAttack.IsComboAction})");
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

    }
}





