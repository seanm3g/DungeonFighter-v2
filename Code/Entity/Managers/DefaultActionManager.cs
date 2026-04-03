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
        /// Default actions are actions marked with IsDefaultAction = true in Actions.json
        /// These actions are always available regardless of weapon type
        /// </summary>
        public void AddDefaultActions(Actor entity)
        {
            var allActionData = ActionLoader.GetAllActionData();
            
            foreach (var actionData in allActionData)
            {
                if (actionData.IsDefaultAction)
                {
                    var action = ActionLoader.GetAction(actionData.Name);
                    if (action != null && (entity.ActionPool == null || !entity.ActionPool.Any(entry => entry.action.Name == action.Name)))
                    {
                        // Mark as combo action so it appears in GetActionPool() and the player's action menu
                        if (!action.IsComboAction)
                        {
                            action.IsComboAction = true;
                            if (entity.ActionPool != null)
                            {
                                var comboActions = entity.ActionPool.Where(a => a.action.IsComboAction).ToList();
                                int maxOrder = comboActions.Count > 0 ? comboActions.Max(a => a.action.ComboOrder) : 0;
                                action.ComboOrder = maxOrder + 1;
                            }
                        }
                        entity.AddAction(action, 1.0);
                    }
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

    }
}





